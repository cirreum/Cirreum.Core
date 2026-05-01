# Cirreum.Core v5.0.0 — Migration Guide

> **From:** v4.x &nbsp;•&nbsp; **To:** v5.0.0
>
> Migrating from v3 → v4? See [MIGRATION-v4.md](MIGRATION-v4.md) instead — that
> guide covers a different transition and is unrelated to the v5 changes here.

## Why v5

This release adds first-class support for multi-IdP server hosts: a single API
host that fans in across a workforce IdP, one or more customer IdPs, and machine
credentials (`ApiKey`, `SignedRequest`, `External` BYOID), with a separate
application user store per non-operator IdP.

Pre-v5, `IApplicationUserResolver` had no way to declare which authentication
scheme it serviced. A multi-portal app forced the single registered resolver to
either branch on issuer/claim shape internally, or fan out lookups across two or
three user stores per request and pick the first hit. v5 lets each resolver
declare its scheme; the adapter that already runs during claims transformation
reads the scheme from `HttpContext.Items` (set by the dynamic scheme forward
selector during auth dispatch) and picks the matching resolver.

The release also centralizes the well-known `HttpContext.Items` keys used to
coordinate authentication context across pipeline stages. The two consts that
held those keys lived on per-interface types whose homes no longer reflected
their actual coupling — both keys are read and written by multiple subsystems
beyond the interface that introduced them.

Migration is mechanical: two `HttpContext.Items` key references, optional `Scheme`
declarations on existing resolvers if you're moving to multi-IdP. ~5 minutes for
the find/replace, plus optional resolver work.

---

## Breaking Changes — Find/Replace Table

### `HttpContext.Items` coordination keys

| v4.x | v5.0 | Notes |
|---|---|---|
| `IAuthenticationBoundaryResolver.ResolvedSchemeKey` | `AuthenticationContextKeys.AuthenticatedScheme` | Const moved off the per-interface type to a dedicated `Cirreum.Security.AuthenticationContextKeys` static class. Naming also tightened — past-participle names what the value is (the scheme that authenticated this request) rather than describing it as merely "resolved." |
| `IApplicationUserResolver.CacheKey` | `AuthenticationContextKeys.ApplicationUserCache` | Same relocation rationale — the key is read by `UserAccessor`, not just the resolver itself. |

Both consts are removed outright — no `[Obsolete]` aliases. Compile errors at
the read/write sites are the intended SemVer signal.

### `HttpContext.Items` literal value change

The string literal stamped under the new `AuthenticatedScheme` key changed from
`"__Cirreum_ResolvedAuthScheme"` (v4.x) to `"__Cirreum_AuthenticatedScheme"`
(v5.0). This affects:

- **Anyone reading the dictionary by raw string literal** rather than through the
  const — they will read `null` post-upgrade because the new writer side stamps
  the new literal. Switch to `AuthenticationContextKeys.AuthenticatedScheme` and
  the value flows through correctly.
- **The duplicated literal in `Cirreum.Runtime.AuthorizationProvider`** — that
  package can't reference `Cirreum.Core` (intended to be standalone in
  non-Cirreum hosts). It duplicates the literal locally with a comment pointing
  at the canonical const. Updated alongside this release to match the new value.

### Removed APIs

| Removed | Replacement |
|---|---|
| `IAuthenticationBoundaryResolver.ResolvedSchemeKey` | `AuthenticationContextKeys.AuthenticatedScheme` |
| `IApplicationUserResolver.CacheKey` | `AuthenticationContextKeys.ApplicationUserCache` |

---

## New Capabilities

### `IApplicationUserResolver.Scheme`

```csharp
public interface IApplicationUserResolver {
    string? Scheme => null;   // default: fallback resolver
    Task<IApplicationUser?> ResolveAsync(string externalUserId, CancellationToken ct = default);
}
```

Declares which authentication scheme the resolver handles:

- `null` (the default via default interface implementation) — fallback resolver
  matched when no exact-scheme resolver is registered. **Existing implementers
  compile and behave unchanged.**
- Non-null — resolver only handles requests authenticated under the named scheme.

Singular by design. Enforces 1:1 scheme→resolver→store mapping. Apps that
genuinely need to share a single store across multiple schemes own their own
discriminator (one resolver, internal namespacing).

### `Cirreum.Security.AuthenticationContextKeys`

New public static class holding the well-known `HttpContext.Items` keys used to
coordinate authentication context across pipeline stages:

```csharp
namespace Cirreum.Security;

public static class AuthenticationContextKeys {
    public const string AuthenticatedScheme  = "__Cirreum_AuthenticatedScheme";
    public const string ApplicationUserCache = "__Cirreum_ApplicationUser";
}
```

| Key | Purpose | Writers | Readers |
|---|---|---|---|
| `AuthenticatedScheme` | The scheme that authenticated the request | `Cirreum.Runtime.Authorization`'s dynamic scheme forward selector; `AudienceProviderRoleClaimsTransformer` (defensive `TryAdd` for explicit-scheme routes) | `IAuthenticationBoundaryResolver` consumers (e.g. `UserAccessor`); the per-scheme `IApplicationUserResolver` dispatcher |
| `ApplicationUserCache` | The resolved `IApplicationUser` for the request | `ApplicationUserRoleResolverAdapter` during role enrichment | `UserAccessor` (steady-state cache hit) |

---

## Migration Walkthrough

A typical adopter migration is ~5 minutes for the find/replace, plus optional
resolver work if you're moving to multi-IdP.

### Step 1 — Update `HttpContext.Items` key references

```diff
- context.Items[IAuthenticationBoundaryResolver.ResolvedSchemeKey]
- context.Items[IApplicationUserResolver.CacheKey]
+ context.Items[AuthenticationContextKeys.AuthenticatedScheme]
+ context.Items[AuthenticationContextKeys.ApplicationUserCache]
```

Add `using Cirreum.Security;` if your file doesn't already import it. The
compiler will catch any missed sites.

### Step 2 — Existing single-IdP apps: no resolver changes required

The default interface implementation of `Scheme` returns `null`, so existing
single-resolver apps compile and behave identically — the resolver becomes the
null-scheme fallback. You can stop here if you're not adopting multi-IdP.

### Step 3 — Multi-IdP server hosts: declare `Scheme` on each resolver

```csharp
public sealed class CustomerEntraResolver : IApplicationUserResolver {
    public string Scheme => "EntraExternalId";   // matches the configured scheme name
    public Task<IApplicationUser?> ResolveAsync(string externalUserId, CancellationToken ct = default) { ... }
}

public sealed class BorrowerDescopeResolver : IApplicationUserResolver {
    public string Scheme => "Descope";
    public Task<IApplicationUser?> ResolveAsync(string externalUserId, CancellationToken ct = default) { ... }
}
```

The `Scheme` value must match the configured authentication scheme name (the
same name your `appsettings.json` uses under
`Cirreum:Authorization:Providers:{Type}:Instances:{key}`).

Register each resolver via the existing `AddApplicationUserResolver<T>()`
extensions. The matching `Cirreum.Runtime.Authorization` and
`Cirreum.Runtime.Wasm` releases switch their registration internals from
`TryAddScoped` to `AddScoped` so multiple resolvers can register side by side.
The dispatcher in `ApplicationUserRoleResolverAdapter` (server) and
`InitializationOrchestrator` (WASM) selects the matching one per request based
on the authenticated scheme.

### Step 4 — Operator/machine-track schemes need no resolver

Workforce/operator schemes never had an application user record and don't get
one in v5. Don't register a resolver for those schemes. The dispatcher's "no
scheme match, no `null`-scheme fallback" path is the correct outcome —
`IApplicationUser` stays `null`, the grant evaluator's existing null-fall-through
accommodates it, and authority for operator callers continues to flow from token
claims.

The same applies to `ApiKey`, `SignedRequest`, and `External` BYOID schemes —
they authenticate partners/integrations, not human users, and don't need an
`IApplicationUserResolver`.

### Step 5 — Update any third-party code that read the dictionary by literal

If you have middleware, telemetry, or test harnesses that read
`HttpContext.Items["__Cirreum_ResolvedAuthScheme"]` (the old literal) by raw
string, switch to `AuthenticationContextKeys.AuthenticatedScheme`. The new
writer side stamps `"__Cirreum_AuthenticatedScheme"`; the old literal will read
`null` post-upgrade.

---

## What Didn't Change

- The `IApplicationUserResolver.ResolveAsync(string externalUserId, ...)` signature.
- The grant evaluator's null-fall-through pattern for callers without an
  application user record (operator and machine tracks). Existing operator-track
  apps work unchanged in v5.
- `IUserState.ApplicationUser` semantics — still nullable, still expected to be
  `null` for operator/machine-track callers.
- `IAuthenticationBoundaryResolver.Resolve(...)` API surface and the
  `Global` / `Tenant` / `None` boundary model.
- `AuthorizationContext.ApplicationUser` — still typed `IApplicationUser?` with
  the same documented "or null when no app-db record backs this caller"
  semantics.
- Cache hit/miss flow in `UserAccessor`. Steady state for tenant requests is
  unchanged: the adapter resolves once during claims transformation, stashes on
  `Items`, `UserAccessor` reads the cache on first `GetUser()` call. The
  cache-miss fallback path is the only place dispatch logic was added.

---

## Doc-only fix

`IUserState.Id` XML remarks no longer call `Id` the "primary key for
application-user resolution" (which conflated the IdP's stable external
identifier with the application user's database primary key). Now correctly
describes `Id` as the external identifier passed to
`IApplicationUserResolver.ResolveAsync`, with grant cache keys and audit trails
as secondary uses. No code change.

---

## Downstream Package Impact

| Package | Impact |
|---|---|
| `Cirreum.Runtime.AuthorizationProvider` | `AudienceProviderRoleClaimsTransformer` defensively stamps `AuthenticatedScheme` on `HttpContext.Items` for routes wired to an explicit scheme that bypass the dynamic forward selector. Updates the duplicated literal to the new value. |
| `Cirreum.Runtime.Authorization` | Adapter dispatches by scheme via `IEnumerable<IApplicationUserResolver>`. `CirreumAuthorizationBuilder.AddApplicationUserResolver` switches to `AddScoped` so N resolvers can register. Forward selector uses `AuthenticationContextKeys.AuthenticatedScheme`. |
| `Cirreum.Services.Server` | `UserAccessor` reads the new const for both boundary resolution and the cache-miss application user fallback path. |
| `Cirreum.Runtime.Wasm` | `InitializationOrchestrator` selects the matching resolver at `Start()` once the authenticated identity is known. `AppRouteView` registration probe uses `GetServices<>().Any()`. `HostingExtensions.AddApplicationUserResolver` switches to `AddScoped`. |
| `Cirreum.Runtime.Wasm.Oidc` / `Cirreum.Runtime.Wasm.Msal` | No source changes — per-builder wrappers delegate to the updated `HostingExtensions` and inherit the new behavior. Dependency floor bump only. |

---

## Looking Ahead (not in this release)

- **Startup validation for duplicate non-null `Scheme` values** — currently the
  dispatcher silently picks the first match. A future release may add a fail-fast
  check at app start to reject duplicate scheme registrations.
- **Boundary-aware `AppRouteView` state machine** — the `NotProvisioned` /
  `Disabled` view states currently fire whenever `_requiresApplicationUser` is
  true and `ApplicationUser is null`. A future release may gate them on
  non-operator boundary so operator-boundary callers transition straight to
  `Ready` regardless of resolver registration. Cosmetic for single-track apps,
  load-bearing for multi-track ones.
- **Doc cleanup of `FLOW.md` / `README.md` overstated claims** — the line
  "Authority comes from the app-user layer (`IOwnedApplicationUser`)" is true
  for the tenant track but overstated for operator and machine tracks. A future
  doc-only release will tighten this.
