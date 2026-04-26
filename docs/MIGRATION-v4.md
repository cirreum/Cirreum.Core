# Cirreum.Core v4.0.0 — Migration Guide

> **From:** v3.x &nbsp;•&nbsp; **To:** v4.0.0
>
> Migrating from v2 → v3? See [MIGRATION-v3.md](MIGRATION-v3.md) instead — that
> guide covers a different transition and is unrelated to the v4 changes here.

## Why v4

This release sharpens Cirreum's authorization vocabulary, closes real bypass surfaces
in the grant pipeline, and ships compile-time, boot-time, runtime, and data-time
security signals that adopters can wire into compliance evidence. The renames are
extensive but mechanical (one find/replace pass); the new capabilities are additive.

The security posture story changes meaningfully:

| Time horizon | What's new |
|---|---|
| **Compile time** | New analyzer rule flags unsafe `ICacheableOperation` + grant interface combinations as Error severity |
| **Boot time** | `app.Services.ValidateAuthorizationConfiguration()` throws `AuthorizationConfigurationException` on Error-severity findings |
| **Request time** | Three new OTel signals — auto-stamped owner, Pattern C bypass, per-stage decision telemetry — emitted on every operation |
| **Data time** | Stale ancestor chains in resource ACLs are guaranteed to fail closed (regression test locks the property) |

Run the migration find/replace pass, optionally call `ValidateAuthorizationConfiguration()`
at startup, and your app inherits the full posture without further changes.

---

## Breaking Changes — Find/Replace Table

### Authorization vocabulary

| v3.x | v4.0 | Notes |
|---|---|---|
| `[RequiresPermission(...)]` | `[RequiresGrant(...)]` | Attribute renamed to reflect Stage 1 grant-only enforcement; ctor argument unchanged (operation-only or feature+operation form) |
| `RequiresPermissionAttribute` | `RequiresGrantAttribute` | Class rename |
| `RequiredPermissionCache` | `RequiredGrantCache` | Class rename |
| `RequiredPermissionCache.ResolveDomainNamespace` | `RequiredGrantCache.ResolveDomainFeature` | Method rename — clearer about what's resolved |
| `IOperationGrantCacheInvalidator.InvalidateDomainAsync(domainFeature, ...)` | `IOperationGrantCacheInvalidator.InvalidateFeatureAsync(feature, ...)` | Method, parameter type, and parameter name all aligned with the framework-wide "feature" vocabulary |
| `OperationGrantCacheKeys.DomainTag()` | `OperationGrantCacheKeys.FeatureTag()` | Internal helper rename for consistency with the public API |
| `OperationGrantCacheDomainOverride` (class) | `OperationGrantCacheFeatureOverride` | Settings record rename |
| `OperationGrantCacheSettings.DomainOverrides` (property) | `OperationGrantCacheSettings.FeatureOverrides` | Property rename — **also affects `appsettings.json`**: rename the JSON key from `"DomainOverrides"` to `"FeatureOverrides"` under `Cirreum:Authorization:Grants:Cache` |
| `GrantedResources.GrantDomainCount` (analyzer metric key) | `GrantedResources.GrantFeatureCount` | Metric key rename — adopters with dashboards or reports filtering on this key need to update queries |
| `GrantedResources.CrossDomainPermissionCount` (analyzer metric key) | `GrantedResources.CrossFeaturePermissionCount` | Metric key rename — same dashboard-update note as above |
| `RequiresGrantAttribute(Permission)` | _(removed)_ | The third ctor was reachable only via reflection — `Permission` instances aren't compile-time constants and could not be used at attribute call sites. Use the operation-only or feature+operation form. |
| `RequiresGrantAttribute.UnresolvedName` | `RequiresGrantAttribute.UnresolvedOperation` | Internal property; only relevant if you wrote custom analyzers |
| `RequiresGrantAttribute.NeedsNamespaceResolution` | `RequiresGrantAttribute.NeedsFeatureResolution` | Internal property; same as above |
| `AuthorizationContext<T>.Permissions` | `AuthorizationContext<T>.RequiredGrants` | Property renamed — same `PermissionSet` value, clearer that it's the *declared* requirements, not caller-held permissions or ACE permissions |
| `AuthorizerBase<T>.WhenPermission` | `AuthorizerBase<T>.WhenRequiresGrant` | Six `When/Unless*Permission*` helpers renamed to match `[RequiresGrant]` declaration |
| `AuthorizerBase<T>.WhenAnyPermission` | `AuthorizerBase<T>.WhenRequiresAnyGrant` | |
| `AuthorizerBase<T>.WhenAllPermissions` | `AuthorizerBase<T>.WhenRequiresAllGrants` | |
| `AuthorizerBase<T>.UnlessPermission` | `AuthorizerBase<T>.UnlessRequiresGrant` | |
| `AuthorizerBase<T>.UnlessAnyPermission` | `AuthorizerBase<T>.UnlessRequiresAnyGrant` | |
| `AuthorizerBase<T>.UnlessAllPermissions` | `AuthorizerBase<T>.UnlessRequiresAllGrants` | |

### Operation contracts

| v3.x | v4.0 | Notes |
|---|---|---|
| `ICacheableQuery` | `ICacheableOperation` | Non-generic marker rename; aligns with Cirreum's CQRS-neutral operation vocabulary |
| `ICacheableQuery<TResultValue>` | `ICacheableOperation<TResultValue>` | Generic interface rename — `TResultValue` parameter unchanged |
| _(none)_ | `IOwnerCacheableLookupOperation<T>` is the **only** safe combination of caching + grant interfaces | New analyzer rule (see below) flags any other combination as Error |

**Not affected:**
- `QueryCaching<,>` intercept — internal pipeline component, name unchanged
- `QueryOverrides` cache settings — name unchanged
- `CacheConsumers.QueryCaching` constant — name unchanged
- `Cirreum.QueryCache.Hybrid` / `Cirreum.QueryCache.Distributed` package names — unchanged (these are about caching *services*, not operation contracts)
- `ResourceTypeInfo.IsCacheableQuery` / `ResourceInfo.IsCacheableQuery` — kept as wire-format property names for snapshot serialization stability

### Cache wire-format changes

These are wire-format breaks for any external code that scrapes Cirreum cache tags:

| v3.x cache tag | v4.0 cache tag | Used by |
|---|---|---|
| `grant:domain:{feature}` | `grant:feature:{feature}` | `OperationGrantCacheKeys.FeatureTag()` — cache invalidation by feature |
| `cirreum.authz.grant.domain` (OTel) | `cirreum.authz.grant.feature` (OTel) | `AuthorizationTelemetry.GrantFeatureTag` — emitted on grant resolution telemetry. Update dashboard queries that filter by this tag |
| `tenant:{OwnerId}` | `owner:{OwnerId}` | `IOwnerCacheableLookupOperation<T>` — auto-added invalidation tag for owner-scoped cacheable lookups |

If you have external infrastructure (Redis monitoring scripts, custom invalidation
producers, dashboard queries) that key on these prefixes, update them.

### Removed APIs

| Removed | Replacement |
|---|---|
| `PermissionSet.ToSignature()` | `OperationGrantCacheKeys.SignatureOf(PermissionSet)` (internal). The signature was unsafe as a general-purpose hash because it omitted feature names; it's now scoped to the grant cache where the surrounding key already carries the feature. If you used `ToSignature()` for your own cache keys, write your own helper that includes feature names. |

---

## New Capabilities

### `[RequiresGrant]` semantics — verb / data split now explicit

The attribute name describes the gating mechanism (the grant pipeline); the argument
is the permission to be granted. XML doc rewritten to lead with this framing —
adopters reading the attribute now understand "you supply a permission; the grant
pipeline does the enforcing."

### Compile-time: `GrantedResourceAnalyzer` rule for unsafe cacheable+grant combinations

New rule with **Error** severity flags any granted operation that mixes
`ICacheableOperation<T>` with a grant interface other than the framework-safe
`IOwnerCacheableLookupOperation<T>`. Shared cache entries cannot safely span
callers with different grant scopes; this combination would leak data between
callers. Detected at startup by introspection.

The previous "Unused Domains" rule was removed — it incorrectly flagged any
domain namespace that didn't use grants as suspect. Not every domain needs grant-based
access control; the rule was noise that drowned real signal.

### Boot-time: `IServiceProvider.ValidateAuthorizationConfiguration()`

```csharp
var app = builder.Build();
app.Services.ValidateAuthorizationConfiguration();   // throws on Error severity
app.Run();
```

Runs every registered `IDomainAnalyzer` against the live DI container and the scanned
domain model. Findings at `IssueSeverity.Error` throw `AuthorizationConfigurationException`
with the full `AnalysisReport` attached. The exception's message is human-readable —
each error appears with category, description, recommendation, and affected types — so
the stack trace alone tells the developer exactly what's wrong.

For callers who want to log/branch instead of throw:

```csharp
var report = app.Services.CheckAuthorizationConfiguration();
if (report is not null) { /* handle */ }
```

Cross-platform — works in ASP.NET Core, Functions, WASM bootstraps, and console hosts.

### Request-time: three new OTel signals

| Tag | When emitted | What it tells you |
|---|---|---|
| `cirreum.authz.grant.owner_auto_stamped` | Stage 1 inferred `OwnerId` from a single-owner grant rather than the caller supplying it | Distinguish framework-inferred owner intent from caller-explicit choice in audit traces |
| `cirreum.authz.grant.pattern_c_bypass` | A Pattern C lookup completed without the handler reading `IOperationGrantAccessor.Current` | The handler returned data without performing the post-fetch ownership check — a real bypass surface |
| (already shipped — now documented) `cirreum.authz.decision` per-stage | Every authorization decision (Stage 1/2/3, pass/deny, reason) | Full pipeline decision trail |

The Pattern C signal also emits a structured warning log via `LoggerMessage`
(EventId: 9001, message naming the operation type) — wire into your dashboards and
alerting so a forgotten `grant.Contains(...)` call surfaces in dev/CI before a real
bypass ships.

### Request-time: `IOperationGrantAccessor` now exposes outcome signals

```csharp
public interface IOperationGrantAccessor {
    OperationGrant Current { get; }
    bool OwnerWasAutoStamped { get; }   // NEW — runtime-readable signal
    bool WasRead { get; }                // NEW — used by Pattern C audit
    void Set(OperationGrant grant);
    void MarkOwnerAutoStamped();         // NEW — called by Stage 1
}
```

Reading `Current` sets `WasRead = true` as a side effect. Stage 2 authorizers,
Stage 3 policies, and handlers can all branch on these signals at runtime — e.g., a
sensitive-write authorizer can deny if `OwnerWasAutoStamped` and require explicit
caller intent.

### Data-time: stale ancestor regression test

Added `Batch_path_does_not_over_grant_when_inheritance_breaking_ancestor_is_stale` to
`ResourceAccessTests`. Locks in the property that when an ancestor in
`AncestorResourceIds` is missing from the store (stale chain), the walk fails closed
— the framework cannot inspect a missing entity, so it must treat the orphan as a
hard stop. Prevents over-grants from un-cascaded deletes.

### Compliance Boundary documentation

New section in `Authorization/README.md` delineating what the framework guarantees
vs. what remains application responsibility, with file references for every framework
guarantee and explicit ownership statements for every app responsibility. Maps to
NIST SP 800-53 AC-3 / AC-6 / AU-2, NIST SP 800-162 (ABAC), OWASP ASVS V4 Access
Control, OWASP Top 10 #1, ISO/IEC 27001 A.9.4.1. This is the document adopters point
at when their compliance team asks for the security model.

### Consistency Model documentation

New section in `Operations/Grants/README.md` documenting the three consistency
surfaces (bypass live, grants eventually consistent up to TTL, Pattern C strong),
revocation invariants, TTL trade-offs, cache key hygiene, and three escape hatches
for domains needing strong consistency (disable L2, aggressive invalidation, layer a
ReBAC store).

### Authorization user guide

New `Authorization/README.md` is the canonical map — pipeline overview, decision
table for picking the right tool (grants / constraints / authorizers / policies /
ACLs), core types, inline examples for each stage, permission model, namespace-derived
feature resolution, discovery surface, DI cheat-sheet. Sub-readmes (Grants, Resources,
FLOW, SEQUENCE) now point at this as "read first" rather than re-litigating the model.

### Export formatters: Runtime Signals & Compliance appendix

`AnalysisReport.ToMarkdown()`, `ToText()`, and `ToHtml()` now append a "Runtime
Signals & Compliance" section that lists the three OTel tags adopters should
observe (`cirreum.authz.decision`, `cirreum.authz.grant.owner_auto_stamped`,
`cirreum.authz.grant.pattern_c_bypass`), points at the Compliance Boundary
documentation for standards mapping, and reminds readers about the
`ValidateAuthorizationConfiguration()` boot-time hook. Compliance teams reading
exported reports now get the runtime/static distinction and the standards mapping
without having to ask. CSV is unchanged (tabular data, not narrative).

---

## Migration Walkthrough

A typical adopter migration is ~10 minutes for the find/replace, plus optional 1 line
for boot-time validation.

### Step 1 — Find/replace across your codebase

Run the table above as find/replace operations. The compiler will catch anything
missed; there are no semantic changes hidden in the renames.

### Step 2 — Update cache tag consumers (if any)

If you have Redis monitoring, custom invalidation producers, or dashboard queries
that key on the cache tag prefixes, update:

- `grant:domain:` → `grant:feature:`
- `tenant:` → `owner:` (auto-tag added by `IOwnerCacheableLookupOperation<T>`)

### Step 3 — Add boot-time validation (recommended)

```csharp
var app = builder.Build();
app.Services.ValidateAuthorizationConfiguration();
app.Run();
```

This converts the analyzer's Error severities into a startup failure. Treats
misconfiguration as a build break instead of a runtime surprise.

### Step 4 — Wire OTel signals into dashboards (optional)

The three new tags (`owner_auto_stamped`, `pattern_c_bypass`, plus the existing
`cirreum.authz.decision`) flow through the standard `Activity` model. If your collector
is already wired (it should be — Cirreum has emitted decision telemetry since v3),
they show up automatically. Recommended dashboards:

- **Pattern C bypass count** (last 24h) — should be zero. Anything else is a missing
  handler check.
- **Auto-stamped writes** (last 24h, by operation type) — useful for understanding
  how often callers omit `OwnerId` and the framework is inferring it.
- **Stage deny count by reason** — already available, now better grouped with the
  new tags as siblings.

### Step 5 — Treat new analyzer Errors as build breaks

The new "unsafe cacheable + grant" rule will fire on any operation combining
`ICacheableOperation<T>` with a grant interface outside `IOwnerCacheableLookupOperation<T>`.
If you have such combinations:

- If owner-scoped caching was intended → switch to `IOwnerCacheableLookupOperation<T>`
- Otherwise → drop `ICacheableOperation<T>`; per-caller authorization decisions cannot
  share a cache entry safely

### Step 6 — Audit Pattern C lookup handlers

For every handler implementing `IOperationHandler<TOp, TResult>` where `TOp : IGrantableLookupBase`:

1. Inject `IOperationGrantAccessor`
2. After loading the entity, check `grant.Contains(entity.OwnerId)`
3. Return `404` (not `403`) on miss — preserves existence-hiding

The `GrantedLookupAudit<,>` intercept will emit a warning log + OTel tag for any
handler that doesn't follow this pattern. Run your test suite, watch for the
warnings, fix any that fire.

---

## Standards Mapping

| Control / standard | v3 status | v4 status |
|---|---|---|
| NIST SP 800-53 **AC-3** (Access Enforcement) | Enforced | Enforced — runtime backstop in `DefaultAuthorizationEvaluator` denies on incomplete graph |
| NIST SP 800-53 **AC-6** (Least Privilege) | Supported | **Enforced at the framework boundary** + analyzer-driven app-side hardening; Pattern C runtime audit closes the highest-risk surface |
| NIST SP 800-53 **AU-2 / AU-12** (Audit Events) | Supported | Supported + new `owner_auto_stamped` and `pattern_c_bypass` tags |
| NIST SP 800-162 (ABAC) | Aligned | Aligned (no changes) |
| OWASP ASVS V4 (Access Control) | Aligned at framework boundary | Aligned + Pattern C residual risk now has a runtime detection signal |
| OWASP Top 10 #1 (Broken Access Control) | Pattern C residual | **Pattern C now monitored at runtime** — bypasses surface as warnings + dashboard signals before reaching production |
| ISO/IEC 27001 A.9.4.1 | Supported | Supported (no changes) |

See `Authorization/README.md#compliance-boundary` for the full statement of what
the framework enforces vs. what remains application responsibility.

---

## Downstream Package Impact

| Package | Impact |
|---|---|
| `Cirreum.QueryCache.Hybrid` | XML doc cref text updated (`ICacheableQuery` → `ICacheableOperation`); no behavior change |
| `Cirreum.QueryCache.Distributed` | XML doc cref text updated; no behavior change |
| Apps depending on `Cirreum.Core` | Mechanical find/replace per the table above; ~10 minute migration |

Class names like `HybridCacheableQueryService` and `DistributedCacheableQueryService`
are kept — they describe the cache *service*, not the operation contract.

---

## What Didn't Change

- `Permission` — kept general; not tied to grants
- `PermissionSet` — kept general; the `ToSignature()` method moved out (was a footgun
  for callers other than the grant cache)
- `AccessEntry` (ACE) — its `Permissions` property is the legitimate permission list
  on an ACL entry, not affected by the `RequiredGrants` rename on context
- `IResourceAccessEvaluator` API surface — `CheckAsync`, `FilterAsync`, etc. unchanged
- `OperationGrant` shape (Denied / Unrestricted / Bounded) and semantics
- The three-stage pipeline structure
- `IOperation` / `IOperation<T>` (Cirreum stays CQRS-neutral; apps layer their own
  `ICommand`/`IQuery` markers if they want)

---

## Looking Ahead (not in this release)

- **Hosted-service wrapper for `ValidateAuthorizationConfiguration()`** — likely
  shipped in `Cirreum.Runtime.*` so adopters get boot-time enforcement implicitly via
  package reference, no `Program.cs` boilerplate needed
- **Component-library dashboards** (Blazor + React) — render the introspection
  snapshot as a security-posture UI; current HTML export covers the same ground
- **Static (Roslyn) analyzer for handler-side concerns** — e.g., flag
  `IGrantableLookupBase` handlers at compile time if they don't inject
  `IOperationGrantAccessor`. Requires a separate analyzer package.
- **Wave 4 hardening** — additional regression tests around constraint ordering,
  policy filtering correctness, role-registry inheritance cycles
