# Request & Authorization Context

## Context Architecture

There is no shared intermediary object. Each context type owns its fields
directly and delegates only to two statics (`DomainContext` for environment /
runtime-type, `DomainFeatureResolver` for the namespace-derived domain).

`AuthorizationContext` is split into a **non-generic base** (resolved caller
identity) and a **generic derived** (adds the specific authorizable object).
The base is stored on a scoped `IAuthorizationContextAccessor` by the
authorization pipeline and reused by downstream consumers (e.g.,
`ResourceAccessEvaluator`) — eliminating redundant role resolution.

```text
                    [IUserStateAccessor]
                            │
                            ▼
                    ┌──────────────┐
                    │  IUserState  │
                    └──────┬───────┘
                           │
            ┌──────────────┴──────────────────────────────────────────────────┐
            │                                                                 │
            ▼                                                                 ▼
┌───────────────────────────────────────────────────┐   ┌───────────────────────────────────────────────────┐
│               RequestContext<TRequest>             │   │       AuthorizationContext (non-generic base)      │
│                                                   │   │                                                   │
│ Record parameters:                                │   │ Record parameters:                                │
│ • UserState (IUserState)                          │   │ • UserState (IUserState)                          │
│ • Request (TRequest)                              │   │ • EffectiveRoles (IImmutableSet<Role>)            │
│ • RequestType (string)                            │   │                                                   │
│ • RequestId (string — Activity.SpanId)            │   │ User convenience (delegate to UserState):         │
│ • CorrelationId (string — Activity.TraceId)       │   │ • UserId, UserName, TenantId, Provider            │
│ • StartTimestamp (long — high-precision)           │   │ • AccessScope, IsAuthenticated                    │
│                                                   │   │ • Profile, HasEnrichedProfile, ApplicationUser    │
│ Derived / captured:                               │   │                                                   │
│ • DomainFeature (DomainFeatureResolver)           │   │ Static / captured:                                │
│ • Environment (DomainContext.Environment)          │   │ • RuntimeType (DomainContext.RuntimeType)          │
│ • RuntimeType (DomainContext.RuntimeType)          │   │ • Timestamp (DateTimeOffset — captured at ctor)   │
│ • Timestamp (DateTimeOffset — captured at ctor)   │   │                                                   │
│                                                   │   │ Helper methods:                                   │
│ User convenience (delegate to UserState):         │   │ • HasActiveTenant(), IsFromProvider(),             │
│ • UserId, UserName, TenantId, Provider            │   │   IsInDepartment()                                │
│ • AccessScope, IsAuthenticated                    │   │                                                   │
│ • Profile, HasEnrichedProfile                     │   │         ▲ stored on IAuthorizationContextAccessor │
│                                                   │   │         │ (scoped, set by pipeline)               │
│ Timing:                                           │   ├─────────┴─────────────────────────────────────────┤
│ • ElapsedDuration (computed on demand)            │   │   AuthorizationContext<TAuthorizableObject>        │
│                                                   │   │   (sealed, extends base)                          │
│ Helper methods:                                   │   │                                                   │
│ • HasActiveTenant()                               │   │ Additional:                                       │
│ • IsFromProvider(provider)                        │   │ • AuthorizableObject (TAuthorizableObject)        │
│ • IsInDepartment(department)                      │   │ • Permissions (RequiredPermissionCache)            │
│                                                   │   │ • DomainFeature (DomainFeatureResolver)            │
└───────────────────────────────────────────────────┘   └───────────────────────────────────────────────────┘
```

## Authorization Flow (Two Entry Points Pattern)

```text
┌───────────────────────────────────────────────────────────────────────────────────────────┐
│                                 IAuthorizationEvaluator                                   │
└───────────────────────────────────────────────────────────────────────────────────────────┘
                                         │
                          ┌──────────────┴───────────────┐
                          │                              │
                          ▼                              ▼
            ┌─────────────────────────────┐   ┌─────────────────────────────────────┐
            │      Ad-hoc Entry Point     │   │     Context-Aware Entry Point       │
            │                             │   │            (Pipeline)               │
            │ Evaluate(authorizableObject)│   │                                     │
            │                             │   │ Evaluate(authorizableObject,        │
            │ 1. Get UserState from       │   │         userState)                  │
            │    IUserStateAccessor       │   │ Uses caller-provided IUserState     │
            └───────────────┬─────────────┘   └─────────────────┬───────────────────┘
                            │                                   │
                            ▼                                   ▼
              ┌──────────────────────────────────────────────────────────────────────┐
              │                       Shared Implementation                          │
              ├──────────────────────────────────────────────────────────────────────┤
              │ 1. Check authentication (→ Unauthenticated on fail)                  │
              │ 2. Assert runtime type == compile-time TAuthorizableObject           │
              │ 3. Resolve evaluators from DI (scope, authorizer, policy arrays)     │
              │ 4. Early-exit if all arrays empty and no owner-gate applies          │
              │ 5. Resolve roles → GetEffectiveRoles (inheritance expanded ONCE)     │
              │ 6. Build AuthorizationContext<T> (ONCE – canonical)                  │
              │                                                                      │
              │ 7. Stage 1 – Scope: first-failure short-circuit                      │
              │      Step 0: OperationGrantEvaluator (if applicable)                 │
              │      Step 1: IScopeEvaluator[] in registration order                 │
              │ 8. Stage 2 – Object Authorizers: one AuthorizerBase<T>               │
              │      FluentValidation rules aggregate within the authorizer;         │
              │      short-circuits to Stage 3 on any failure                        │
              │ 9. Stage 3 – Policy: IPolicyValidator[] filtered by                  │
              │      SupportedRuntimeTypes + AppliesTo, sorted by Order;             │
              │      aggregates within stage                                         │
              │                                                                      │
              │ 10. Return Result.Success / Result.Fail(Forbidden)                   │
              └──────────────────────────────────────────────────────────────────────┘
```

## Request Pipeline Flow

```text
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                              RequestHandlerWrapperImpl<T>                                   │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ 1. Start Activity (if telemetry listening) + capture StartTimestamp                         │
│ 2. Resolve handler + intercepts from DI                                                     │
│ 3. If no intercepts: invoke handler directly (BYPASS — rare)                                │
│ 4. Otherwise (TYPICAL):                                                                     │
│      a. GetUser() from IUserStateAccessor (cached)                                          │
│      b. RequestContext<T>.Create(UserState, Request, RequestType,                           │
│         RequestId, CorrelationId, StartTimestamp)                                            │
│      c. Walk pipeline via PipelineCursor (single delegate alloc)                            │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                             Authorization Intercept                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ Receives: RequestContext<TRequest>                                                          │
│ Calls: evaluator.Evaluate(context.Request, context.UserState)                               │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                         DefaultAuthorizationEvaluator                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Receives: AuthorizableObject + IUserState                                                 │
│ • Resolves roles from userState.Profile                                                     │
│ • Builds: AuthorizationContext<TAuthorizableObject>                                         │
│   └─> Composes UserState + EffectiveRoles + AuthorizableObject                              │
│ • Stamps base context on IAuthorizationContextAccessor (scoped)                             │
│ • Validators work with canonical AuthorizationContext                                       │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```text
                    [IUserStateAccessor]
                            │
                            ▼
                    ┌──────────────┐
                    │  IUserState  │
                    └──────┬───────┘
                           │
            ┌──────────────┴──────────────────────────────────────────────────┐
            │                                                                 │
            ▼                                                                 ▼
┌──────────────────────────────────┐   ┌──────────────────────────────────────────┐
│        RequestContext<T>         │   │     AuthorizationContext (base)            │
│                                  │   │     ── stored on accessor ──              │
│ Record params:                   │   │                                            │
│ • UserState                      │   │ Record params:                             │
│ • Request                        │   │ • UserState                                │
│ • RequestType                    │   │ • EffectiveRoles                           │
│ • RequestId                      │   │                                            │
│ • CorrelationId                  │   │ Delegates to UserState:                    │
│ • StartTimestamp                 │   │ • UserId, UserName, TenantId               │
│                                  │   │ • Provider, IsAuthenticated                │
│ Delegates to UserState:          │   │ • AccessScope, ApplicationUser             │
│ • UserId, UserName, TenantId    │   │ • Profile, HasEnrichedProfile              │
│ • Provider, IsAuthenticated     │   │                                            │
│ • AccessScope                    │   │ Static / captured:                         │
│ • Profile, HasEnrichedProfile   │   │ • RuntimeType, Timestamp                   │
│                                  │   │                                            │
│ Derived:                         │   │ Helpers: HasActiveTenant, IsFromProvider,  │
│ • Environment (DomainContext)    │   │   IsInDepartment                           │
│ • RuntimeType (DomainContext)    │   ├────────────────────────────────────────────┤
│ • DomainFeature (Resolver)       │   │  AuthorizationContext<TAuthorizableObject> │
│ • Timestamp (captured at ctor)   │   │  (sealed, extends base)                   │
│ • ElapsedDuration (computed)     │   │                                            │
│                                  │   │ • AuthorizableObject                       │
│                                  │   │ • Permissions (RequiredPermissionCache)     │
│                                  │   │ • DomainFeature (DomainFeatureResolver)     │
└──────────────────────────────────┘   └──────────────────────────────────────────┘
```

## Key Insights

### Single Creation

```text
RequestContext created ONCE via RequestContextFactory
    └─> Flows through pipeline to every intercept

AuthorizationContext created ONCE inside DefaultAuthorizationEvaluator
    ├─> Flows to every authorization stage (Scope → Authorizer → Policy)
    └─> Base (non-generic) stored on IAuthorizationContextAccessor
        └─> Reused by downstream consumers (e.g. ResourceAccessEvaluator)
```

### Zero Rebuilding

```text
1. RequestHandlerWrapperImpl<T> calls RequestContextFactory which
   calls GetUser() (sync fast-path when cached) and builds RequestContext
   with Activity-based IDs
2. Pipeline cursor hands RequestContext to each intercept
3. Authorization intercept passes context.Request + context.UserState
   to IAuthorizationEvaluator
4. Evaluator resolves effective roles + builds AuthorizationContext<T>
5. Evaluator stamps base context on IAuthorizationContextAccessor (scoped)
6. Every stage's validators see the same canonical context
7. No second GetUser() call — UserState is shared by reference
8. ResourceAccessEvaluator reads from accessor — zero re-resolution
```

### Clear Ownership

```text
RequestContext owns pipeline tracking:
    ├─> WHO: UserState (delegates to IUserState for identity/profile)
    ├─> WHAT: Request payload + RequestType
    ├─> WHEN: Timestamp (display) + StartTimestamp (precision)
    ├─> WHERE: Environment + RuntimeType (from DomainContext)
    └─> TRACING: RequestId + CorrelationId (from Activity)

AuthorizationContext owns authorization decisions:
    ├─> WHO: UserState (same IUserState, delegates for identity/profile)
    ├─> ROLES: EffectiveRoles (inheritance-expanded, computed once)
    ├─> TARGET: AuthorizableObject being evaluated
    ├─> PERMISSIONS: PermissionSet (from RequiredPermissionCache)
    └─> DOMAIN: DomainFeature (from DomainFeatureResolver)
```

### Timing Architecture

```text
StartTimestamp (long)
    ├─> Captured at request start (high-precision)
    ├─> Stored on RequestContext
    └─> Used to compute elapsed time on demand

ElapsedDuration (computed on demand)
    └─> RequestContext.ElapsedDuration (TimeSpan)

Benefits:
    • No stopwatch object needed
    • No state mutation
    • Accurate duration from any point in pipeline
    • Zero allocation for timing infrastructure
```

## Property Mapping Reference

### RequestContext Core Properties
- `UserState` (IUserState) — complete user identity and profile
- `Request` (TRequest) — the request payload
- `RequestType` (string) — type name for logging/diagnostics
- `RequestId` (string) — unique identifier (from Activity.SpanId)
- `CorrelationId` (string) — trace identifier (from Activity.TraceId)
- `StartTimestamp` (long) — high-precision timestamp for duration calculation

### RequestContext Derived Properties
- `Environment` → `DomainContext.Environment`
- `RuntimeType` → `DomainContext.RuntimeType`
- `DomainFeature` → `DomainFeatureResolver.Resolve<TRequest>()`
- `Timestamp` → `DateTimeOffset.UtcNow` (captured at construction)
- `ElapsedDuration` → `Timing.GetElapsedTime(StartTimestamp)`

### RequestContext User Convenience Properties
- `UserId` → `UserState.Id`
- `UserName` → `UserState.Name`
- `TenantId` → `UserState.Profile.Organization.OrganizationId`
- `Provider` → `UserState.Provider`
- `AccessScope` → `UserState.AccessScope`
- `IsAuthenticated` → `UserState.IsAuthenticated`
- `Profile` → `UserState.Profile`
- `HasEnrichedProfile` → `UserState.Profile.IsEnriched`

### AuthorizationContext (Non-Generic Base) Core Properties
- `UserState` (IUserState) — complete user identity and profile
- `EffectiveRoles` (IImmutableSet&lt;Role&gt;) — inheritance-expanded roles

### AuthorizationContext (Non-Generic Base) Derived Properties
- `RuntimeType` → `DomainContext.RuntimeType`
- `Timestamp` → `DateTimeOffset.UtcNow` (captured at construction)
- `ApplicationUser` → `UserState.ApplicationUser`

### AuthorizationContext (Non-Generic Base) User Convenience Properties
- `UserId` → `UserState.Id`
- `UserName` → `UserState.Name`
- `TenantId` → `UserState.Profile.Organization.OrganizationId`
- `Provider` → `UserState.Provider`
- `AccessScope` → `UserState.AccessScope`
- `IsAuthenticated` → `UserState.IsAuthenticated`
- `Profile` → `UserState.Profile`
- `HasEnrichedProfile` → `UserState.Profile.IsEnriched`

### AuthorizationContext&lt;TAuthorizableObject&gt; (Generic Derived) Additional Properties
- `AuthorizableObject` (TAuthorizableObject) — the object being evaluated
- `Permissions` → `RequiredPermissionCache.GetFor<TAuthorizableObject>()`
- `DomainFeature` → `DomainFeatureResolver.Resolve<TAuthorizableObject>()`

### IAuthorizationContextAccessor
- Scoped accessor holding the resolved `AuthorizationContext` (non-generic base)
- Set by `DefaultAuthorizationEvaluator` after role resolution
- Read by downstream consumers (e.g., `ResourceAccessEvaluator`) to avoid re-resolving roles
- Returns `null` before the authorization pipeline runs (e.g., background jobs)

## Access Scope

`AccessScope` is the coarse authorization dimension indicating *which IdP scheme*
authenticated the caller. It's stamped onto `IUserState` by
`IAccessScopeResolver` during user enrichment and surfaces on every context.

| Value | Meaning |
|---|---|
| `None` | Anonymous caller, or no `IAccessScopeResolver` is registered |
| `Global` | Authenticated via the configured `PrimaryScheme` — typically operator staff acting across tenants |
| `Tenant` | Authenticated via a customer/tenant scheme (Entra External ID, BYOID, per-customer OIDC, API keys, signed requests) |

### Where It's Used

- **Grant evaluation (Stage 1 Step 0a).** The `OperationGrantEvaluator` uses
  `AccessScope` to enforce CRL rules differently per scope. `Global`
  callers must supply an explicit `OwnerId` for writes (no auto-enrich),
  and must supply `OwnerId` for cacheable reads (no unbounded cache bucket).
  `Tenant` callers may auto-enrich from single-element reach. See the
  [Grants README](Authorization/Operations/Grants/README.md) for full CRL semantics.
- **Owner-scope gate (Stage 1 Step 0b).** The default `OwnerScopeEvaluator`
  uses `AccessScope` to decide whether the caller is *required* to match
  the resource's `OwnerId`, or is a cross-tenant operator who can bypass
  owner-match (e.g., `Global` callers performing admin-level operations).
- **Scope evaluators (Stage 1 Step 1).** Custom `IScopeEvaluator`
  implementations can short-circuit on `AccessScope` to enforce
  tenant-only or global-only routes.
- **Object authorizers (Stage 2).** A single
  `AuthorizerBase<T>` may branch on `context.AccessScope` to
  apply different rule sets per scope.
- **Policy validators (Stage 3).** Kill-switches and time-window policies
  often apply only to `Tenant` callers, bypassed for `Global`.

### Customization

Consumers replace the default resolver by registering their own
`IAccessScopeResolver` *before* `AddAuthorization` runs (TryAdd pattern).
See `IAccessScopeResolver` for an example implementation.

## Design Principles

1. **No intermediary objects**: `RequestContext` and `AuthorizationContext` each own their fields directly. `IUserState` is shared by reference — no copying, no vocabulary translation.
2. **Single Source of Truth for identity**: `IUserState` is the canonical identity carrier. Both contexts delegate user convenience properties to it.
3. **Static environment**: `Environment` and `RuntimeType` come from `DomainContext` — set once at startup, read from any context without passing.
4. **Immutability**: All contexts are records, ensuring thread-safety and predictable behavior.
5. **High-Precision Timing**: `StartTimestamp` on `RequestContext` enables accurate duration calculation at any point without mutable state.
6. **Zero Allocation Timing**: Computed `ElapsedDuration` calculates elapsed time on demand without allocating stopwatch objects.
