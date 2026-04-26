# Cirreum.Core v3 Migration Guide

> **From:** v2.x &nbsp;•&nbsp; **To:** v3.x
>
> Migrating from v3 → v4? See [MIGRATION-v4.md](MIGRATION-v4.md) instead — v4 is
> a separate release that introduces grant-vocabulary clarity and runtime security
> signals on top of v3.

Cirreum.Core v3 is a terminology and architectural clarity release. The runtime behavior is unchanged — all breaking changes are **rename-only** and can be resolved with find-and-replace. No logic changes, no behavioral differences, no data migration.

## Why v3?

Cirreum.Core's Conductor is a **domain operating engine** — the seam between ASP.NET's HTTP layer and your domain processing pipeline. v2 used mediator-style naming (`IRequest`, `TResponse`, `RequestContext`) that obscured this identity. v3 makes every type say what it means.

---

## Quick Reference — Find and Replace

Most of the migration is mechanical. Run these replacements **in order** across your solution:

### 1. Conductor — Request → Operation

| Find | Replace |
|------|---------|
| `IRequest<` | `IOperation<` |
| `IRequest` (marker) | `IOperation` |
| `IRequestHandler<` | `IOperationHandler<` |
| `RequestContext<` | `OperationContext<` |
| `RequestHandlerDelegate<` | `OperationHandlerDelegate<` |
| `IBaseRequest` | `IBaseOperation` |

### 2. Generic Type Parameters

| Find | Replace |
|------|---------|
| `TResponse` | `TResultValue` |
| `TResource` | `TAuthorizableObject` |

> **Note:** `TResponse` → `TResultValue` only affects code that declares generic type parameters (handler implementations, intercept implementations, custom pipeline extensions). If you're just *using* handlers, the rename is transparent.

### 3. Domain Model

| Find | Replace |
|------|---------|
| `IDomainResource` | `IDomainObject` |
| `IAuthorizableResource` | `IAuthorizableObject` |

### 4. Authorization

| Find | Replace |
|------|---------|
| `IAuthorizationPolicyValidator` | `IPolicyValidator` |
| `AuthorizationValidatorBase<` | `AuthorizerBase<` — see [Authorization Validators](#authorization-validators) |
| `IAuthorizationResourceValidator<` | `IAuthorizer<` — see [Authorization Validators](#authorization-validators) |
| `IScopeEvaluator` | `IAuthorizationConstraint` |
| `AccessScope` | `AuthenticationBoundary` |
| `[RequiresPermission(...)]` | `[RequiresGrant(...)]` — attribute renamed to reflect Stage 1 grant-only enforcement; ctor surface unchanged (operation-only or feature+operation) |
| `RequiresPermissionAttribute` | `RequiresGrantAttribute` |
| `RequiredPermissionCache` | `RequiredGrantCache` |
| `AuthorizationContext<T>.Permissions` | `AuthorizationContext<T>.RequiredGrants` — property renamed; same `PermissionSet` value, clearer that it's the *declared* requirements (not caller-held permissions, not ACE permissions) |
| `RequiredPermissionCache.ResolveDomainNamespace` | `RequiredGrantCache.ResolveDomainFeature` |
| `AuthorizerBase<T>.WhenPermission` | `AuthorizerBase<T>.WhenRequiresGrant` |
| `AuthorizerBase<T>.WhenAnyPermission` | `AuthorizerBase<T>.WhenRequiresAnyGrant` |
| `AuthorizerBase<T>.WhenAllPermissions` | `AuthorizerBase<T>.WhenRequiresAllGrants` |
| `AuthorizerBase<T>.UnlessPermission` | `AuthorizerBase<T>.UnlessRequiresGrant` |
| `AuthorizerBase<T>.UnlessAnyPermission` | `AuthorizerBase<T>.UnlessRequiresAnyGrant` |
| `AuthorizerBase<T>.UnlessAllPermissions` | `AuthorizerBase<T>.UnlessRequiresAllGrants` |

### 5. Caching

| Find | Replace |
|------|---------|
| `ICacheableQueryService` | `ICacheService` |
| `InMemoryCacheableQueryService` | `InMemoryCacheService` |
| `NoCacheQueryService` | `NoCacheService` |
| `QueryCacheSettings` | `CacheExpirationSettings` |
| `ICacheableQuery` / `ICacheableQuery<TResultValue>` | `ICacheableOperation` / `ICacheableOperation<TResultValue>` — interface renamed to align with Cirreum's CQRS-neutral operation vocabulary; `TResultValue` parameter unchanged. The `QueryCaching` intercept and `QueryOverrides` settings keep their existing names |

### 6. Introspection (formerly Authorization.Analysis)

| Find | Replace |
|------|---------|
| `Cirreum.Authorization.Analysis` | `Cirreum.Introspection` |
| `Cirreum.Authorization.Documentation` | `Cirreum.Introspection.Documentation` |
| `IAuthorizationAnalyzer` | `IDomainAnalyzer` |
| `IAuthorizationAnalyzerWithOptions` | `IDomainAnalyzerWithOptions` |
| `IAuthorizationDocumenter` | `IDomainDocumenter` |
| `AuthorizationDocumenter` | `DomainDocumenter` |
| `DefaultAnalyzerProvider` | `DomainAnalyzerProvider` |
| `AuthorizationModel` | `DomainModel` |
| `AuthorizationSnapshot` | `DomainSnapshot` |

---

## Namespace Changes

| Old Namespace | New Namespace |
|---------------|---------------|
| `Cirreum.Conductor.Caching` | `Cirreum.Caching` |
| `Cirreum.Conductor.Configuration` (CacheProvider) | `Cirreum.Caching` |
| `Cirreum.Authorization.Analysis` | `Cirreum.Introspection` |
| `Cirreum.Authorization.Analysis.Analyzers` | `Cirreum.Introspection.Analyzers` |
| `Cirreum.Authorization.Analysis.Formatters` | `Cirreum.Introspection.Documentation.Formatters` |
| `Cirreum.Authorization.Documentation` | `Cirreum.Introspection.Documentation` |
| `Cirreum.Authorization.Analysis.Modeling` | `Cirreum.Introspection.Modeling` |
| `Cirreum.Authorization.Analysis.Modeling.Export` | `Cirreum.Introspection.Modeling.Export` |
| `Cirreum.Authorization.Analysis.Modeling.Types` | `Cirreum.Introspection.Modeling.Types` |

---

## Deleted Types

These types were removed with no direct 1:1 replacement:

| Removed Type | Migration Path |
|--------------|----------------|
| `IDomainRequest` | Use `IOperation` or `IOperation<TResultValue>` directly |
| `IAuditableRequest` | Auditing is now handled via intercepts and telemetry |
| `IAuthorizableRequest` | Authorization is now attribute-driven via `[RequiresGrant]` on operation types, resolved through `IAuthorizableOperation` interfaces |
| `RequestCompletedNotification` | Replaced by `OperationTelemetry` and the notification publisher pipeline |
| `AuthorizationValidatorBase<T>` | Use `AuthorizerBase<TAuthorizableObject>` — same FluentValidation pattern, new base class |
| `IAuthorizationResourceValidator<T>` | Use `IAuthorizer<TAuthorizableObject>` |

---

## Authorization Validators

v2's `AuthorizationValidatorBase<TResource>` and `IAuthorizationResourceValidator<TResource>` have been replaced by a more flexible system:

**Option A — Authorizer (most common):**
```csharp
// v2
public class MyAuthorizer : AuthorizationValidatorBase<MyResource> {
    public MyAuthorizer() {
        RuleFor(ctx => ctx.UserState).Must(...);
    }
}

// v3
public class MyAuthorizer : AuthorizerBase<MyAuthorizableObject> {
    public MyAuthorizer() {
        RuleFor(ctx => ctx.UserState).Must(...);
    }
}
```

**Option B — Policy-based (complex cross-cutting rules):**
```csharp
// v3
public class MyPolicyValidator : IPolicyValidator {
    public string PolicyName => "my-policy";
    public int Order => 100;
    public DomainRuntimeType[] SupportedRuntimeTypes => [...];

    public bool AppliesTo<TAuthorizableObject>(...) => ...;
    public Task<ValidationResult> ValidateAsync<TAuthorizableObject>(...) => ...;
}
```

---

## Caching

The caching subsystem moved from Conductor-specific to a platform-level concern:

```csharp
// v2
public class MyService(ICacheableQueryService cache) { ... }

// v3
public class MyService(ICacheService cache) { ... }
```

The `ICacheService` API uses `GetOrCreateAsync` with `CacheExpirationSettings` instead of the old `QueryCacheSettings`.

v3 also introduces **keyed cache services** for telemetry. The `cirreum.cache.consumer` metric tag identifies which subsystem triggered cache operations. Consumers that inject plain `ICacheService` get tagged as `"other"` — no code changes required.

---

## IIntercept Implementations

If you have custom intercepts, update the signature:

```csharp
// v2
public class MyIntercept<TRequest, TResponse> : IIntercept<TRequest, TResponse>
    where TRequest : notnull {

    public async Task<Result<TResponse>> HandleAsync(
        RequestContext<TRequest> context,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken) { ... }
}

// v3
public class MyIntercept<TOperation, TResultValue> : IIntercept<TOperation, TResultValue>
    where TOperation : notnull {

    public async Task<Result<TResultValue>> HandleAsync(
        OperationContext<TOperation> context,
        OperationHandlerDelegate<TOperation, TResultValue> next,
        CancellationToken cancellationToken) { ... }
}
```

---

## New in v3

These are additive features — no migration required, but worth knowing about:

| Feature | Description |
|---------|-------------|
| **Operation Grants** | Owner-scoped grant-based access control with caching, invalidation, and warm-up |
| **Resource Access** | Object-level ACL evaluation via `IProtectedResource` and `IResourceAccessEvaluator` |
| **Permission Model** | First-class `Permission`, `PermissionSet`, and `[RequiresGrant]` attribute |
| **Authorization Constraints** | `IAuthorizationConstraint` for global cross-cutting authorization gates |
| **Authorization Telemetry** | Centralized OpenTelemetry instrumentation for the authorization pipeline |
| **Cache Consumer Tagging** | `cirreum.cache.consumer` metric tag distinguishes cache usage by subsystem |
| **Authentication Boundary** | `AuthenticationBoundary` enum tracks whether caller authenticated via Global or Tenant IdP |
| **`IAuthorizationContextAccessor`** | Access the current `AuthorizationContext` from anywhere in the pipeline |
| **`IOwnedApplicationUser`** | Tenant/owner identity sourced from the application user, not JWT claims |
| **Performance Optimizations** | Fast-path pipeline dispatch: 48ns/112B for void operations |

---

## Layer Migration Order

Cirreum.Core is the foundation of the platform stack. Migrate bottom-up:

```
1. Cirreum.Core              ← this release (v3)
2. Cirreum infrastructure    ← update NuGet ref, apply renames
3. Cirreum.Runtime.xxx       ← update NuGet ref, apply renames
4. Applications              ← update NuGet ref, apply renames + adopt new features
```

Each layer starts by applying the find-and-replace table from the [Quick Reference](#quick-reference--find-and-replace) section — those renames are fully mechanical.

At the **application layer**, you'll also want to evaluate the new authorization capabilities and migrate accordingly:

- **Operation Grants** — If your operations are owner-scoped, update your operation interfaces to implement `IOwnerMutateOperation`, `IOwnerLookupOperation`, `IOwnerSearchOperation`, or `ISelfOperation` as appropriate, and implement an `IOperationGrantProvider` to resolve grants for your domain.
- **Resource ACLs** — If your domain models need object-level access control, implement `IProtectedResource` on those models and provide an `IAccessEntryProvider` to supply access entries.
- **Authorization workarounds** — Any manual authorization logic that was bolted on to work around v2's limitations can likely be replaced by the built-in grant evaluation, permission model, or policy validator pipeline. Review and simplify.
