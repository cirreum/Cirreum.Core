# Resource Access

## Object-Level ACL Evaluation for Cirreum Applications

Resource Access is Cirreum's opt-in object-level permission system. It answers
the question *"does this caller have permission X on this specific data object?"*
— evaluated **inside the handler** after the object is loaded, complementing the
request-time pipeline that runs before it.

This is the third authorization tier:

| Tier | Answers | When | Where |
|------|---------|------|-------|
| **Authorization** (root) | *Does this caller have the right roles/claims?* | Pre-handler (pipeline) | `AuthorizerBase<T>` |
| **Operations.Grants** | *Which owners can this caller access?* | Pre-handler (Stage 1) | `OperationGrantEvaluator` |
| **Resources** | *Does this caller have permission X on this object?* | In-handler (data-time) | `IResourceAccessEvaluator` |

Resource Access does **not** replace Grants or Roles — it layers on top. Grants
answer "which tenants"; Resources answer "which folders/documents/projects within
that tenant."

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [Type Reference](#type-reference)
4. [Hierarchy & Inheritance](#hierarchy--inheritance)
5. [Handler Patterns](#handler-patterns)
6. [DI Registration](#di-registration)
7. [Full Example](#full-example)
8. [Caching](#caching)
9. [Telemetry & Logging](#telemetry--logging)
10. [Design Decisions](#design-decisions)

---

## Core Concepts

| Concept | Description |
|---------|-------------|
| **ACL** | Access Control List — the collection of `AccessEntry` records on a resource |
| **ACE** | Access Control Entry — a single `AccessEntry`: one Role → one or more Permissions |
| **Protected Resource** | Any domain object implementing `IProtectedResource` (e.g., folders, projects, workspaces) |
| **Effective Access** | The computed union of a resource's own ACL + inherited ancestor ACLs + root defaults |
| **Root Defaults** | The fallback ACL at the top of the hierarchy (organization-wide defaults) |

### What Resource Access Is Not

- **Not a replacement for Grants** — Grants scope by owner/tenant; Resources scope
  by individual data object. A folder lives inside a tenant; both checks can apply.
- **Not a pipeline stage** — it runs inside handlers, not before them. The handler
  loads the data, then asks `IResourceAccessEvaluator` to check it.
- **Not mandatory** — if you never call `AddResourceAccess`, everything works exactly
  as before. Zero overhead, zero configuration.

---

## Architecture

```text
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Handler                                            │
│                                                                                 │
│   1. Load resource from database                                                │
│   2. Call IResourceAccessEvaluator                                              │
│      ├─ CheckAsync(resource, permission)      ← single object                  │
│      ├─ CheckAsync(resourceId, permission)    ← by ID (loads via provider)     │
│      └─ FilterAsync(resources, permission)    ← batch filtering                │
│                                                                                 │
└───────────────┬─────────────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                     ResourceAccessEvaluator (sealed, scoped)                    │
│                                                                                 │
│   1. Read caller's identity from IAuthorizationContextAccessor                  │
│      (pipeline already resolved — zero role resolution cost)                    │
│   2. Resolve effective access:                                                  │
│      ├─ L1 cache check (per-request dictionary)                                │
│      ├─ Merge resource's own AccessList                                         │
│      ├─ Walk up hierarchy (if InheritPermissions)                               │
│      │   ├─ Cycle detection (visited set)                                       │
│      │   ├─ Sibling optimization (L1 cache hit on parent)                       │
│      │   └─ Orphan detection (parent not found)                                 │
│      └─ Merge RootDefaults at hierarchy root                                    │
│   3. Check: does any entry grant permission for caller's role?                  │
│   4. Emit telemetry + structured logs                                           │
│                                                                                 │
└───────────────┬─────────────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    IAccessEntryProvider<T> (app-implemented)                     │
│                                                                                 │
│   GetByIdAsync(resourceId)  → load resource from store                          │
│   GetParentId(resource)     → navigate hierarchy                                │
│   RootDefaults              → organization-wide fallback ACL                    │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Type Reference

### Primitives

| Type | Kind | Description |
|------|------|-------------|
| `AccessEntry` | sealed record | A single ACE: binds a `Role` to `Permission[]` on a resource |
| `IProtectedResource` | interface | Marker for objects with an embedded ACL (`ResourceId`, `AccessList`, `InheritPermissions`) |

### Contracts

| Type | Kind | Implemented By |
|------|------|---------------|
| `IAccessEntryProvider<T>` | interface | App (persistence layer) — loads resources and navigates hierarchy |
| `IResourceAccessEvaluator` | interface | Framework — handlers inject this to check/filter |

### Internals

| Type | Kind | Description |
|------|------|-------------|
| `ResourceAccessEvaluator` | sealed class (internal) | Hierarchy walker + L1 cache + telemetry |
| `EffectiveAccess` | sealed class (internal) | Computed snapshot: merged ACL entries (L1 cache value) |
| `ResourceAccessBuilder` | sealed class | Fluent builder for provider registration |
| `ResourceAccessLogging` | static partial class | Source-generated structured log messages |

---

## Hierarchy & Inheritance

Resources form a tree. Each resource can inherit permissions from its ancestors:

```text
Root (no parent)
  └─ RootDefaults applied here
      │
      ├─ Workspace A  [InheritPermissions: true]
      │   ├─ own ACL: Editor → [browse, upload]
      │   │
      │   ├─ Project X  [InheritPermissions: true]
      │   │   ├─ own ACL: Reviewer → [browse]
      │   │   └─ effective: own + Workspace A + root defaults
      │   │
      │   └─ Project Y  [InheritPermissions: false]   ← breaks inheritance
      │       ├─ own ACL: Admin → [browse, upload, delete]
      │       └─ effective: own ACL only (no ancestors)
      │
      └─ Workspace B  [InheritPermissions: true]
          └─ effective: own + root defaults
```

### Algorithm

```text
ResolveEffectiveAccess(resource, provider):
  1. L1 cache check → return if hit
  2. entries = [...resource.AccessList]
  3. if resource.InheritPermissions:
     a. visited = { resource.ResourceId }
     b. walk up via provider.GetParentId → provider.GetByIdAsync:
        - parentId in visited → log cycle, stop
        - L1 has parent → merge cached entries, stop (sibling optimization)
        - parent null → log orphan, stop
        - merge parent.AccessList
        - !parent.InheritPermissions → stop
        - continue to parent's parent
     c. reached root → merge provider.RootDefaults
  4. if at root (no parent) → merge provider.RootDefaults
  5. cache in L1, return
```

### Edge Cases

| Scenario | Behavior |
|----------|----------|
| **Cycle** (A → B → A) | Detected via visited set, logged as warning, walk stops |
| **Orphan** (parent ID exists but parent not found) | Logged as warning, walk stops |
| **Broken inheritance** (`InheritPermissions: false`) | Walk stops at that resource; ancestors not merged |
| **Null ResourceId** (transient resource) | No caching; root defaults used if at root |
| **Sibling optimization** | If a parent was already resolved in this request, its cached effective access is reused — avoids redundant walks when checking siblings |

---

## Handler Patterns

### Check a loaded resource (commands, single lookups)

```csharp
public async Task<Result> Handle(DeleteDocument request, CancellationToken ct) {
    var document = await repository.GetAsync(request.DocumentId, ct);

    var authResult = await access.CheckAsync<DocumentFolder>(
        document.FolderId, Permissions.Document.Delete, ct);
    if (authResult.IsFailure) {
        return authResult;
    }

    await repository.DeleteAsync(document, ct);
    return Result.Success;
}
```

### Check by ID before loading (avoids unnecessary DB read)

```csharp
public async Task<Result<string>> Handle(UploadDocument request, CancellationToken ct) {
    var authResult = await access.CheckAsync<DocumentFolder>(
        request.FolderId, Permissions.Document.Upload, ct);
    if (authResult.IsFailure) {
        return authResult.Cast<string>();
    }

    // Authorized — proceed with upload
    var documentId = await storage.UploadAsync(request.File, request.FolderId, ct);
    return Result.Ok(documentId);
}
```

### Check with null ID (root defaults)

```csharp
// null folderId → "can this caller create top-level folders?"
var authResult = await access.CheckAsync<DocumentFolder>(
    (string?)null, Permissions.Folder.Create, ct);
```

### Filter a collection (queries, listings)

```csharp
public async Task<Result<IReadOnlyList<Folder>>> Handle(ListFolders request, CancellationToken ct) {
    var allFolders = await repository.GetAllAsync(request.WorkspaceId, ct);

    var authorized = await access.FilterAsync(
        allFolders, Permissions.Folder.Browse, ct);

    return Result.Ok(authorized);
}
```

### Permission delegation (Document → Folder)

A Document doesn't carry its own ACL — permissions live on the Folder. The handler
checks the document's folder:

```csharp
var authResult = await access.CheckAsync<DocumentFolder>(
    document.FolderId, Permissions.Document.Upload, ct);
```

This is a common pattern: leaf objects delegate authorization to their container.

---

## DI Registration

```csharp
services.AddResourceAccess(resources => {
    resources.AddProvider<DocumentFolder, DocumentFolderAccessEntryProvider>();
    resources.AddProvider<Project, ProjectAccessEntryProvider>();
});
```

This registers:
- `IResourceAccessEvaluator` → `ResourceAccessEvaluator` (Scoped, idempotent via `TryAdd`)
- `IAccessEntryProvider<DocumentFolder>` → `DocumentFolderAccessEntryProvider` (Scoped)
- `IAccessEntryProvider<Project>` → `ProjectAccessEntryProvider` (Scoped)

---

## Full Example

### 1. Define the protected resource

```csharp
public sealed class DocumentFolder : IProtectedResource {
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? ParentFolderId { get; init; }
    public IReadOnlyList<AccessEntry> AccessList { get; init; } = [];
    public bool InheritPermissions { get; init; } = true;

    // IProtectedResource
    string? IProtectedResource.ResourceId => this.Id;
}
```

### 2. Define permissions

```csharp
public static class Permissions {
    public static class Folder {
        public static readonly Permission Browse = new("folders", "browse");
        public static readonly Permission Create = new("folders", "create");
        public static readonly Permission Delete = new("folders", "delete");
    }
    public static class Document {
        public static readonly Permission Upload = new("documents", "upload");
        public static readonly Permission Download = new("documents", "download");
        public static readonly Permission Delete = new("documents", "delete");
    }
}
```

### 3. Implement the provider

```csharp
public sealed class DocumentFolderAccessEntryProvider(
    IFolderRepository repository) : IAccessEntryProvider<DocumentFolder> {

    public async ValueTask<DocumentFolder?> GetByIdAsync(
        string resourceId, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(resourceId, cancellationToken);

    public string? GetParentId(DocumentFolder resource) =>
        resource.ParentFolderId;

    public IReadOnlyList<AccessEntry> RootDefaults { get; } = [
        new AccessEntry {
            Role = Roles.Admin,
            Permissions = [
                Permissions.Folder.Browse,
                Permissions.Folder.Create,
                Permissions.Folder.Delete,
                Permissions.Document.Upload,
                Permissions.Document.Download,
                Permissions.Document.Delete
            ]
        },
        new AccessEntry {
            Role = Roles.Member,
            Permissions = [
                Permissions.Folder.Browse,
                Permissions.Document.Download
            ]
        }
    ];
}
```

### 4. Register

```csharp
services.AddResourceAccess(resources => {
    resources.AddProvider<DocumentFolder, DocumentFolderAccessEntryProvider>();
});
```

### 5. Use in a handler

```csharp
public sealed class UploadDocumentHandler(
    IResourceAccessEvaluator access,
    IDocumentStorage storage)
    : IRequestHandler<UploadDocument, Result<string>> {

    public async Task<Result<string>> Handle(
        UploadDocument request, CancellationToken ct) {

        // Check folder-level permission
        var authResult = await access.CheckAsync<DocumentFolder>(
            request.FolderId, Permissions.Document.Upload, ct);
        if (authResult.IsFailure) {
            return authResult.Cast<string>();
        }

        var documentId = await storage.UploadAsync(
            request.File, request.FolderId, ct);
        return Result.Ok(documentId);
    }
}
```

### What happens at runtime

```text
1. Handler calls CheckAsync<DocumentFolder>("folder-7", Document.Upload)
2. ResourceAccessEvaluator reads caller identity from IAuthorizationContextAccessor
   (already resolved by the authorization pipeline — zero re-resolution)
3. Loads folder-7 via DocumentFolderAccessEntryProvider.GetByIdAsync
4. folder-7 has InheritPermissions: true
5. Walks up: folder-7 → folder-3 → null (root)
6. Merges: folder-7 ACL + folder-3 ACL + RootDefaults
7. Checks: does any merged entry grant "documents:upload" for the caller's role?
8. Returns Result.Success or Result.Fail(ForbiddenAccessException)
```

---

## Caching

### L1 Per-Request Cache

| Key Format | Scope | Storage |
|------------|-------|---------|
| `{TypeName}:{ResourceId}` | DI scope (per-request) | `Dictionary` on scoped evaluator |

The L1 cache provides two optimizations:
- **Dedup:** Checking the same resource twice in one request resolves once.
- **Sibling optimization:** When checking folder A and folder B that share a parent,
  the parent's effective access is computed once and reused.

### Effective Roles

`ResourceAccessEvaluator` reads the resolved `AuthorizationContext` directly from
`IAuthorizationContextAccessor` — the caller's `UserState` and `EffectiveRoles`
are available immediately with zero role resolution cost. The authorization pipeline
always runs before the handler, so the context is guaranteed to be populated.

### No L2 Cache

Resource ACLs are typically mutable (users change folder permissions frequently).
An L2 cross-request cache would require complex invalidation. The L1 per-request
cache handles the hot case (multiple checks in one request) without staleness risk.

---

## Telemetry & Logging

### Structured Logs (Source-Generated)

| Event | Level | When |
|-------|-------|------|
| `ResourceAccessAllowed` | Information | Permission granted |
| `ResourceAccessDenied` | Warning | Permission denied (includes deny code) |
| `ResourceAccessCycleDetected` | Warning | Hierarchy cycle found during walk |
| `ResourceAccessOrphanDetected` | Warning | Parent resource not found during walk |

### Metrics (AuthorizationTelemetry)

Every exit path records a decision via `AuthorizationTelemetry.RecordDecision`:

| Decision | Reason |
|----------|--------|
| Pass | `pass` |
| Deny | `RESOURCE_ACCESS_DENIED` |
| Deny | `RESOURCE_NOT_FOUND` |

All instrumentation is zero-cost when OTel is not attached.

---

## Design Decisions

### Why In-Handler, Not Pipeline

The pipeline runs before the handler — it doesn't have the data object yet.
Resource ACLs live on the data (the folder, the project, the workspace). The
handler must load the object first, then check permissions. This is fundamentally
different from operation-level auth which gates on the *request shape*, not the
*data content*.

### Why IProtectedResource Is Orthogonal to IAuthorizableObject

A `DocumentFolder` implements `IProtectedResource` for object-level ACLs, but the
*request* (`UploadDocument`) implements `IAuthorizableObject` for pipeline auth.
They're different types at different layers. Forcing them into one interface would
conflate "what does this operation require?" with "what permissions does this data
object grant?"

### Why No L2 Cache

ACLs change frequently (user shares a folder, admin revokes access). An L2 cache
would serve stale permissions until TTL expires — unacceptable for security. The
L1 per-request cache is sufficient: it deduplicates within a single request without
staleness risk.

### Why Root Defaults Exist

Without root defaults, a newly created top-level folder would have an empty ACL —
nobody could access it, including admins. Root defaults provide the organization-wide
baseline (e.g., "Admins can do everything, Members can browse"). Resources override
by adding their own entries or breaking inheritance.

### Why the Provider Is Scoped

`IAccessEntryProvider<T>` is registered as Scoped because it typically depends on
scoped infrastructure (DbContext, repository). The evaluator is also Scoped, so its
L1 cache naturally aligns with the request lifetime.

---

## Related Documentation

- [Authorization Flow](../FLOW.md) — high-level operation → authorization flow
- [Authorization Sequence](../SEQUENCE.md) — detailed three-stage pipeline
- [Grants](../Operations/Grants/README.md) — grant-based access control (Stage 1)
- [Context Architecture](../../CONTEXT.md) — operation & authorization context
