# Context

## Context Composition Architecture

```text
┌───────────────────────────────────────────────────────────────────────────────────────────┐
│                                      OperationContext                                     │
│                       (Single Source of Truth for WHO / WHEN / WHERE)                     │
├───────────────────────────────────────────────────────────────────────────────────────────┤
│ • Environment                                                                             │
│ • Runtime (ApplicationRuntimeType)                                                        │
│ • Timestamp                                                                               │
│ • UserState (IUserState)                                                                  │
│ • OperationId                                                                             │
│ • CorrelationId                                                                           │
│                                                                                           │
│ Convenience Properties (defined ONCE):                                                    │
│ • UserId, UserName, TenantId, Provider, IsAuthenticated                                   │
│ • Profile, HasEnrichedProfile                                                             │
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
│ + Operation                                               │   │ + Operation                                               │
│ + Stopwatch                                               │   │ + EffectiveRoles                                          │
│ + Request                                                 │   │ + Resource                                                │
│ + RequestType                                             │   │                                                           │
│                                                           │   │ Delegates to Operation for user properties & helpers      │
│ Delegates to Operation for user properties & helpers      │   │                                                           │
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
│ 2. Create OperationContext                                                                  │
│    └─> Environment, Runtime, Timestamp, UserState, OperationId, CorrelationId               │
│ 3. Create RequestContext<TRequest>                                                          │
│    └─> Composes OperationContext + Stopwatch + Request                                      │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                             Authorization Intercept                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ Receives: RequestContext<TRequest>                                                          │
│ Extracts: context.Operation (OperationContext)                                              │
│ Calls: authorizor.Evaluate(context.Request, context.Operation)                              │
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
              │ • Runtime                                                          │
              │ • Timestamp                                                        │
              │ • UserState                                                        │
              │ • OperationId                                                      │
              │ • CorrelationId                                                    │
              └───────┬─────────────────────────────────────┬──────────────────────┘
                      │                                     │
                      ▼                                     ▼
         ┌──────────────────────────────────┐   ┌──────────────────────────────────┐
         │          RequestContext          │   │        AuthorizationContext      │
         │ + Stopwatch                      │   │ + EffectiveRoles                 │
         │ + Request                        │   │ + Resource                       │
         │ + RequestType                    │   │                                  │
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
1. Dispatcher creates OperationContext
2. RequestContext composes it
3. Authorization intercept extracts context.Operation
4. Evaluator reuses it (no GetUser() call!)
5. AuthorizationContext composes it
6. Validators use canonical context
```

### ✅ Clear Ownership

```text
OperationContext owns WHO/WHEN/WHERE
RequestContext owns pipeline tracking
AuthorizationContext owns authorization decisions
```
