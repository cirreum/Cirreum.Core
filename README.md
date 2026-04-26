# Cirreum Core

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Core.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Core/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Core.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Core/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Core?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Core/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Core?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Core/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Foundational primitives and abstractions for the Cirreum Framework**

## Overview

**Cirreum.Core** is the foundational library of the Cirreum ecosystem. It provides the core abstractions, primitives, and shared patterns used across all Cirreum libraries and runtime components.

This is *not* the application's domain core. Instead, it acts as the **framework core**—the layer that defines the structural backbone of the entire stack.

All other Cirreum libraries (Conductor, Messaging, Authorization, Runtime, Components, and more) build directly on this project.

## Purpose

Cirreum.Core exists to deliver a stable, consistent, and expressive foundation that:

- Defines contracts and interfaces shared across the framework
- Supplies lightweight primitives for contexts, identifiers, and pipelines
- Hosts cross-cutting patterns such as CQRS contracts and authorization resources
- Provides utilities supporting consistent behavior across the ecosystem

Its mission is to centralize building blocks that must be **universally accessible and long-lived** across all Cirreum packages.

## Key Features

### 🎯 Context Architecture

A flat context system where each context owns its fields directly and delegates to `IUserState` for identity:

- **OperationContext&lt;T&gt;** - Pipeline operation envelope: `IUserState`, operation payload, timing, correlation IDs
- **AuthorizationContext&lt;T&gt;** - Authorization decisions: `IUserState`, effective roles, authorizable object, permissions

```csharp
// Created once in OperationHandlerWrapperImpl<T> (typical path)
var operationContext = OperationContext<TOperation>.Create(
    userState, operation, operationType, operationId, correlationId, startTimestamp);

// Built inside DefaultAuthorizationEvaluator
var authContext = new AuthorizationContext<TAuthorizableObject>(
    userState, effectiveRoles, authorizableObject);
```

[See detailed context documentation](src/Cirreum.Core/CONTEXT.md)

### 🔐 Authorization Abstractions

Flexible, policy-based authorization with support for RBAC, ABAC, and grant-based access control patterns:

- **RBAC** — roles with inheritance via `IAuthorizationRoleRegistry` and `EffectiveRoles`
- **ABAC** — attribute checks over user, resource, and ambient context inside `AuthorizerBase<T>` FluentValidation rules
- **Owner-scope** — owner→resource and tenant→resource relationships enforced by `OperationGrantEvaluator` (Stage 1 Step 0) and custom `IAuthorizationConstraint` implementations (Stage 1 Step 1)
- **Grants** — opt-in grant-based access control answering *"which owners can this caller access?"* via `IOperationGrantProvider`, with L1/L2 grant caching and Mutate/Lookup/Search/Self enforcement semantics

- `IAuthorizationEvaluator` - Pluggable, three-stage authorization evaluator (Grants + Constraints → Authorizers → Policy)
- `IAuthorizableObject` - Objects that can be authorized
- `AuthorizerBase<T>` - FluentValidation-backed object authorizer (one per authorizable object type)
- `IAuthorizationConstraint` - Stage 1 consumer-provided global authorization constraints
- `OperationGrantEvaluator` / `IOperationGrantProvider` - Stage 1 grant-aware scope gate with Mutate/Lookup/Search/Self enforcement
- `IPolicyValidator` - Stage 3 cross-cutting policy validators (hours, quotas, kill-switches)
- `AuthenticationBoundary` (None / Global / Tenant) - Authentication boundary resolved by `IAuthenticationBoundaryResolver`
- `OperationGrant` - Computed set of accessible owners per operation (Denied / Unrestricted / Bounded)
- Role resolution with inheritance support via `IAuthorizationRoleRegistry`
- Flexible AuthN/AuthZ with support for OIDC/MSAL/BYOID

### 📡 State Management

A synchronous state notification system designed for Blazor WASM's single-threaded runtime:

**Core Infrastructure**
- `IApplicationState` — marker interface for all state types
- `IStateManager` — central orchestrator for state retrieval, subscription, and notification
- `IScopedNotificationState` — batched notification with nested scope support
- `ScopedNotificationState` — base class with thread-safe scope counting via `Interlocked`

```csharp
// Subscribe and notify
stateManager.Subscribe<IThemeState>(state => jsModule.ApplyTheme(state.Current));
stateManager.NotifySubscribers<IThemeState>();

// Batch multiple mutations into a single notification
using var scope = state.CreateNotificationScope();
state.SetA(a);
state.SetB(b);
// single notification fires when scope disposes
```

**Built-in State Contracts**
- `IPageState` — page title composition (prefix, suffix, separator) and PWA detection
- `IThemeState` — theme mode (`light`/`dark`/`auto`), applied mode, and palette selection
- `INotificationState` — in-app notification management with read/dismiss semantics
- `IInitializationState` — startup progress tracking with task counting and error collection

**State Containers**
- `IStateContainer` → `IPersistableStateContainer` — key-value storage with typed handles and serialization
- `ISessionState` / `ILocalState` / `IMemoryState` — browser session, local, and in-memory storage markers
- `IStateValueHandle<T>` — typed handle for individual state values with change notification
- `IStateContainerEncryption` — pluggable encryption/obfuscation for persisted values

**Remote State**
- `IRemoteState` — domain data fetched from APIs and cached in memory with `IsLoaded`/`IsLoading`/`IsRefreshing` lifecycle
- `IInitializableRemoteState` — remote state that participates in startup initialization

**Initialization Pipeline**
- `IInitializationOrchestrator` — coordinates two-phase startup (auth → app services)
- `IInitializable` — contract for services that participate in ordered, conditional startup initialization

**Identity & Session**
- `IUserState` — current user context (auth status, claims, profile, application user binding)
- `IUserSession` — session tracking with activity timestamps and expiration
- `IApplicationUser` — application-layer user entity independent of identity provider

**Registration**
- `IStateBuilder` — fluent builder for DI registration of state types, remote state, and encryption

### 🚀 Operation Pipeline & CQRS (Cirreum Conductor)

A high-performance operation pipeline engine with comprehensive support for command/query separation:

**Core Features**
- Type-safe operation/result contracts with `Result<TResultValue>` pattern
- Automatic handler discovery and registration
- Configurable intercept pipeline (validation, authorization, caching, telemetry)
- Pub/sub notification system with multiple publishing strategies
- OpenTelemetry integration with zero overhead when disabled

**Operation Abstractions**
- `IOperation` / `IOperation<TResultValue>` - Base operation contracts
- `IAuthorizableOperation` / `IAuthorizableOperation<TResultValue>` - Authorized operations
- `IOwnerMutateOperation` / `IOwnerLookupOperation<TResultValue>` / `IOwnerSearchOperation<TResultValue>` - Owner-scoped grant-aware operations
- `IOwnerCacheableLookupOperation<TResultValue>` - Owner-scoped cacheable lookups
- `ISelfMutateOperation` / `ISelfLookupOperation<TResultValue>` - Self-scoped identity operations
- `ICacheableOperation<TResultValue>` - Query result caching with sliding/absolute expiration + cache tags

**Built-in Intercepts** (outermost → innermost, wrapping the handler)
- **Validation** - FluentValidation integration
- **Authorization** - Three-stage Scope / Resource / Policy pipeline
- **GrantedLookupAudit** - Pattern C runtime audit; emits a warning log + OTel tag when an `IGrantableLookupBase` operation completes without the handler reading the grant accessor (handler-deferred ownership check skipped)
- **HandlerPerformance** - Operation timing and metrics
- **QueryCaching** - Automatic result caching with configurable providers

**Discriminator**
- `IAuthorizableOperationBase` - Single pipeline discriminator for authorization; inherits `IAuthorizableObject` so every operation is its own authorizable resource (no wrapping required)

### 📊 Observability

Centralized telemetry with zero overhead when OpenTelemetry is not configured:

**Telemetry Classes** (each owns an `ActivitySource` and/or `Meter`):
- `OperationTelemetry` — Conductor dispatcher metrics (operation counts, durations, failures/cancellations)
- `AuthorizationTelemetry` — three-stage pipeline metrics (per-stage decision counters, pipeline duration histogram, grant resolution cache tracking)
- `CacheTelemetry` — cache hit/miss counters and operation duration histograms

**Cache Instrumentation** — `InstrumentedCacheService` decorator wraps any `ICacheService` implementation transparently via DI. All consumers (`QueryCaching`, `OperationGrantFactory`, future consumers) get hit/miss tracking and duration metrics for free. The decorator detects hits vs misses via a `factoryExecuted` flag — no `ICacheService` API changes needed. `NoCacheService` is never wrapped (zero overhead when caching is disabled).

**OTel Integration**:
```csharp
builder.Services.AddOpenTelemetry()
    .AddCirreum()       // wires all Cirreum activity sources + meters
    .UseOtlpExporter();
```

**Metrics Published**:

| Metric | Type | Tags |
|--------|------|------|
| `conductor.operations.total` | Counter | operation.type, operation.status |
| `conductor.operations.duration` | Histogram (ms) | operation.type, operation.status |
| `cirreum.authz.decisions` | Counter | stage, step, decision, reason |
| `cirreum.authz.duration` | Histogram (ms) | resource_type, decision |
| `cirreum.authz.grant.cache` | Counter | cache_level (bypass/l1-hit/l2/denied-early) |
| `cirreum.authz.grant.duration` | Histogram (ms) | domain, resource_type |
| `cirreum.cache.operations` | Counter | status, caller, consumer |
| `cirreum.cache.duration` | Histogram (ms) | status, caller, consumer |

### 🏗️ Primitives & Utilities

Battle-tested building blocks:

- High-precision timing with `StartTimestamp`
- Environment and runtime context abstractions
- Correlation and operation tracking

## Responsibilities

### 1. Cross-Framework Abstractions

Base interfaces and extensibility points for:

- Operation dispatch and pipeline behaviors (implemented by **Cirreum.Conductor**)
- Authorization and evaluator pipelines
- Environment and identity access
- Plugin and integration boundaries

### 2. Core Primitives

Foundational building blocks including:

- Context structures (`OperationContext<T>`, `AuthorizationContext<T>`)
- Identifiers, markers, and metadata carriers
- Base implementations for validators, handlers, and authorizable objects
- High-precision timing infrastructure

### 3. Shared Patterns

Definitions that support Cirreum's architectural patterns:

- CQRS-style operation and result contracts
- RBAC / ABAC / grant-based authorization with role inheritance and owner/tenant relationships
- Interceptors, pipelines, and execution flows
- Metadata propagation and scoped operation details

| Interface | Auth | Cache | Owner-Gated | Use Case |
|-----------|:----:|:-----:|:-----------:|----------|
| `IOperation` / `IOperation<T>` | ✗ | ✗ | ✗ | Internal / anonymous |
| `ICacheableOperation<T>` | ✗ | ✓ | ✗ | Public cached lookups |
| `IAuthorizableOperation` / `IAuthorizableOperation<T>` | ✓ | ✗ | ✗ | Protected operations (reads and writes) |
| `IOwnerMutateOperation` / `<TResultValue>` | ✓ | ✗ | ✓ | Grant-aware writes |
| `IOwnerLookupOperation<TResultValue>` | ✓ | ✗ | ✓ | Grant-aware point-reads |
| `IOwnerCacheableLookupOperation<TResultValue>` | ✓ | ✓ | ✓ | Grant-aware cached reads |
| `IOwnerSearchOperation<TResultValue>` | ✓ | ✗ | ✓ | Grant-aware cross-owner queries |
| `ISelfMutateOperation` / `<TResultValue>` | ✓ | ✗ | ✓ | Self-scoped identity writes |
| `ISelfLookupOperation<TResultValue>` | ✓ | ✗ | ✓ | Self-scoped identity reads |

### 4. Utilities & Helpers

Common functionality implemented using a curated set of stable dependencies:

- **SmartFormat** for formatting and templating
- **FluentValidation** for rule-based validation
- **CsvHelper** for import/export workflows

## Dependencies

Cirreum.Core is intentionally lightweight, but not dependency-free. It includes a **small, stable set of critical foundational libraries**:

- `Cirreum.Result` - Railway-oriented programming with Result<T>
- 'OpenTelemetry.Api' - OpenTelemetry support
- 'OpenTelemetry.Extensions.Hosting' - OpenTelemetry support
- `Microsoft.Extensions.Telemetry.Abstractions` - OpenTelemetry support
- `Microsoft.Extensions.Configuration.Json` - Configuration management
- `Microsoft.Extensions.Configuration.Binder` - Configuration binding
- `FluentValidation` - Validation and Authorization rules
- `SmartFormat` - String formatting
- `CsvHelper` - CSV processing

These are *framework-level* dependencies chosen for stability, longevity, and ecosystem alignment.

## Design Principles

### 🎨 Lightweight, Not Minimalist
Dependencies are curated—not avoided for their own sake. Each dependency provides significant value and is widely adopted in the .NET ecosystem.

### 🔒 Stable and Forward-Compatible
The API surface here is foundational and should evolve slowly. Breaking changes are avoided whenever possible.

### 🔌 Extensible by Design
Every contract is intended to be implemented differently across runtimes or applications. Extensibility is a first-class concern.

### ✅ Testability First
All primitives and abstractions are unit-test-friendly with minimal dependencies and clear interfaces.

### ⚡ Performance Conscious
Zero-allocation patterns where possible, computed properties over mutable state, and careful use of immutable records.

### 📊 Observable by Default
Built-in support for OpenTelemetry with centralized telemetry classes, zero-cost instrumentation when OTel is not configured, and decorator-based cache telemetry that instruments all `ICacheService` consumers automatically.

## Architecture

### Context Architecture

```text
OperationContext<T> (Pipeline Context)
    ├─> WHO: UserState (IUserState) — delegates UserId, UserName, etc.
    ├─> WHAT: Operation, OperationType
    ├─> WHEN: Timestamp, StartTimestamp, ElapsedDuration
    ├─> WHERE: Environment, RuntimeType (from DomainContext)
    └─> TRACING: OperationId, CorrelationId (from Activity)

AuthorizationContext<T> (Authorization Decisions)
    ├─> WHO: UserState (IUserState) — delegates UserId, UserName, etc.
    ├─> ROLES: EffectiveRoles (inheritance-expanded)
    ├─> TARGET: AuthorizableObject
    ├─> PERMISSIONS: PermissionSet (from RequiredGrantCache)
    └─> DOMAIN: DomainFeature (from DomainFeatureResolver)
```

### Zero-Allocation Timing

```csharp
// Captured once at operation start
long startTimestamp = Stopwatch.GetTimestamp();

// Computed on demand, zero allocation
TimeSpan elapsed = operationContext.ElapsedDuration;
```

### Authorization Flow

```csharp
// Ad-hoc authorization (evaluator gets UserState from IUserStateAccessor)
var result = await evaluator.Evaluate(authorizableObject);

// Pipeline authorization (passes UserState from OperationContext)
var result = await evaluator.Evaluate(authorizableObject, userState);
```

Object authorizers derive from `AuthorizerBase<TAuthorizableObject>`, which
is an `AbstractValidator<AuthorizationContext<TAuthorizableObject>>`. Each authorizable object
type has **exactly one** registered authorizer (mirroring FluentValidation's
one-validator-per-type contract):

```csharp
public sealed class DocumentAuthorizer
    : AuthorizerBase<DocumentResource>
{
    public DocumentAuthorizer()
    {
        // Owner can always access; otherwise admin-role required
        this.RuleFor(context => context)
            .Must(context =>
                context.AuthorizableObject.OwnerId == context.UserId
                || context.EffectiveRoles.Any(r => r.Name == "Admin"))
            .WithMessage("You don't have permission to access this document");
    }
}
```

Cross-cutting runtime rules (hours, quotas, kill-switches) live in
`IPolicyValidator` implementations evaluated in Stage 3. Global
authorization constraints live in `IAuthorizationConstraint`
implementations evaluated in Stage 1.

### Grants

For multi-tenant applications requiring grant-based access control,
Grants answers *"which owners can this caller access?"* without the handler
knowing about grant tables:

```csharp
// 1. Define grant-aware operations (domain derived from namespace convention)
[RequiresGrant("delete")]
public sealed record DeleteIssue(string Id) : IOwnerMutateOperation {
    public string? OwnerId { get; set; }
}

// 2. Implement a grant resolver (the only app code)
public class AppOperationGrantProvider : IOperationGrantProvider {
    public async ValueTask<OperationGrantResult> ResolveGrantsAsync<TAuthorizableObject>(
        AuthorizationContext<TAuthorizableObject> context, CancellationToken ct)
        where TAuthorizableObject : IAuthorizableObject {
        // Query your grants table
        var ownerIds = await db.GetGrantedOwners(context.UserId, context.RequiredGrants);
        return new OperationGrantResult(ownerIds);
    }
}

// 3. Register
services.AddOperationGrants<AppOperationGrantProvider>();
```

See [Grants README](src/Cirreum.Core/Authorization/Operations/Grants/README.md) for the
full architecture, Mutate/Lookup/Search/Self enforcement rules, caching strategy, and configuration.

## Usage

### Installation

```bash
dotnet add package Cirreum.Core
```

### Basic Context Usage

```csharp
// 1. Create operation context (done once via OperationContextFactory)
var operationContext = OperationContext<MyOperation>.Create(
    userState: currentUserState,
    operation: myOperation,
    operationType: nameof(MyOperation),
    operationId: activity?.SpanId.ToString()
        ?? ActivitySpanId.CreateRandom().ToHexString(),
    correlationId: activity?.TraceId.ToString()
        ?? ActivityTraceId.CreateRandom().ToHexString(),
    startTimestamp: Stopwatch.GetTimestamp());

// 2. Access context throughout pipeline
logger.LogInformation(
    "Processing {OperationType} for user {UserId} - {Elapsed}ms",
    operationContext.OperationType,
    operationContext.UserId,
    operationContext.ElapsedDuration.TotalMilliseconds);
```

### Authorization

```csharp
// Define authorizable object (operations are their own authorizable object via IAuthorizableOperationBase)
public sealed record GetDocumentQuery : IAuthorizableOperation<DocumentDto>
{
    public required string DocumentId { get; init; }
    public required string OwnerId { get; init; }
}

// Exactly one AuthorizerBase<T> per authorizable object type (FluentValidation-style)
public sealed class GetDocumentQueryAuthorizer
    : AuthorizerBase<GetDocumentQuery>
{
    public GetDocumentQueryAuthorizer()
    {
        // Owner can always access; otherwise Admin role required
        this.RuleFor(context => context)
            .Must(context =>
                context.AuthorizableObject.OwnerId == context.UserId
                || context.EffectiveRoles.Any(r => r.Name == "Admin"))
            .WithMessage("You don't have permission to access this document");
    }
}

// Evaluate authorization (normally runs inside the Authorization intercept)
var result = await authEvaluator.Evaluate(query, operationContext);
if (result.IsFailed)
{
    return Forbid(result.Errors);
}
```

## Integration with Cirreum Ecosystem

Every other Cirreum library depends on **Cirreum.Core**:

- **Cirreum.Conductor** - Implements the operation pipeline with CQRS patterns
- **Cirreum.Authorization** - Provides default authorization evaluators
- **Cirreum.Runtime** - Supplies runtime-specific implementations
- **Cirreum.Components** - Builds on primitives for UI components
- **Cirreum.Messaging** - Extends messaging contracts for events/notifications

This library:

- Defines the shared vocabulary of the framework
- Establishes conventions for operation handling, authorization, and behaviors
- Acts as the foundational contract layer between domain-agnostic logic and implementation-specific libraries

## Documentation

- [Authorization (user guide)](src/Cirreum.Core/Authorization/README.md) — start here for anything auth-related: pipeline, permissions, roles, the decision table for picking the right tool
  - [Authorization Flow](src/Cirreum.Core/Authorization/FLOW.md) — full request flow from HTTP entry through pipeline exit
  - [Pipeline Sequence](src/Cirreum.Core/Authorization/SEQUENCE.md) — three-stage evaluator internals, telemetry, allocation notes
  - [Grants](src/Cirreum.Core/Authorization/Operations/Grants/README.md) — grant-based access control (Stage 1 Step 0)
  - [Resources](src/Cirreum.Core/Authorization/Resources/README.md) — object-level ACLs evaluated in-handler
- [Context Architecture](src/Cirreum.Core/CONTEXT.md) — operation & authorization context architecture
- [Conductor](src/Cirreum.Core/Conductor/README.md) — in-process operation pipeline + dispatcher

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Core follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [Documentation](https://github.com/cirreum/Cirreum.Core/wiki)
- 🐛 [Issue Tracker](https://github.com/cirreum/Cirreum.Core/issues)
- 💬 [Discussions](https://github.com/cirreum/Cirreum.Core/discussions)

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*