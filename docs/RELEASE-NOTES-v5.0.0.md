# Cirreum.Core 5.0.0 — Per-scheme application user resolution

Multi-IdP server hosts can now register one `IApplicationUserResolver` per
authentication scheme and dispatch by the request's authenticated scheme,
rather than packing scheme-detection gymnastics inside a single resolver or
fanning out queries across user stores.

The release also centralizes the well-known `HttpContext.Items` keys used to
coordinate authentication context across pipeline stages, removing two
per-interface consts whose homes no longer reflected their actual coupling.

---

## Why this release exists

A typical Cirreum server fans in across multiple identity surfaces in the
same host:

- Workforce IdP (Entra) — operator/staff principals; no application user
  record (claims-only authority).
- Customer IdP(s) (Entra External ID, Descope, generic OIDC) — tenant
  principals backed by an application user store.
- Machine credentials (`ApiKey`, `SignedRequest`, `External` BYOID) —
  partner/integration callers; usually no application user record.

Pre-`5.0.0`, `IApplicationUserResolver` had no way to declare which
authentication scheme it serviced. A multi-portal app (e.g. Internal portal
on workforce, External portal on customer Entra, Borrower portal on Descope)
forced the single registered resolver to either branch on issuer/claim
shape internally, or fan out lookups across two or three user stores per
request and pick the first hit. Both options are wrong: one couples the
resolver to issuer-detection plumbing it shouldn't know about; the other
spends N database round-trips when one would do.

The fix is small and surface-honest: the resolver declares its scheme; the
adapter that already runs during claims transformation reads the scheme
from `HttpContext.Items` (set by the dynamic scheme forward selector during
auth dispatch) and picks the matching resolver.

---

## What's new

### `IApplicationUserResolver.Scheme`

```csharp
public interface IApplicationUserResolver {
    string? Scheme => null;   // default fallback
    Task<IApplicationUser?> ResolveAsync(string externalUserId, CancellationToken ct = default);
}
```

- `null` (the default) → fallback resolver matched when no exact-scheme
  resolver is registered. Existing implementers compile unchanged and
  behave as the fallback.
- Non-null → declares the resolver only handles the named authentication
  scheme.

Singular by design. Enforces a 1:1 scheme→resolver→store mapping by
construction. Apps that genuinely need to share a single store across
schemes own their own discriminator (one resolver, internal namespacing).

### `Cirreum.Security.AuthenticationContextKeys`

A new public static class holding the well-known `HttpContext.Items` keys
used to coordinate authentication context across pipeline stages:

```csharp
public static class AuthenticationContextKeys {
    public const string AuthenticatedScheme  = "__Cirreum_AuthenticatedScheme";
    public const string ApplicationUserCache = "__Cirreum_ApplicationUser";
}
```

| Key | Purpose | Writers | Readers |
|---|---|---|---|
| `AuthenticatedScheme` | The scheme that authenticated the request | `Cirreum.Runtime.Authorization`'s dynamic scheme forward selector; `AudienceProviderRoleClaimsTransformer` (defensive `TryAdd` for explicit-scheme routes) | `IAuthenticationBoundaryResolver` consumers (e.g. `UserAccessor`); the per-scheme `IApplicationUserResolver` dispatcher |
| `ApplicationUserCache` | The resolved `IApplicationUser` for the request | `ApplicationUserRoleResolverAdapter` during role enrichment | `UserAccessor` (steady-state cache hit) |

Both keys were previously consts on the per-interface types that originally
read them. They've been promoted to a dedicated static class because each
key is now read and written by multiple subsystems beyond the interface
that introduced it — the previous home was misleading about the coupling.

---

## What's removed (breaking)

| Removed | Replacement |
|---|---|
| `IAuthenticationBoundaryResolver.ResolvedSchemeKey` | `AuthenticationContextKeys.AuthenticatedScheme` |
| `IApplicationUserResolver.CacheKey` | `AuthenticationContextKeys.ApplicationUserCache` |

Both consts are removed outright — no `[Obsolete]` aliases. The string
**values** changed too: the old `"__Cirreum_ResolvedAuthScheme"` literal is
now `"__Cirreum_AuthenticatedScheme"` to match the new const name.

Any external consumer that read or wrote these keys directly will break at
compile time, which is exactly the SemVer signal you want for a contract
change of this kind.

---

## What's fixed

### `IUserState.Id` XML doc

The remarks called `Id` the "primary key for application-user resolution,"
conflating the IdP's stable external identifier with the application user's
database primary key. Reworded to describe `Id` as the external identifier
passed to `IApplicationUserResolver.ResolveAsync` — the application's data
store decides its own primary key.

---

## Architectural principle

> **Authentication context coordination keys live on a dedicated holder, not
> on the domain interfaces that happen to read them.**

The previous shape (`IAuthenticationBoundaryResolver.ResolvedSchemeKey`,
`IApplicationUserResolver.CacheKey`) implied each key was a private
implementation detail of one consumer. In reality both keys are
cross-pipeline contracts: written by one stage, read by several others.
Centralizing them on `AuthenticationContextKeys` makes the contract
explicit and gives downstream subsystems a single place to look.

The same principle applies to the duplicated string literal in
`Cirreum.Runtime.AuthorizationProvider`: that package can't reference
`Cirreum.Core` (it's intended to be standalone in non-Cirreum hosts), so it
duplicates the literal with a comment pointing at the canonical location.
The literal value is a stable contract; treat changes as breaking.

---

## Migration

### 1. Update `HttpContext.Items` key references

```diff
- context.Items[IAuthenticationBoundaryResolver.ResolvedSchemeKey]
- context.Items[IApplicationUserResolver.CacheKey]
+ context.Items[AuthenticationContextKeys.AuthenticatedScheme]
+ context.Items[AuthenticationContextKeys.ApplicationUserCache]
```

Add `using Cirreum.Security;` if your file doesn't already import it.

### 2. Existing `IApplicationUserResolver` implementations — no change required

Default interface implementation of `Scheme` returns `null`, so existing
single-resolver apps compile and behave identically (the resolver becomes
the null-scheme fallback).

### 3. Multi-IdP server hosts — declare schemes on resolvers

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

Register each resolver. The matching `Cirreum.Runtime.Authorization` and
`Cirreum.Runtime.Wasm` releases switch their registration extensions from
`TryAddScoped` to `AddScoped` so multiple resolvers can register
side-by-side; the dispatcher in `ApplicationUserRoleResolverAdapter` (server)
and `InitializationOrchestrator` (WASM) selects the matching one per
request based on the authenticated scheme.

### 4. Operator/machine-track requests need no resolver

Workforce/operator schemes never had an application user record and don't
get one now. The dispatcher's "no scheme match, no `null`-scheme fallback"
path is the correct outcome — `IApplicationUser` stays `null`, the grant
evaluator's existing null-fall-through accommodates it, and authority for
operator callers continues to flow from token claims.

---

## Coordinated downstream releases

The constants were read and written outside `Cirreum.Core` itself.
Coordinated updates in matching releases:

- `Cirreum.Runtime.AuthorizationProvider` — `AudienceProviderRoleClaimsTransformer`
  defensively stamps the new key for explicit-scheme routes that bypass the
  forward selector.
- `Cirreum.Runtime.Authorization` — `ApplicationUserRoleResolverAdapter`
  injects `IEnumerable<IApplicationUserResolver>` and dispatches by scheme;
  `CirreumAuthorizationBuilder.AddApplicationUserResolver` switches to
  `AddScoped` so multiple resolvers can register; forward selector uses the
  new const.
- `Cirreum.Services.Server` — `UserAccessor` reads the new const for both
  the boundary resolver dispatch and the cache-miss application user
  fallback.
- `Cirreum.Runtime.Wasm` — `InitializationOrchestrator` selects the
  matching resolver at `Start` time once the authenticated identity is
  known; `AppRouteView` registration probe uses
  `GetServices<>().Any()`; `HostingExtensions.AddApplicationUserResolver`
  switches to `AddScoped`.

The per-builder wrappers in `Cirreum.Runtime.Wasm.Oidc` and
`Cirreum.Runtime.Wasm.Msal` need no source changes; they delegate down to
the updated `HostingExtensions` and inherit the new behavior.

---

## Compatibility

- No type forwarders. The const removal is a clean source break — consumers
  see compile errors at the read/write sites and migrate to
  `AuthenticationContextKeys`.
- `IApplicationUserResolver.Scheme` is additive (default interface
  implementation); existing implementers remain source-compatible without
  edits.
- The duplicated string literal in `Cirreum.Runtime.AuthorizationProvider`
  changed value alongside the renamed const. Anyone reading `HttpContext.Items`
  for the legacy `"__Cirreum_ResolvedAuthScheme"` literal directly (rather
  than through the const) will read `null` post-upgrade — the new writer
  side stamps `"__Cirreum_AuthenticatedScheme"`.

## See also

- `CHANGELOG.md` entry for `5.0.0` for the condensed change list.
- The downstream package READMEs and changelogs for the coordinated
  updates that ship alongside this release.
