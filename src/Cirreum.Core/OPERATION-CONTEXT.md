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
              │ 1. Validate resource type                                            │
              │ 2. Get validators                                                    │
              │ 3. Check authentication                                              │
              │ 4. Get & resolve roles                                               │
              │ 5. Build AuthorizationContext (ONCE - canonical)                     │
              │ 6. Run validators                                                    │
              │ 7. Return Result                                                     │
              └──────────────────────────────────────────────────────────────────────┘
```

## Request Pipeline Flow

```text
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                                      Dispatcher                                             │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ 1. Get UserState from IUserStateAccessor                                                    │
│ 2. Capture StartTimestamp (high-precision timing)                                           │
│ 3. Create OperationContext                                                                  │
│    └─> Environment, RuntimeType, Timestamp, StartTimestamp,                                 │
│        UserState, OperationId, CorrelationId                                                │
│ 4. Create RequestContext<TRequest>                                                          │
│    └─> Composes OperationContext + Request + RequestType                                    │
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
              │                 (Created ONCE in Dispatcher)                       │
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
         │ • RequestId (→ OperationId)      │   │ • Profile, HasEnrichedProfile    │
         └──────────────────────────────────┘   └──────────────────────────────────┘
```

## Key Insights

### ✅ Single Creation

```text
OperationContext created ONCE in Dispatcher
    ├─> Composed into RequestContext
    └─> Composed into AuthorizationContext
```

### ✅ Zero Rebuilding

```text
1. Dispatcher creates OperationContext with StartTimestamp
2. RequestContext composes it
3. Authorization intercept extracts context.Operation
4. Evaluator reuses it (no GetUser() call!)
5. AuthorizationContext composes it
6. Validators use canonical context
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
- `IsAuthenticated` → `Operation.IsAuthenticated`
- `Profile` → `Operation.Profile`
- `HasEnrichedProfile` → `Operation.HasEnrichedProfile`
- `UserState` → `Operation.UserState`
- `RuntimeType` → `Operation.RuntimeType`
- `Timestamp` → `Operation.Timestamp`
- All helper methods delegate to Operation

## Design Principles

1. **Single Source of Truth**: OperationContext is created once and contains all canonical WHO/WHEN/WHERE/TIMING information
2. **Composition over Duplication**: RequestContext and AuthorizationContext compose OperationContext rather than duplicating its data
3. **Delegation for Convenience**: Higher-level contexts delegate to OperationContext for user properties and helpers
4. **Immutability**: All contexts are records, ensuring thread-safety and predictable behavior
5. **High-Precision Timing**: StartTimestamp enables accurate duration calculation at any point without mutable state
6. **Zero Allocation Timing**: Computed properties calculate elapsed time on demand without allocating stopwatch objects