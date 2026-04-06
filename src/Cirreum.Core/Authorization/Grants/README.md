# Grants (ReBAC)

## Relationship-Based Access Control for Cirreum Applications

Grants is Cirreum's opt-in ReBAC (Relationship-Based Access Control) system that
**augments the existing authorization pipeline** — it does not replace RBAC or ABAC.
The base authorization system (roles, resource authorizers, policy validators) continues
to work exactly as before. Grants adds a new dimension: *"for this operation, which
owners can this caller reach?"* — answered before the handler runs, without the handler
knowing anything about grant tables or relationships.

Grants integrates into the existing three-stage authorization pipeline as **Stage 1 Step 0**,
running before scope evaluators, resource authorizers, and policy validators. Resources
that don't implement a Granted interface are completely unaffected — the grant gate is
a no-op pass with zero overhead.

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [Grant Domain Setup](#grant-domain-setup)
4. [Request Interfaces](#request-interfaces)
5. [CRL Enforcement](#crl-enforcement)
6. [Permission Model](#permission-model)
7. [Reach Resolution Flow](#reach-resolution-flow)
8. [Caching](#caching)
9. [Discovery & Analysis](#discovery--analysis)
10. [DI Registration](#di-registration)
11. [Configuration](#configuration)
12. [Design Decisions](#design-decisions)

---

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Grant** | A stored relationship: *"caller X holds permission P on owner Y"* |
| **Domain** | A bounded context (e.g., Issues, Documents) identified by a marker interface |
| **Permission** | A namespaced capability (e.g., `issues:delete`, `issues:read`) |
| **AccessReach** | The computed set of owners a caller can touch for a given operation |
| **CRL** | Command / Read / List — the three grant-aware operation patterns |

### What Grants Is Not

- **Not RBAC** — roles live in Stage 2 resource authorizers. Grants don't replace roles;
  they answer *"which owners"*, not *"which actions"*.
- **Not a grant store** — Core defines the pipeline and contracts. The app implements
  `IGrantResolver<TDomain>` to query its own grants table.
- **Not mandatory** — if you never call `AddAccessGrants`, the pipeline behaves exactly
  as before. Zero overhead, zero configuration.

---

## Architecture

```text
                         ┌──────────────────────────────────────────────────────┐
                         │                 Authorization Pipeline                │
                         ├──────────────────────────────────────────────────────┤
                         │ Stage 1 — Scope (first-failure short-circuit)        │
                         │   Step 0: GrantEvaluator (if resource is Granted)    │  ← Grants
                         │   Step 0: OwnerScopeEvaluator (if Owner-Scoped)      │
                         │   Step 1: IScopeEvaluator[] (app-provided, optional)  │
                         │                                                      │
                         │ Stage 2 — Resource (aggregate, then short-circuit)   │
                         │   ResourceAuthorizerBase<T> (roles, ABAC rules)      │
                         │                                                      │
                         │ Stage 3 — Policy (aggregate)                         │
                         │   IPolicyValidator[] (hours, quotas, kill-switches)  │
                         └──────────────────────────────────────────────────────┘
```

### Component Roles

```text
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              App Layer                                              │
│                                                                                     │
│   [GrantDomain("issues")]                                                           │
│   public interface IIssueOperation;                                                 │
│                                                                                     │
│   public class IssueGrantResolver : IGrantResolver<IIssueOperation>  ← App writes   │
│   {                                                                                 │
│       ResolveGrantsAsync(...)  → queries grants table                               │
│       ShouldBypassAsync(...)   → admin role check (optional)                        │
│       ResolveHomeOwnerAsync(.) → home tenant (optional)                             │
│   }                                                                                 │
└─────────────────────────────────┬───────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              Core Layer (sealed, no extension points)               │
│                                                                                     │
│   GrantBasedAccessReachResolver<TDomain>  ← orchestrator                            │
│     • Bypass check (live, never cached)                                             │
│     • L1 scoped cache → L2 cross-request cache → cold-path resolution               │
│     • Merges grants + home owner → AccessReach                                      │
│                                                                                     │
│   GrantEvaluator  ← CRL enforcement                                                │
│     • Command: OwnerId ∈ reach (pre-handler)                                       │
│     • Read: stash reach for post-fetch check, or OwnerId ∈ reach when supplied      │
│     • List: OwnerIds ⊆ reach, stamp when null                                      │
│                                                                                     │
│   AccessReach  ← the gate's output                                                  │
│     • Denied (empty set) / Unrestricted (no bound) / Bounded (explicit owners)      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Grant Domain Setup

Every bounded context that uses grants declares a **domain marker interface** with
`[GrantDomain]`:

```csharp
[GrantDomain("issues")]
public interface IIssueOperation;
```

- The namespace (`"issues"`) is normalized to lowercase
- Declared once — all permissions in this domain share the namespace
- Validated at startup: a `TDomain` without `[GrantDomain]` throws `InvalidOperationException`

---

## Request Interfaces

Grants provides three composable interfaces that layer on top of existing Conductor
request types:

| Interface | Base | Sidecar | Scope |
|-----------|------|---------|-------|
| `IGrantedCommand<TDomain>` | `IAuthorizableCommand` | `OwnerId` (scalar) | Single-owner write |
| `IGrantedCommand<TDomain, TResponse>` | `IAuthorizableCommand<TResponse>` | `OwnerId` (scalar) | Single-owner write with response |
| `IGrantedRead<TDomain, TResponse>` | `IAuthorizableQuery<TResponse>` | `OwnerId` (scalar) | Single-owner read |
| `IGrantedCacheableRead<TDomain, TResponse>` | `ICacheableQuery<TResponse>` | `OwnerId` + `CallerAccessScope` | Cacheable single-owner read |
| `IGrantedList<TDomain, TResponse>` | `IAuthorizableQuery<TResponse>` | `OwnerIds` (plural) | Cross-owner query |

### Example

```csharp
[RequiresPermission("delete")]
public sealed record DeleteIssue(string Id) : IGrantedCommand<IIssueOperation> {
    public string? OwnerId { get; set; }
}

[RequiresPermission("read")]
public sealed record GetIssue(string Id) : IGrantedRead<IIssueOperation, Issue> {
    public string? OwnerId { get; set; }
}

[RequiresPermission("read")]
public sealed record ListIssues : IGrantedList<IIssueOperation, IReadOnlyList<Issue>> {
    public IReadOnlyList<string>? OwnerIds { get; set; }
}
```

---

## CRL Enforcement

The `GrantEvaluator` enforces timing rules per operation kind:

### Command

```text
OwnerId supplied  →  OwnerId ∈ reach? Pass : Deny
OwnerId null:
  • Global caller         →  Deny (OwnerId required for cross-tenant writes)
  • Unrestricted reach    →  Deny (OwnerId required — ambiguous target)
  • Single-owner reach    →  Auto-enrich OwnerId from reach, Pass
  • Multi-owner reach     →  Deny (ambiguous — caller must specify)
```

### Read

```text
OwnerId supplied  →  OwnerId ∈ reach? Pass : Deny
OwnerId null      →  Pass (Pattern C — reach stashed on IAccessReachAccessor,
                      handler checks post-fetch entity's owner against reach)
```

**Pattern C (existence-hiding):** The handler fetches the entity, checks
`reach.Contains(entity.OwnerId)`, and returns 404 (not 403) if the caller
doesn't have reach — preventing information leakage about resource existence.

### List

```text
OwnerIds supplied  →  OwnerIds ⊆ reach? Pass : Deny
OwnerIds null      →  Stamp OwnerIds from reach (unrestricted = null = no bound)
```

### Pre-flight: User Enabled Check

Before any CRL check, the evaluator verifies the application user is enabled
via `IOwnedApplicationUser.IsEnabled`. Disabled users are denied regardless of grants.

---

## Permission Model

### Declaration

Permissions are declared on grant-aware resources via `[RequiresPermission]`:

```csharp
// Single-arg — namespace auto-resolved from TDomain's [GrantDomain("issues")]
[RequiresPermission("delete")]
public sealed record DeleteIssue : IGrantedCommand<IIssueOperation> { ... }

// Two-arg explicit — namespace validated against domain
[RequiresPermission("issues", "delete")]
public sealed record ArchiveIssue : IGrantedCommand<IIssueOperation> { ... }

// Permission constant — namespace validated
[RequiresPermission(Permissions.Issues.Delete)]
public sealed record PurgeIssue : IGrantedCommand<IIssueOperation> { ... }
```

### Namespace Validation

All permissions on a granted resource **must** use the domain's namespace. A mismatch
throws `InvalidOperationException` at startup:

```csharp
// Runtime error — namespace "audit" does not match domain "issues"
[RequiresPermission("audit", "write")]
public sealed record BadAction : IGrantedCommand<IIssueOperation> { ... }
```

Cross-cutting concerns (audit logging, rate limiting) belong in Stage 2 resource
authorizers or Stage 3 policy validators — not in grant permissions.

### AND Semantics

When multiple permissions are declared, the caller must hold **all** of them on the
target owner(s). Permissions are evaluated with AND semantics, not OR.

---

## Reach Resolution Flow

```text
Hot  (L1 hit):  ResolveAsync → ShouldBypassAsync (live) → L1 dict hit → return
Warm (L2 hit):  ResolveAsync → bypass check → L1 miss → L2 cache hit → L1 populate → return
Cold (miss):    ResolveAsync → bypass check → L1 miss → L2 miss → factory(DB) → L2+L1 populate → return
```

### Resolution Steps

```mermaid
flowchart TD
    A[ResolveAsync] --> B{Authenticated?}
    B -- No --> C[AccessReach.Denied]
    B -- Yes --> D{ShouldBypassAsync?}
    D -- Yes --> E[AccessReach.Unrestricted]
    D -- No --> F{Permissions declared?}
    F -- No --> G[AccessReach.Denied]
    F -- Yes --> H{L1 cache hit?}
    H -- Yes --> I[Return cached reach]
    H -- No --> J{L2 cache hit?}
    J -- Yes --> K[Populate L1, return]
    J -- No --> L[ResolveGrantsAsync]
    L --> M[ResolveHomeOwnerAsync]
    M --> N[Merge grants + home owner]
    N --> O{Combined set empty?}
    O -- Yes --> P[AccessReach.Denied]
    O -- No --> Q[AccessReach.ForOwners]
    Q --> R[Populate L1 + L2, return]
```

### AccessReach Shapes

| Shape | `OwnerIds` | Meaning |
|-------|-----------|---------|
| **Denied** | `[]` (empty) | Caller has no access for this operation |
| **Unrestricted** | `null` | No bound — cross-tenant visibility (admin bypass) |
| **Bounded** | `["owner-a", "owner-b"]` | Explicit set of reachable owners |

---

## Caching

### Two-Level Cache

| Level | Scope | Storage | Purpose |
|-------|-------|---------|---------|
| **L1** | DI scope (per-request) | `Dictionary` on scoped resolver | Same user hitting 5 granted operations in one request resolves once |
| **L2** | Cross-request | `ICacheableQueryService` | Second request from same user is a cache hit |

### Cache Key Format

```
reach:v{version}:{callerId}:{domain}:{permissionSignature}
```

- **`callerId`** — covers both C2M (human users with delegated permissions) and M2M
  (service principals with app roles)
- **`domain`** — the `[GrantDomain]` namespace (e.g., `issues`)
- **`permissionSignature`** — sorted, `+`-joined permission names (e.g., `delete+write`)

Sorting is required for **cache correctness**: permissions use AND semantics, so
`["delete","archive"]` and `["archive","delete"]` must hit the same entry.

### Cache Tags

| Tag | Purpose |
|-----|---------|
| `reach:caller:{callerId}` | Invalidate all entries for a user |
| `reach:domain:{domain}` | Invalidate all entries for a domain |

### What's Never Cached

- **Bypass checks** (`ShouldBypassAsync`) — always live. Admin role promotion is immediate.
- **Denied reach** from unauthenticated callers — short-circuit before cache lookup.

### Invalidation

```csharp
// After granting/revoking — invalidate the affected user
await cacheInvalidator.InvalidateCallerAsync(targetUserId);

// After a domain-wide policy change — invalidate all users in this domain
await cacheInvalidator.InvalidateDomainAsync<IIssueOperation>();
```

---

## Discovery & Analysis

Grants integrates into Cirreum's existing authorization discovery and analysis system.
No parallel discovery mechanism is needed.

### Resource Discovery

`AuthorizationModel` automatically detects granted resources during assembly scanning:

- `ResourceTypeInfo.IsGranted` — whether the resource implements a Granted interface
- `ResourceTypeInfo.GrantDomain` — the `[GrantDomain]` namespace
- `ResourceTypeInfo.RequiredPermissions` — resolved permissions from `[RequiresPermission]`

These flow through to the serializable `ResourceInfo` export for API transport and
the `DomainCatalog` for organized resource browsing.

### Grant Domain Summary

`AuthorizationSnapshot.Capture()` produces `GrantDomainInfo` records:

```csharp
public sealed record GrantDomainInfo(
    string Domain,                        // e.g., "issues"
    IReadOnlyList<string> Permissions,    // all unique permissions in this domain
    int GrantedResourceCount              // number of resources in this domain
);
```

Admin UIs use this to populate permission-picker dropdowns without reflection.

### GrantedResourceAnalyzer

The analyzer detects grant-specific misconfigurations:

| Check | Severity | Description |
|-------|----------|-------------|
| Missing permissions | Warning | Granted resource without `[RequiresPermission]` — no permission gate |
| Inert permissions | Warning | `[RequiresPermission]` on non-granted resource — attribute has no effect |
| No resource authorizer | Info | Granted resource without Stage 2 authorizer — grants-only is valid but flagged |
| Orphaned domains | Warning | `[GrantDomain]` marker with no granted resources |
| Mixed authorization | Info | Domain boundary with both granted and non-granted resources — possible incomplete migration |

All metrics flow into the standard `AnalysisReport` and `AuthorizationSnapshot`:

```text
Granted Resources.GrantedResourceCount     = 12
Granted Resources.GrantDomainCount         = 3
Granted Resources.TotalPermissionCount     = 8
Granted Resources.MissingPermissionCount   = 0
Granted Resources.InertPermissionCount     = 1
Granted Resources.OrphanedDomainCount      = 0
```

---

## DI Registration

### Per-Domain Registration

```csharp
// Register grants for a single bounded context
services.AddAccessGrants<IIssueOperation, IssueGrantResolver>();
services.AddAccessGrants<IDocumentOperation, DocumentGrantResolver>();
```

Each call registers:
- The app's `IGrantResolver<TDomain>`
- Core's `GrantBasedAccessReachResolver<TDomain>` orchestrator
- Shared infrastructure (idempotent): `IAccessReachAccessor`, `AccessReachResolverSelector`,
  `GrantEvaluator`, cache settings

### Assembly-Scanned Registration

```csharp
// Discover and register all IGrantResolver<TDomain> implementations
services.AddGrantAuthorization(
    configuration: builder.Configuration,
    assemblies: [typeof(Program).Assembly],
    configureCaching: settings => settings.Expiration = TimeSpan.FromMinutes(10));
```

### Idempotency

- Per-domain: duplicate `AddAccessGrants<TDomain>` calls are no-ops
- Shared services: registered via `TryAdd` — safe to call repeatedly
- Cache infrastructure: registered once across all domains

---

## Configuration

```json
{
  "Cirreum": {
    "Authorization": {
      "Grants": {
        "Cache": {
          "Enabled": true,
          "Version": 1,
          "Expiration": "00:05:00",
          "DomainOverrides": {
            "issues": { "Expiration": "00:10:00" },
            "admin": { "Enabled": false }
          }
        }
      }
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Master switch for L2 caching |
| `Version` | `1` | Bump to invalidate all stale cache entries |
| `Expiration` | `00:05:00` | Default cache entry TTL |
| `DomainOverrides` | `{}` | Per-domain overrides keyed by `[GrantDomain]` namespace |

---

## Customization Patterns

### Shared Grant Resolver Base

When an app has multiple grant domains that share common behavior (e.g., the same
admin bypass logic or home-owner policy), create an app-level abstract base class:

```csharp
// App layer — shared defaults for all grant resolvers
public abstract class AppGrantResolverBase<TDomain> : IGrantResolver<TDomain>
    where TDomain : class {

    // Shared bypass: app-wide super-admin skips grants in every domain
    public virtual ValueTask<bool> ShouldBypassAsync<TResource>(
        AuthorizationContext<TResource> context,
        CancellationToken cancellationToken)
        where TResource : IAuthorizableResource =>
        new(context.EffectiveRoles.Any(r => r.Name == "SuperAdmin"));

    // Each domain provides its own grant lookup
    public abstract ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
        AuthorizationContext<TResource> context,
        CancellationToken cancellationToken)
        where TResource : IAuthorizableResource;

    // Shared home-owner with custom suspension check
    public virtual ValueTask<string?> ResolveHomeOwnerAsync<TResource>(
        AuthorizationContext<TResource> context,
        CancellationToken cancellationToken)
        where TResource : IAuthorizableResource {

        if (context.UserState.ApplicationUser is IOwnedApplicationUser { IsEnabled: true } user) {
            return new(user.OwnerId);
        }
        return new((string?)null);
    }
}
```

Domain resolvers inherit the shared behavior and only implement the data lookup:

```csharp
public class IssueGrantResolver : AppGrantResolverBase<IIssueOperation> {
    public override async ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
        AuthorizationContext<TResource> context, CancellationToken ct) {
        var ownerIds = await db.GetGrantedOwners(context.UserId, context.RequiredPermissions);
        return new GrantedReach(ownerIds);
    }
}

public class DocumentGrantResolver : AppGrantResolverBase<IDocumentOperation> {
    public override async ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
        AuthorizationContext<TResource> context, CancellationToken ct) {
        var ownerIds = await db.GetDocumentGrantedOwners(context.UserId, context.RequiredPermissions);
        return new GrantedReach(ownerIds);
    }
}
```

Core provides sensible defaults via default interface methods on `IGrantResolver<TDomain>`,
so this pattern is only needed when the app wants to **customize** those defaults
consistently across domains. If the defaults are fine, implement `IGrantResolver<TDomain>`
directly — no base class needed.

### Extensions for Auxiliary Dimensions

`AccessReach.Extensions` carries app-specific auxiliary dimensions through the pipeline:

```csharp
public override async ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
    AuthorizationContext<TResource> context, CancellationToken ct) {

    var (ownerIds, tiers) = await db.GetGrantsWithTiers(context.UserId, ...);

    return new GrantedReach(
        ownerIds,
        Extensions: new Dictionary<string, object> {
            ["allowed-tiers"] = tiers   // e.g., ["gold", "platinum"]
        });
}

// In the handler — read auxiliary dimensions from reach
public async Task<Result<Issue>> Handle(GetIssue request, CancellationToken ct) {
    var reach = reachAccessor.Get();
    var tiers = reach.Extensions?["allowed-tiers"] as IReadOnlyList<string>;
    // Apply as additional predicate scope...
}
```

Extensions are opaque to Core — they flow through the cache and reach accessor
unchanged. Handlers read them via `IAccessReachAccessor` and apply them as
additional filters.

---

## Design Decisions

### Why Grants Lives in Core

Grants is part of the Conductor authorization pipeline — the same layer that owns
`IAuthorizationEvaluator`, `ResourceAuthorizerBase<T>`, and `IScopeEvaluator`. Moving
it to a separate package would split the pipeline contract across assemblies. Core
already has identical patterns: settings POCOs, `IConfiguration` binding, assembly
scanning, and `ICacheableQueryService`.

### Why the App Never Touches AccessReach

`AccessReach` has three distinguished shapes (Denied / Unrestricted / Bounded) with
subtle edge cases (empty-set collapse, home-owner merge, null semantics). The
orchestrator (`GrantBasedAccessReachResolver`) handles all translation policy so apps
can't accidentally produce an invalid reach. Apps return `GrantedReach` (a simple
owner list) and Core does the rest.

### Why CRL Instead of a Generic "Grant Gate"

Command, Read, and List have fundamentally different timing requirements:
- Commands must know the target owner *before* the handler (no speculative writes)
- Reads may need to hide existence (Pattern C — check *after* fetch)
- Lists operate on sets, not scalars (subset enforcement, auto-stamping)

A single generic gate would either be too permissive or too restrictive. CRL captures
the real-world patterns.

### Why Bypass Is Never Cached

Admin role changes must take effect immediately. Caching bypass would mean a newly
promoted admin has to wait for TTL expiry — unacceptable for security-sensitive
operations. Bypass checks are cheap (in-memory role lookup) and always run live.

### Why Permission Sorting Matters

Permissions use AND semantics. `["delete","archive"]` and `["archive","delete"]`
represent the same requirement. Without deterministic sorting, they'd produce different
cache keys and miss each other's entries — causing unnecessary cold-path resolution.

---

## Related Documentation

- [Authorization Flow](../FLOW.md) — high-level request → authorization flow
- [Authorization Sequence](../SEQUENCE.md) — detailed three-stage pipeline
- [Operational Context](../../OPERATION-CONTEXT.md) — context composition and AccessScope
- [Conductor](../../Conductor/README.md) — in-process dispatcher + intercept pipeline
