# Authorization

The Cirreum authorization model — what protects your operations, where each
piece runs, and how to reach for the right tool.

This is the **map**. Each subsystem has its own deep-dive README; start here,
follow the links when you need detail.

---

## Table of Contents

1. [Mental Model](#mental-model)
2. [Pick Your Tool](#pick-your-tool)
3. [The Three-Stage Pipeline](#the-three-stage-pipeline)
4. [Core Types](#core-types)
5. [Stage 1 — Scope](#stage-1--scope)
6. [Stage 2 — Object Authorizers](#stage-2--object-authorizers)
7. [Stage 3 — Policy Validators](#stage-3--policy-validators)
8. [Resource ACLs (data-time)](#resource-acls-data-time)
9. [Permission Model](#permission-model)
10. [Domain Feature Resolution](#domain-feature-resolution)
11. [Discovery & Analysis](#discovery--analysis)
12. [DI Registration Cheat-Sheet](#di-registration-cheat-sheet)
13. [Sub-system References](#sub-system-references)

---

## Mental Model

Cirreum authorization runs in **two distinct moments**:

| When | Question | Mechanism |
|------|----------|-----------|
| **Pre-handler** (request-time) | *Can this caller invoke this operation?* | Three-stage pipeline (Scope → Authorizer → Policy) |
| **In-handler** (data-time) | *Does this caller have permission X on this specific object?* | `IResourceAccessEvaluator` (`AccessEntry`-based ACL) |

The pipeline runs once per operation and never sees the loaded data; the
data-time check runs after the handler reads it. Both can apply to the same
request — they answer different questions.

Authority always comes from the **app-user layer**
(`IOwnedApplicationUser`), not from IdP claims. Tokens identify *who* is
calling; the Cirreum user store decides *what* they can do.

Failures flow as `Result.Fail(...)`, never exceptions.

---

## Pick Your Tool

| If you need to… | Use | Stage | Deep dive |
|---|---|---|---|
| Gate "which owners can this caller access?" | `[RequiresGrant]` + `IOperationGrantProvider` | 1 (Step 0) | [Operations/Grants/README.md](Operations/Grants/README.md) |
| Cross-cutting Stage 1 pre-check (tenant gates, ambient invariants) | `IAuthorizationConstraint` | 1 (Step 1) | [Stage 1 below](#stage-1--scope) |
| Per-resource RBAC / CBAC / ABAC rules | `AuthorizerBase<TAuthorizableObject>` | 2 | [Stage 2 below](#stage-2--object-authorizers) |
| Cross-cutting runtime policies (hours, quotas, kill-switches) | `IPolicyValidator` | 3 | [Stage 3 below](#stage-3--policy-validators) |
| Per-row ACL on loaded data (folders, projects, workspaces) | `IResourceAccessEvaluator` + `AccessEntry` | n/a (in-handler) | [Resources/README.md](Resources/README.md) |

When in doubt: **start with an `AuthorizerBase<T>`**. If the rules are
generic across operations, extract a constraint or policy. Reach for grants
only when you genuinely need owner-scoped fan-out.

---

## The Three-Stage Pipeline

```text
┌────────────────────────────────────────────────────────────────────┐
│ Stage 1 — Scope (first-failure short-circuit)                      │
│   Step 0: OperationGrantEvaluator (only if Granted interface)      │
│   Step 1: IAuthorizationConstraint[] (registration order)          │
│                                                                    │
│ Stage 2 — Object Authorizers (aggregate, then short-circuit)       │
│   AuthorizerBase<T>  (one per resource type, FluentValidation)     │
│                                                                    │
│ Stage 3 — Policy Validators (aggregate)                            │
│   IPolicyValidator[]  (filtered by AppliesTo, sorted by Order)     │
└────────────────────────────────────────────────────────────────────┘
```

Why the strategy differs per stage:

- **Stage 1 short-circuits** because scope failures (wrong tenant, no
  granted access, kill-switch) make every downstream check meaningless.
- **Stage 2 aggregates** failures across rules within a single authorizer
  (so devs see *every* denial during iteration), then short-circuits to
  Stage 3 — policies are typically the expensive checks.
- **Stage 3 aggregates** to report all failing policies together. By the
  time you reach Stage 3 you've already passed Stages 1 and 2; spending a
  bit more to give a complete picture is a fair trade.

For the full request-flow sequence (HTTP → claims transform → conductor →
authorization → handler), see [FLOW.md](FLOW.md). For the inside of the
evaluator (preflight, role resolution, telemetry exit paths), see
[SEQUENCE.md](SEQUENCE.md).

---

## Core Types

### Caller identity & evaluation

| Type | Purpose |
|---|---|
| `IUserState` | Resolved caller identity, including the app-user record |
| `AuthorizationContext` (base) | `UserState` + `EffectiveRoles` — stored on `IAuthorizationContextAccessor` |
| `AuthorizationContext<TAuthorizableObject>` | Generic extension carrying the operation, its `RequiredGrants`, and `DomainFeature` |
| `IAuthorizationContextAccessor` | Scoped accessor — handlers/evaluators can read the resolved context without re-resolving roles |
| `IAuthorizationEvaluator` | Top-level orchestrator (`DefaultAuthorizationEvaluator`) — runs all three stages |

### Permissions

| Type | Purpose |
|---|---|
| `Permission` | A `feature:operation` pair (e.g., `issues:delete`). Case-insensitive value type |
| `PermissionSet` | General-purpose immutable collection of `Permission` values with query helpers |
| `RequiresGrantAttribute` | Declares grant requirements on an authorizable object (Stage 1 enforcement) |
| `RequiredGrantCache` | Per-type cache of `[RequiresGrant]` declarations; resolves namespace-derived features |

### Roles

| Type | Purpose |
|---|---|
| `Role` | A named role (case-insensitive equality) |
| `IAuthorizationRoleRegistry` | Registers roles + inheritance, computes `EffectiveRoles` |
| `ApplicationRoles` | Default role-collection contract |

### Resource ACLs (data-time)

| Type | Purpose |
|---|---|
| `IProtectedResource` | Marks an entity as ACL-aware (`AccessList`, `InheritPermissions`, `AncestorResourceIds`) |
| `AccessEntry` | A single ACE: one `Role` → one or more `Permission`s |
| `IAccessEntryProvider<T>` | App-implemented loader for protected resources |
| `IResourceAccessEvaluator` | Handler-facing API: `CheckAsync` / `FilterAsync` |

### Design Note: Core Stays CQRS-Neutral

Core ships `IOperation` and `IOperation<TResultValue>` (plus `ICacheableOperation<T>`)
as the only operation contracts. There is no `ICommand`, no `IQuery`, no
read/write split at the framework level — the pipeline treats every operation
the same way. Distinguishing intent (command vs. query, mutation vs. read,
sync vs. async) is an **app concern**, and apps are free to express it however
fits the codebase:

- A pair of marker interfaces (`ICommand : IOperation`, `IQuery<T> : IOperation<T>`)
- An abstract base record/class (`abstract record QueryBase<T> : IOperation<T>`)
- A namespace convention (`MyApp.Domain.Issues.Commands.*`, `.Queries.*`)
- Nothing at all — operations are operations

Don't add markers reflexively. Add them when they earn their keep — for
example, when a marker interface lets DI register a behavior across a whole
class of operations, or when domain reviewers want the intent visible at the
type signature. The framework never inspects them.

---

## Stage 1 — Scope

### Step 0 — Grant gate (optional)

Runs only when the authorizable object implements one of the granted
interfaces (`IOwnerMutateOperation`, `IOwnerLookupOperation`,
`IOwnerSearchOperation`, `ISelfMutateOperation`, `ISelfLookupOperation`).
Resolves the caller's `OperationGrant` and enforces grant timing per
operation kind. **Full details:** [Operations/Grants/README.md](Operations/Grants/README.md).

### Step 1 — `IAuthorizationConstraint`

Cross-cutting global pre-checks evaluated against the
`AuthorizationContext`. Constraints run in registration order; the first
failure short-circuits Stage 1. They do not know about override roles or
the `IOwnedApplicationUser` (those live in the grant evaluator).

```csharp
public sealed class TenantSuspensionConstraint(ITenantStatusProvider tenants)
    : IAuthorizationConstraint {

    public async Task<ValidationResult> EvaluateAsync<TAuthorizableObject>(
        AuthorizationContext<TAuthorizableObject> context,
        CancellationToken ct)
        where TAuthorizableObject : notnull, IAuthorizableObject {

        if (context.TenantId is null) {
            return new ValidationResult();
        }
        var status = await tenants.GetStatusAsync(context.TenantId, ct);
        return status.IsSuspended
            ? new ValidationResult([new ValidationFailure("tenant", "Tenant is suspended.")])
            : new ValidationResult();
    }
}
```

Register with `services.AddScoped<IAuthorizationConstraint, TenantSuspensionConstraint>()`.

---

## Stage 2 — Object Authorizers

Exactly **one** `AuthorizerBase<TAuthorizableObject>` per authorizable
object type. FluentValidation rules inside aggregate; multiple registrations
for the same type throw at evaluation.

```csharp
public sealed class DeleteIssueAuthorizer : AuthorizerBase<DeleteIssue> {
    public DeleteIssueAuthorizer() {
        this.HasAnyRole(Roles.IssueManager, Roles.IssueAdmin);

        this.WhenRequiresGrant(Permissions.Issues.Delete, () =>
            this.HasRole(Roles.IssueManager))
            .Otherwise(() =>
                this.HasRole(Roles.ReadOnly));
    }
}
```

Helper methods on `AuthorizerBase<T>`:

| Method | Purpose |
|---|---|
| `HasRole`, `HasAnyRole`, `HasAllRoles`, `HasTwoOrMoreRoles` | Role assertions |
| `HasClaim(type, value)` | Direct principal claim assertion |
| `WhenRequiresGrant`, `WhenRequiresAnyGrant`, `WhenRequiresAllGrants` | Branch rules on declared `[RequiresGrant]` permissions via `ctx.RequiredGrants` |
| `UnlessRequiresGrant`, `UnlessRequiresAnyGrant`, `UnlessRequiresAllGrants` | Inverse branches |

The `When/UnlessRequires*Grant` family inspects the *declared* requirements (what
the operation says it needs), not the caller's held permissions. This lets
a single authorizer apply different rules to operations that share its
type (rare) or branch on permission stacking.

Register with `services.AddScoped<IAuthorizer<DeleteIssue>, DeleteIssueAuthorizer>()`.

---

## Stage 3 — Policy Validators

Cross-cutting runtime policies — hours, quotas, kill-switches, A/B-style
gates. Filtered by `SupportedRuntimeTypes` and `AppliesTo(...)`, sorted by
`Order` (ascending = earlier).

```csharp
public sealed class BusinessHoursPolicy : IPolicyValidator {
    public string PolicyName => "business-hours";
    public int Order => 100;
    public DomainRuntimeType[] SupportedRuntimeTypes => [DomainRuntimeType.ServerApi];

    public bool AppliesTo<T>(T authorizableObject, DomainRuntimeType rt, DateTimeOffset ts)
        where T : notnull, IAuthorizableObject =>
        authorizableObject is IRestrictedHoursOperation;

    public Task<ValidationResult> ValidateAsync<T>(
        AuthorizationContext<T> context, CancellationToken ct)
        where T : notnull, IAuthorizableObject {

        var hour = context.Timestamp.Hour;
        return Task.FromResult(hour is >= 9 and < 17
            ? new ValidationResult()
            : new ValidationResult([new ValidationFailure("hours", "Outside business hours.")]));
    }
}
```

Register with `services.AddScoped<IPolicyValidator, BusinessHoursPolicy>()`.

Stage 3 **aggregates** failures across all applicable policies, so callers
see the complete list of policy denials in one shot.

---

## Resource ACLs (data-time)

When permissions live on the *data* (folders, projects, workspaces),
handlers check after loading the object via `IResourceAccessEvaluator`:

```csharp
public async Task<Result<string>> Handle(UploadDocument request, CancellationToken ct) {
    var authResult = await access.CheckAsync<DocumentFolder>(
        request.FolderId, Permissions.Document.Upload, ct);
    if (authResult.IsFailure) {
        return authResult.Cast<string>();
    }
    var documentId = await storage.UploadAsync(request.File, request.FolderId, ct);
    return Result.Ok(documentId);
}
```

The evaluator walks the resource's ACL and inherited ancestor ACLs (with
batch-load optimization for materialized ancestor chains), merges
`RootDefaults`, and checks whether any entry grants the permission for the
caller's effective role. Per-request L1 cache; no L2 (ACLs are
volatile).

**Full details:** [Resources/README.md](Resources/README.md).

---

## Permission Model

A `Permission` is a `feature:operation` pair (e.g., `issues:delete`). The
`feature` corresponds to a bounded context; the `operation` is a verb.
Both are case-insensitive.

### Declaring requirements

`[RequiresGrant]` declares the permissions an operation requires. It has
two ctor forms:

```csharp
// Operation-only — feature derived from namespace convention
[RequiresGrant("delete")]
public sealed record DeleteIssue : IOwnerMutateOperation { ... }

// Explicit feature + operation — feature validated against domain
[RequiresGrant("issues", "archive")]
public sealed record ArchiveIssue : IOwnerMutateOperation { ... }
```

Stack multiple attributes for AND semantics:

```csharp
[RequiresGrant("write")]
[RequiresGrant("audit")]
public sealed record AuditedWrite : IOwnerMutateOperation { ... }
```

### Where the declared set surfaces

| Location | Use |
|---|---|
| `RequiredGrantCache.GetFor<T>()` | Per-type cache populated once at startup |
| `AuthorizationContext<T>.RequiredGrants` | Hoisted `PermissionSet` available to every stage |
| Stage 1 grant gate | **Enforces** — only owners holding every required permission qualify |
| Stage 2 / Stage 3 | **Inspect-only** — branch rules on what the operation declared |

> [!IMPORTANT]
> `[RequiresGrant]` is *not* a Stage 2/3 gate. It declares requirements
> that the grant pipeline enforces. ACL permissions on a loaded resource
> (`AccessEntry.Permissions`) are an entirely separate concept; do not
> conflate them.

### `PermissionSet`

A general-purpose immutable collection of `Permission` values. Construct
it directly, query it, filter by feature. Not tied to grants — the grant
pipeline is just one consumer.

```csharp
var set = new PermissionSet([new Permission("issues", "delete"), new Permission("issues", "read")]);
set.Contains(Permissions.Issues.Delete);     // true
set.HasFeature("issues");                    // true
set.HasOperation("delete");                  // true
var issueOps = set.ForFeature("issues");     // subset
```

---

## Domain Feature Resolution

The domain feature is derived structurally from the C# namespace — no
attribute or marker interface needed:

```text
MyApp.Domain.Issues.Commands.DeleteIssue   →  feature = "issues"
MyApp.Domain.Admin.Queries.ListUsers       →  feature = "admin"
```

`DomainFeatureResolver` finds the first segment after `"Domain"` and
lowercases it. Cached per-type for zero-cost repeated lookups.

The resolved value surfaces on `AuthorizationContext.DomainFeature` and
`OperationContext.DomainFeature`, and is consumed by `RequiredGrantCache`
to resolve operation-only `[RequiresGrant("delete")]` to a fully qualified
`Permission`.

---

## Discovery & Analysis

`DomainModel` scans assemblies once and exposes:

| Surface | Use |
|---|---|
| `GetAllResources()` / `GetAuthorizableResources()` | Every resource type with its authorizer, rules, grant metadata |
| `GetAuthorizationRules()` | Every FluentValidation rule across every authorizer |
| `GetPolicyRules()` | Every registered `IPolicyValidator` with its `AppliesTo` shape |
| `GetCatalog()` | Catalog organized by Domain Boundary → Resource Kind → Resource |

`DomainSnapshot.Capture(...)` returns a fully serializable snapshot
including the catalog, role hierarchy, mermaid diagrams, and analysis
report — designed for admin dashboards and cross-runtime comparisons (WASM
vs Server).

Built-in analyzers detect common misconfigurations:

| Analyzer | Detects |
|---|---|
| `GrantedResourceAnalyzer` | Granted resources without `[RequiresGrant]`, cross-feature permissions, missing grant providers, mixed authorization within a feature |
| Role-hierarchy / authorizer / policy analyzers | Orphan roles, missing authorizers on protected resources, etc. |

---

## DI Registration Cheat-Sheet

```csharp
// Roles + base authorization (always required)
services.AddCirreumAuthorization(typeof(Program).Assembly);

// Per-resource authorizers (one per IAuthorizableObject)
services.AddScoped<IAuthorizer<DeleteIssue>, DeleteIssueAuthorizer>();

// Constraints (Stage 1 Step 1) — zero or more, registration order matters
services.AddScoped<IAuthorizationConstraint, TenantSuspensionConstraint>();

// Policies (Stage 3) — zero or more
services.AddScoped<IPolicyValidator, BusinessHoursPolicy>();

// Optional: grant-based access control (Stage 1 Step 0)
services.AddOperationGrants<AppOperationGrantProvider>();

// Optional: resource ACLs (in-handler)
services.AddResourceAccess(r => r.AddProvider<DocumentFolder, DocumentFolderAccessEntryProvider>());
```

If you never call `AddOperationGrants` or `AddResourceAccess`, those
subsystems are inert — zero overhead, zero configuration.

---

## Compliance Boundary

If you're mapping Cirreum to NIST SP 800-53, OWASP ASVS, ISO/IEC 27001, or
similar — this section delineates what the framework guarantees vs. what
remains the application's responsibility. The short version: **Cirreum
enforces the *envelope*; apps must still configure the *contents*
correctly.**

### What the framework enforces

These properties are guaranteed by Cirreum itself; apps cannot accidentally
disable them through configuration alone.

| Property | Where enforced |
|---|---|
| **Default-deny on incomplete authorization graph.** An `IAuthorizableOperation` with no constraints, no authorizer, no policies, and no grant gate fails closed at runtime — not "passes through." | `DefaultAuthorizationEvaluator` |
| **Default-deny on missing roles.** A caller with no registered roles is denied before any stage runs. | `DefaultAuthorizationEvaluator` |
| **No deny-override across stages.** First failure short-circuits Stage 1; Stage 2 aggregates then short-circuits to Stage 3. Once any stage denies, no later stage can rescue. | Pipeline orchestration |
| **No wildcard scope.** `Permission` is a strict `feature:operation` pair. There is no `*` pattern, no string matching, no scope hierarchy that could accidentally collapse to "everything." | `Permission` value type |
| **Bypass is never cached.** `ShouldBypassAsync` runs live on every request — admin promotion *and* revocation take effect immediately. | `OperationGrantFactory` |
| **Self-scoped identity match is hardcoded.** `ExternalId == UserId` is not config-toggleable. | `OperationGrantEvaluator` |
| **Cross-feature grant declarations rejected at startup.** A `[RequiresGrant("audit","write")]` on a type in `*.Domain.Issues.*` throws — can't accidentally grant cross-feature access. | `RequiredGrantCache` feature validation |
| **Authorization runs *before* the handler.** The intercept placement is fixed; handlers cannot run for an `IAuthorizableOperation` that hasn't passed all stages. | `AuthorizationIntercept` |

### What the application owns

These remain the app's responsibility because the framework cannot inspect
the semantics of app-supplied data and code. A misconfigured app can
over-grant; the framework cannot detect it.

| Responsibility | Why the framework can't enforce it |
|---|---|
| **Role permission scopes.** Cirreum has no concept of "minimal capability." If `Roles.User` is declared with admin-equivalent permissions, that's allowed. | Roles are app-defined data |
| **`IOperationGrantProvider.ResolveGrantsAsync` data correctness.** A resolver that returns "all owners" for every caller over-grants. Cirreum trusts the data. | Grants live in the app's database |
| **Authorizer rule adequacy.** A `Pass()`-only authorizer authorizes everything. Cirreum runs FluentValidation rules; it does not analyze them for restrictiveness. | Rules are app-defined predicates |
| **Policy validator logic.** A validator that returns success unconditionally lets everything through. | Same as above |
| **Pattern C handler-deferred ownership checks.** When a lookup is invoked with no `OwnerId`, Stage 1 stashes the grant on `IOperationGrantAccessor` and *passes* — the handler is expected to check `grant.Contains(entity.OwnerId)` after fetch. If the handler forgets, that's an authorization bypass. | Framework cannot observe handler internals |
| **Handler-internal data access.** A handler can read records, return them, or branch on them in ways the framework doesn't see. ACL filtering belongs *inside* the handler via `IResourceAccessEvaluator`. | Same — out of pipeline scope |
| **`ShouldBypassAsync` decision boundary.** App decides who bypasses. A bypass-everyone resolver is allowed by Cirreum. | Bypass is app-defined |
| **Grant cache invalidation.** Cirreum's L2 cache is eventually consistent up to TTL; revocations require explicit `cacheInvalidator.InvalidateCallerAsync` / `InvalidateFeatureAsync` calls. The framework can't observe revocations happening in the app's database. | App owns the invalidation channel |

### How to harden the app side

The framework provides analyzer rules and runtime signals to close most of
the application-side gaps:

**Boot-time enforcement** — call once after host build to convert Error-severity analyzer findings into a startup failure:

```csharp
var app = builder.Build();
app.Services.ValidateAuthorizationConfiguration();   // throws AuthorizationConfigurationException on Error severity
app.Run();
```

This runs every registered `IDomainAnalyzer` and throws `AuthorizationConfigurationException` if any reports an error. The exception carries the full `AnalysisReport` for logging/telemetry. For callers who want to branch on the result rather than throw, the sibling `CheckAuthorizationConfiguration()` returns the report (or `null` when the configuration passes). Cross-platform — works in ASP.NET Core, Functions, WASM bootstraps, and console hosts.

**Static analysis** (the rules `ValidateAuthorizationConfiguration` runs; treat **Error** severities as build breaks in CI):

- **`AuthorizableResourceAnalyzer`** — flags `IAuthorizableOperation` types missing an authorizer at startup with **Error** severity. The runtime backstop in `DefaultAuthorizationEvaluator` denies if missed, but boot-time detection is preferred.
- **`GrantedResourceAnalyzer`** — flags:
  - Granted resources without `[RequiresGrant]` (Warning)
  - Cross-domain permission declarations on granted resources (Warning)
  - Mixed-authorization domains (Info — possible incomplete migration)
  - Missing `IOperationGrantProvider` when granted resources exist (Error)
  - **Unsafe `ICacheableOperation` + grant interface combinations** (Error) — flags any granted operation that mixes caching with grant semantics outside `IOwnerCacheableLookupOperation<T>`. Shared cache entries cannot safely span callers with different grant scopes.
- Other analyzers (`AnonymousResourceAnalyzer`, `RoleHierarchyAnalyzer`, `ProtectedResourceAnalyzer`, etc.) cover orthogonal concerns; see [Introspection/Analyzers](../Introspection/Analyzers/).

**Runtime signals** (queryable from handler/Stage 2/Stage 3 code; emitted as OTel activity tags for dashboards):

- **`IOperationGrantAccessor.OwnerWasAutoStamped`** — `true` when Stage 1 inferred `OwnerId` from a single-owner grant rather than the caller supplying it. Forensic signal for audit (was the user explicit, or did the framework choose?). OTel tag: `cirreum.authz.grant.owner_auto_stamped`.
- **`IOperationGrantAccessor.WasRead`** — `true` once handler code has read `Current` at least once. Used by the Pattern C audit hook below to detect missing post-fetch ownership checks.
- **`GrantedLookupAudit<,>` intercept** — runs after every `IGrantableLookupBase` operation. When the operation entered with `OwnerId == null` (Pattern C path) and the handler completed without reading `IOperationGrantAccessor.Current`, emits a **warning log** (`PatternCBypassDetected`) and an OTel activity tag (`cirreum.authz.grant.pattern_c_bypass`). This is the runtime backstop for the highest-risk surface in the model — it cannot deny (handler already returned), but it makes missing checks visible in traces and SIEM pipelines so they get caught in CI/dev.
- **`AuthorizationTelemetry.RecordDecision`** — every gate decision (Stage 1 / 2 / 3, pass / deny, reason, scope, evaluator, resource type) is recorded as an OTel counter + tagged on the current activity. Pipe to your tracing collector; the framework is zero-cost when no listeners are attached.

The combination — boot-time analyzer errors + scoped runtime signals + per-decision telemetry — gives operators a real-time view of authorization posture without requiring code-side audit instrumentation.

### Standards mapping

| Control / standard | Status |
|---|---|
| NIST SP 800-53 **AC-3** (Access Enforcement) | **Enforced.** Pipeline gates every `IAuthorizableOperation`; default-deny on incomplete config. |
| NIST SP 800-53 **AC-6** (Least Privilege) | **Enforced at the framework boundary** (no implicit allows, no deny-override, no wildcards, immediate bypass revocation, cross-feature rejection). **Not enforced inside app code** — role permission scopes, grant resolution data, authorizer/policy logic, and handler-internal data access remain app responsibilities. Pattern C handler-deferred checks are the highest-risk surface; analyzer rules close most of this gap. |
| NIST SP 800-53 **AU-2 / AU-12** (Audit Events) | **Supported.** `AuthorizationTelemetry` records every decision via OTel; activity tags include stage, step, decision, reason, scope, evaluator, resource type. App is responsible for collector configuration. |
| NIST SP 800-162 (ABAC) | **Aligned.** `IAuthorizationConstraint` and `IPolicyValidator` are PDPs over `AuthorizationContext`. PIP boundary is implicit (any DI service); make it explicit if you ever ship policies declaratively (OPA/Rego, XACML). |
| OWASP ASVS V4 (Access Control) | **Aligned at framework boundary.** Pattern C is the residual risk surface — close via analyzer + handler-side discipline. |
| OWASP Top 10 #1 (Broken Access Control) | **Pattern C is the residual risk.** All other paths are framework-enforced. |
| ISO/IEC 27001 A.9.4.1 (Information Access Restriction) | **Supported.** `IResourceAccessEvaluator` provides per-object ACL evaluation; document audit-trail expectations in your control narrative. |

---

## Sub-system References

- [Authorization Flow](FLOW.md) — full request flow from HTTP entry through pipeline exit
- [Pipeline Sequence](SEQUENCE.md) — three-stage pipeline internals, telemetry exit paths, allocation notes
- [Grants](Operations/Grants/README.md) — grant-based access control (Stage 1 Step 0)
- [Resources](Resources/README.md) — object-level ACLs evaluated in-handler
- [Operation & Authorization Context](../CONTEXT.md) — context architecture, AuthenticationBoundary, scoped accessors
- [Changelog](../../docs/CHANGELOG.md) — release-by-release summary
- [Migration v3 → v4](../../docs/MIGRATION-v4.md) — current major version migration
- [Migration v2 → v3](../../docs/MIGRATION-v3.md) — historical reference
