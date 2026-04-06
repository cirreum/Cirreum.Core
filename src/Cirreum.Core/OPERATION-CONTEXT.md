# Operational Context

## Context Composition Architecture

```text
┌───────────────────────────────────────────────────────────────────────────────────────────┐
│                                      OperationContext                                     │
│                (Single Source of Truth for WHO / WHEN / WHERE / TIMING)                   │
├───────────────────────────────────────────────────────────────────────────────────────────┤
│ • Environment                                                                             │
│ • RuntimeType (DomainRuntimeType)                                                         │
│ • Timestamp (DateTimeOffset - human-readable, for logging/display)                        │
│ • StartTimestamp (long - high-precision timestamp for duration calculation)               │
│ • UserState (IUserState)                                                                  │
│ • OperationId                                                                             │
│ • CorrelationId                                                                           │
│                                                                                           │
│ Convenience Properties (defined ONCE):                                                    │
│ • UserId, UserName, TenantId, Provider, IsAuthenticated                                   │
│ • AccessScope (None / Global / Tenant — resolved by IAccessScopeResolver)                 │
│ • Profile, HasEnrichedProfile                                                             │
│ • Elapsed (TimeSpan - computed on demand)                                                 │
│ • ElapsedMilliseconds (double - computed on demand)                                       │
│                                                                                           │
│ Helper Methods (defined ONCE):                                                            │
│ • HasActiveTenant()                                                                       │
│ • IsFromProvider(provider)                                                                │
│ • IsInDepartment(department)                                                              │
└───────────────────────────────────────────────────────────────────────────────────────────┘
                                          │
                                          │ Composed by
                                          │
                 ┌────────────────────────┼────────────────────────────────────────────────┐
                 │                                                                         │
                 ▼                                                                         ▼
┌───────────────────────────────────────────────────────────┐   ┌───────────────────────────────────────────────────────────┐
│                     RequestContext                        │   │                  AuthorizationContext                     │
│                                                           │   │                                                           │
│ + Operation (OperationContext)                            │   │ + Operation (OperationContext)                            │
│ + Request (TRequest)                                      │   │ + EffectiveRoles (IImmutableSet<Role>)                    │
│ + RequestType (string)                                    │   │ + Resource (TResource : IAuthorizableResource)            │
│                                                           │   │                                                           │
│ Delegates to Operation for:                               │   │ Delegates to Operation for:                               │
│ • All user properties & helpers                           │   │ • All user properties & helpers                           │
│ • Environment, RuntimeType                                │   │ • Environment, RuntimeType, Timestamp                     │
│ • Timing (StartTimestamp, ElapsedDuration)                │   │                                                           │
│ • RequestId (→ OperationId)                               │   │                                                           │
│ • CorrelationId                                           │   │                                                           │
└───────────────────────────────────────────────────────────┘   └───────────────────────────────────────────────────────────┘
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
            │ Evaluate(resource)          │   │                                     │
            │                             │   │ Evaluate(resource, operation)       │
            │ 1. Get UserState            │   │ Uses existing OperationContext      │
            │ 2. Build OperationContext   │   │                                     │
            └───────────────┬─────────────┘   └─────────────────┬───────────────────┘
                            │                                   │
                            ▼                                   ▼
              ┌──────────────────────────────────────────────────────────────────────┐
              │                       Shared Implementation                          │
              ├──────────────────────────────────────────────────────────────────────┤
              │ 1. Check authentication (→ Unauthenticated on fail)                  │
              │ 2. Assert runtime type == compile-time TResource                     │
              │ 3. Resolve evaluators from DI (scope, resource, policy arrays)       │
              │ 4. Early-exit if all arrays empty and no owner-gate applies          │
              │ 5. Resolve roles → GetEffectiveRoles (inheritance expanded ONCE)     │
              │ 6. Build AuthorizationContext<T> (ONCE – canonical)                  │
              │                                                                      │
              │ 7. Stage 1 – Scope: first-failure short-circuit                      │
              │      Step 0: OwnerScopeEvaluator (if applicable)                     │
              │      Step 1: IScopeEvaluator[] in registration order                 │
              │ 8. Stage 2 – Resource: one ResourceAuthorizerBase<T>                 │
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
│      b. RequestContext<T>.Create(...)                                                       │
│         └─> Internally constructs OperationContext                                          │
│             (Environment, RuntimeType, Timestamp, StartTimestamp,                           │
│              UserState, OperationId, CorrelationId)                                         │
│      c. Walk pipeline via PipelineCursor (single delegate alloc)                            │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                             Authorization Intercept                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ Receives: RequestContext<TRequest>                                                          │
│ Extracts: context.Operation (OperationContext)                                              │
│ Calls: evaluator.Evaluate(context.Request, context.Operation)                               │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                         DefaultAuthorizationEvaluator                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Receives: Resource + OperationContext                                                     │
│ • Resolves roles from OperationContext.UserState.Profile                                    │
│ • Builds: AuthorizationContext<TResource>                                                   │
│   └─> Composes OperationContext + EffectiveRoles + Resource                                 │
│ • Validators work with canonical AuthorizationContext                                       │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```text
                    [IUserStateAccessor]
                            │
                            ▼
                    ┌──────────────┐
                    │  UserState   │
                    └──────┬───────┘
                           │
                           ▼
              ┌────────────────────────────────────────────────────────────────────┐
              │                           OperationContext                         │
              │        (Created ONCE in RequestHandlerWrapperImpl<T>)              │
              │ • Environment                                                      │
              │ • RuntimeType                                                      │
              │ • Timestamp (DateTimeOffset)                                       │
              │ • StartTimestamp (long - for high-precision timing)                │
              │ • UserState                                                        │
              │ • OperationId                                                      │
              │ • CorrelationId                                                    │
              │ • Computed: Elapsed, ElapsedMilliseconds                           │
              └───────┬─────────────────────────────────────┬──────────────────────┘
                      │                                     │
                      ▼                                     ▼
         ┌──────────────────────────────────┐   ┌──────────────────────────────────┐
         │          RequestContext          │   │        AuthorizationContext      │
         │ + Operation                      │   │ + Operation                      │
         │ + Request                        │   │ + EffectiveRoles                 │
         │ + RequestType                    │   │ + Resource                       │
         │                                  │   │                                  │
         │ Delegates timing:                │   │ Delegates user context:          │
         │ • StartTimestamp                 │   │ • UserId, UserName, TenantId     │
         │ • ElapsedDuration (→ Elapsed)    │   │ • Provider, IsAuthenticated      │
         │ • RequestId (→ OperationId)      │   │ • AccessScope                    │
         │                                  │   │ • Profile, HasEnrichedProfile    │
         └──────────────────────────────────┘   └──────────────────────────────────┘
```

## Key Insights

### ✅ Single Creation

```text
OperationContext created ONCE by RequestContext.Create (in the wrapper)
    ├─> Composed into RequestContext
    └─> Composed into AuthorizationContext (later, inside the evaluator)
```

### ✅ Zero Rebuilding

```text
1. RequestHandlerWrapperImpl<T> calls GetUser() then RequestContext.Create,
   which internally builds the canonical OperationContext with StartTimestamp
2. RequestContext<T> composes it
3. Pipeline cursor hands RequestContext to each intercept
4. Authorization intercept extracts context.Operation
5. Evaluator reuses it — no second GetUser() call
6. AuthorizationContext<T> composes it (Operation + EffectiveRoles + Resource)
7. Every stage's validators see the same canonical context
```

### ✅ Clear Ownership

```text
OperationContext owns WHO/WHEN/WHERE/TIMING
    ├─> WHO: UserState with all identity & profile information
    ├─> WHEN: Timestamp (display) + StartTimestamp (precision)
    ├─> WHERE: Environment + RuntimeType
    └─> TIMING: Computed Elapsed & ElapsedMilliseconds properties

RequestContext owns pipeline tracking
    └─> Request-specific context + delegation to Operation

AuthorizationContext owns authorization decisions
    └─> EffectiveRoles + Resource + delegation to Operation
```

### ✅ Timing Architecture

```text
StartTimestamp (long)
    ├─> Captured at operation start (high-precision)
    ├─> Stored in OperationContext
    ├─> Available to all contexts via delegation
    └─> Used to compute elapsed time on demand

Elapsed Properties (computed on demand)
    ├─> OperationContext.Elapsed (TimeSpan)
    ├─> OperationContext.ElapsedMilliseconds (double)
    └─> RequestContext.ElapsedDuration (delegates to Elapsed)

Benefits:
    • No stopwatch object needed
    • No state mutation
    • Accurate duration from any point in pipeline
    • Zero allocation for timing infrastructure
```

## Property Mapping Reference

### OperationContext Core Properties
- `Environment` (string) - deployment environment
- `RuntimeType` (DomainRuntimeType) - execution context type
- `Timestamp` (DateTimeOffset) - human-readable operation start time
- `StartTimestamp` (long) - high-precision timestamp for duration calculation
- `UserState` (IUserState) - complete user identity and profile
- `OperationId` (string) - unique identifier for this operation
- `CorrelationId` (string) - identifier for correlating related operations

### Convenience Properties (Computed)
- `UserId` → `UserState.Id`
- `UserName` → `UserState.Name`
- `TenantId` → `UserState.Profile.Organization.OrganizationId`
- `Provider` → `UserState.Provider`
- `AccessScope` → `UserState.AccessScope` (see [Access Scope](#access-scope))
- `IsAuthenticated` → `UserState.IsAuthenticated`
- `Profile` → `UserState.Profile`
- `HasEnrichedProfile` → `UserState.Profile.IsEnriched`
- `Elapsed` → `Timing.GetElapsedTime(StartTimestamp)`
- `ElapsedMilliseconds` → `Timing.GetElapsedMilliseconds(StartTimestamp)`

### RequestContext Delegations
- `Environment` → `Operation.Environment`
- `RuntimeType` → `Operation.RuntimeType`
- `UserState` → `Operation.UserState`
- `Timestamp` → `Operation.Timestamp`
- `StartTimestamp` → `Operation.StartTimestamp`
- `RequestId` → `Operation.OperationId`
- `CorrelationId` → `Operation.CorrelationId`
- `ElapsedDuration` → `Operation.Elapsed`
- All user convenience properties delegate to Operation
- All helper methods delegate to Operation

### AuthorizationContext Delegations
- `UserId` → `Operation.UserId`
- `UserName` → `Operation.UserName`
- `TenantId` → `Operation.TenantId`
- `Provider` → `Operation.Provider`
- `AccessScope` → `Operation.AccessScope`
- `IsAuthenticated` → `Operation.IsAuthenticated`
- `Profile` → `Operation.Profile`
- `HasEnrichedProfile` → `Operation.HasEnrichedProfile`
- `UserState` → `Operation.UserState`
- `RuntimeType` → `Operation.RuntimeType`
- `Timestamp` → `Operation.Timestamp`
- All helper methods delegate to Operation

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

- **Grant evaluation (Stage 1 Step 0a).** The `GrantEvaluator` uses
  `AccessScope` to enforce CRL rules differently per scope. `Global`
  callers must supply an explicit `OwnerId` for writes (no auto-enrich),
  and must supply `OwnerId` for cacheable reads (no unbounded cache bucket).
  `Tenant` callers may auto-enrich from single-element reach. See the
  [Grants README](Authorization/Grants/README.md) for full CRL semantics.
- **Owner-scope gate (Stage 1 Step 0b).** The default `OwnerScopeEvaluator`
  uses `AccessScope` to decide whether the caller is *required* to match
  the resource's `OwnerId`, or is a cross-tenant operator who can bypass
  owner-match (e.g., `Global` callers performing admin-level operations).
- **Scope evaluators (Stage 1 Step 1).** Custom `IScopeEvaluator`
  implementations can short-circuit on `AccessScope` to enforce
  tenant-only or global-only routes.
- **Resource authorizers (Stage 2).** A single
  `ResourceAuthorizerBase<T>` may branch on `context.AccessScope` to
  apply different rule sets per scope.
- **Policy validators (Stage 3).** Kill-switches and time-window policies
  often apply only to `Tenant` callers, bypassed for `Global`.

### Customization

Consumers replace the default resolver by registering their own
`IAccessScopeResolver` *before* `AddAuthorization` runs (TryAdd pattern).
See `IAccessScopeResolver` for an example implementation.

## Design Principles

1. **Single Source of Truth**: OperationContext is created once and contains all canonical WHO/WHEN/WHERE/TIMING information
2. **Composition over Duplication**: RequestContext and AuthorizationContext compose OperationContext rather than duplicating its data
3. **Delegation for Convenience**: Higher-level contexts delegate to OperationContext for user properties and helpers
4. **Immutability**: All contexts are records, ensuring thread-safety and predictable behavior
5. **High-Precision Timing**: StartTimestamp enables accurate duration calculation at any point without mutable state
6. **Zero Allocation Timing**: Computed properties calculate elapsed time on demand without allocating stopwatch objects