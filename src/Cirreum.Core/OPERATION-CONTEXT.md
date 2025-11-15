# Architecture Diagrams

## Context Composition Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        OperationContext                         │
│  (Single Source of Truth for WHO/WHEN/WHERE)                    │
├─────────────────────────────────────────────────────────────────┤
│ • Environment                                                   │
│ • Runtime (ApplicationRuntimeType)                              │
│ • Timestamp                                                     │
│ • UserState (IUserState)                                        │
│ • OperationId                                                   │
│ • CorrelationId                                                 │
│                                                                 │
│ Convenience Properties (defined ONCE):                          │
│ • UserId, UserName, TenantId, Provider, IsAuthenticated         │
│ • Profile, HasEnrichedProfile                                   │
│                                                                 │
│ Helper Methods (defined ONCE):                                  │
│ • HasActiveTenant()                                             │
│ • IsFromProvider(provider)                                      │
│ • IsInDepartment(department)                                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Composed by
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌──────────────┐    ┌──────────────────┐    ┌──────────────┐
│ RequestContext│    │AuthorizationContext│    │AuditLogEntry │
│              │    │                  │    │              │
│ + Operation  │    │ + Operation      │    │ Built FROM   │
│ + Stopwatch  │    │ + EffectiveRoles │    │ Operation    │
│ + Request    │    │ + Resource       │    │              │
│ + RequestType│    │                  │    │ + DurationMs │
│              │    │ Delegates to     │    │ + Result     │
│ Delegates to │    │ Operation for    │    │ + FailureReason│
│ Operation for│    │ user properties  │    │ + ErrorType  │
│ all user     │    │ and helpers      │    │              │
│ properties   │    │                  │    │              │
│ and helpers  │    │                  │    │              │
└──────────────┘    └──────────────────┘    └──────────────┘
```

## Authorization Flow (Two Entry Points Pattern)

```
┌─────────────────────────────────────────────────────────────────┐
│                   IAuthorizationEvaluator                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┴──────────────┐
                │                            │
                ▼                            ▼
    ┌───────────────────────┐    ┌────────────────────────┐
    │   Ad-hoc Entry Point  │    │ Context-Aware Entry    │
    │                       │    │ Point (Pipeline)       │
    │ Evaluate(resource)    │    │                        │
    │                       │    │ Evaluate(resource,     │
    │ 1. Get UserState      │    │          operation)    │
    │    from accessor      │    │                        │
    │ 2. Build Operation    │────┼───►│                   │
    │    Context            │    │    │ Uses existing     │
    │ 3. Delegate ──────────┘    │    │ OperationContext  │
    │                            │    │                   │
    └────────────────────────────┴────┴───────────────────┘
                                       │
                                       ▼
                        ┌──────────────────────────────┐
                        │ Shared Implementation        │
                        ├──────────────────────────────┤
                        │ 1. Validate resource type    │
                        │ 2. Get validators            │
                        │ 3. Check authentication      │
                        │ 4. Get & resolve roles       │
                        │ 5. Build AuthorizationContext│
                        │    (ONCE - canonical)        │
                        │ 6. Run validators            │
                        │ 7. Return Result             │
                        └──────────────────────────────┘
```

## Request Pipeline Flow

```
┌──────────────────────────────────────────────────────────────┐
│                         Dispatcher                           │
├──────────────────────────────────────────────────────────────┤
│ 1. Get UserState from IUserStateAccessor                     │
│ 2. Create OperationContext                                   │
│    └─> Environment, Runtime, Timestamp, UserState,           │
│        OperationId, CorrelationId                            │
│ 3. Create RequestContext<TRequest>                           │
│    └─> Composes OperationContext + Stopwatch + Request       │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ Pass RequestContext
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                    Authorization Intercept                   │
├──────────────────────────────────────────────────────────────┤
│ • Receives: RequestContext<TRequest>                         │
│ • Extracts: context.Operation (OperationContext)             │
│ • Calls: authorizor.Evaluate(                                │
│            context.Request,                                  │
│            context.Operation)  ← Reuses! No rebuilding!      │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ Pass to evaluator
                              ▼
┌──────────────────────────────────────────────────────────────┐
│              DefaultAuthorizationEvaluator                   │
├──────────────────────────────────────────────────────────────┤
│ • Receives: Resource + OperationContext                      │
│ • Resolves roles from OperationContext.UserState.Profile     │
│ • Builds: AuthorizationContext<TResource>                    │
│   └─> Composes OperationContext + EffectiveRoles + Resource  │
│ • Validators work with canonical AuthorizationContext        │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ After request completion
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                       Audit Logging                          │
├──────────────────────────────────────────────────────────────┤
│ • Receives: RequestContext<TRequest>                         │
│ • Extracts: context.Operation properties                     │
│ • Creates: AuditLogEntry                                     │
│   └─> Built FROM OperationContext + outcome data             │
│ • Publishes: AuditEventNotification                          │
└──────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```
                    [IUserStateAccessor]
                            │
                            │ GetUser()
                            ▼
                    ┌──────────────┐
                    │  UserState   │
                    └──────┬───────┘
                           │
                           │ Used to build
                           ▼
              ┌─────────────────────────┐
              │   OperationContext      │◄─── Created ONCE
              │                         │     in Dispatcher
              │ • Environment           │
              │ • Runtime               │
              │ • Timestamp             │
              │ • UserState             │
              │ • OperationId           │
              │ • CorrelationId         │
              └────┬─────────────┬──────┘
                   │             │
         Composed  │             │  Composed
         by        │             │  by
                   │             │
                   ▼             ▼
      ┌─────────────────┐   ┌──────────────────┐
      │ RequestContext  │   │AuthorizationContext│
      │                 │   │                  │
      │ + Stopwatch     │   │ + EffectiveRoles │
      │ + Request       │   │ + Resource       │
      │ + RequestType   │   │                  │
      └────┬────────────┘   └────┬─────────────┘
           │                     │
           │ Used for            │ Used for
           │ Audit               │ Validation
           ▼                     ▼
    ┌──────────────┐    ┌────────────────────┐
    │AuditLogEntry │    │  FluentValidation  │
    │              │    │  Validators        │
    │ Snapshot of  │    │                    │
    │ Operation +  │    │ Work with          │
    │ Outcome      │    │ AuthContext        │
    └──────────────┘    └────────────────────┘
```

## Key Insights

### ✅ Single Creation
```
OperationContext created ONCE in Dispatcher
    │
    ├─> Composed into RequestContext (pipeline tracking)
    ├─> Composed into AuthorizationContext (validation)
    └─> Extracted into AuditLogEntry (persistence)
```

### ✅ Zero Rebuilding
```
Pipeline Flow:
1. Dispatcher creates OperationContext
2. RequestContext composes it
3. Authorization intercept extracts context.Operation
4. Evaluator reuses it (no GetUser() call!)
5. AuthorizationContext composes it
6. Validators use canonical context
7. Audit factory extracts from it
```

### ✅ Clear Ownership
```
OperationContext owns:
    └─> WHO/WHEN/WHERE information

RequestContext owns:
    └─> Pipeline tracking (timing, correlation)

AuthorizationContext owns:
    └─> Authorization decisions (roles, resource)

AuditLogEntry owns:
    └─> Outcome information (result, duration)
```