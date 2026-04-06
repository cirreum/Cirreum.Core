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

A composable context system that provides single-source-of-truth operational context throughout your application:

- **OperationContext** - Canonical WHO/WHEN/WHERE/TIMING information
- **RequestContext** - Pipeline-aware request context with delegation
- **AuthorizationContext** - Authorization decisions with effective roles

```csharp
// Created once in RequestHandlerWrapperImpl<T> (typical path)
var operation = OperationContext.Create(
    userState, operationId, correlationId, startTimestamp);

// Composed into request context
var requestContext = new RequestContext<TRequest>(
    operation, request, requestType);

// Composed into authorization context (inside the evaluator)
var authContext = new AuthorizationContext<TResource>(
    operation, effectiveRoles, resource);
```

[See detailed context documentation](src/Cirreum.Core/OPERATION-CONTEXT.md)

### 🔐 Authorization Abstractions

Flexible, policy-based authorization with support for RBAC, ABAC, and ReBAC patterns:

- **RBAC** — roles with inheritance via `IAuthorizationRoleRegistry` and `EffectiveRoles`
- **ABAC** — attribute checks over user, resource, and ambient context inside `ResourceAuthorizerBase<T>` FluentValidation rules
- **ReBAC** — owner→resource and tenant→resource relationships enforced by `OwnerScopeEvaluatorBase` (Stage 1 Step 0) and custom `IScopeEvaluator` implementations (Stage 1 Step 1)
- **Grants (ReBAC)** — opt-in grant-based access control answering *"which owners can this caller reach?"* via `IGrantResolver<TDomain>`, with L1/L2 reach caching and CRL (Command/Read/List) enforcement semantics

- `IAuthorizationEvaluator` - Pluggable, three-stage authorization evaluator (Scope → Resource → Policy)
- `IAuthorizableResource` - Resources that can be authorized
- `ResourceAuthorizerBase<TResource>` - FluentValidation-backed resource authorizer (one per resource type)
- `IScopeEvaluator` / `OwnerScopeEvaluatorBase` - Stage 1 scope gates (tenant, owner, access-scope)
- `GrantEvaluator` / `IGrantResolver<TDomain>` - Stage 1 grant-aware scope gate with CRL enforcement
- `IPolicyValidator` - Stage 3 cross-cutting policy validators (hours, quotas, kill-switches)
- `AccessScope` (None / Global / Tenant) - Coarse authorization dimension resolved by `IAccessScopeResolver`
- `AccessReach` - Computed set of reachable owners per operation (Denied / Unrestricted / Bounded)
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

### 🚀 Messaging & CQRS (Cirreum Conductor)

A high-performance mediator implementation with comprehensive pipeline support for command/query separation:

**Core Features**
- Type-safe request/response contracts with `Result<T>` pattern
- Automatic handler discovery and registration
- Configurable intercept pipeline (validation, authorization, caching, telemetry)
- Pub/sub notification system with multiple publishing strategies
- OpenTelemetry integration with zero overhead when disabled

**Request Abstractions**
- `IRequest` / `IRequest<TResponse>` - Base request contracts
- `IAuthorizableCommand` / `IAuthorizableCommand<TResponse>` - Authorized write operations
- `IAuthorizableQuery<TResponse>` - Authorized read operations
- `IAuthorizableCacheableQuery<TResponse>` - Authorized, cacheable read operations
- `IAuthorizableOwnerScopedCommand` / `IAuthorizableOwnerScopedQuery<TResponse>` / `IAuthorizableOwnerScopedCacheableQuery<TResponse>` - Owner-scoped variants that opt into the Stage 1 owner gate
- `ICacheableQuery<TResponse>` - Query result caching with sliding/absolute expiration + cache tags

**Built-in Intercepts** (outermost → innermost, wrapping the handler)
- **Validation** - FluentValidation integration
- **Authorization** - Three-stage Scope / Resource / Policy pipeline
- **HandlerPerformance** - Request timing and metrics
- **QueryCaching** - Automatic result caching with configurable providers

**Discriminator**
- `IAuthorizableRequestBase` - Single pipeline discriminator for authorization; inherits `IAuthorizableResource` so every request is its own authorizable resource (no wrapping required)

### 🏗️ Primitives & Utilities

Battle-tested building blocks:

- High-precision timing with `StartTimestamp`
- Environment and runtime context abstractions
- Correlation and operation tracking

## Responsibilities

### 1. Cross-Framework Abstractions

Base interfaces and extensibility points for:

- Messaging and dispatch behaviors (implemented by **Cirreum.Conductor**)
- Authorization and evaluator pipelines
- Environment and identity access
- Plugin and integration boundaries

### 2. Core Primitives

Foundational building blocks including:

- Context structures (`OperationContext`, `RequestContext`, `AuthorizationContext`)
- Identifiers, markers, and metadata carriers
- Base implementations for validators, handlers, and authorizable resources
- High-precision timing infrastructure

### 3. Shared Patterns

Definitions that support Cirreum's architectural patterns:

- CQRS-style request and response contracts
- RBAC / ABAC / ReBAC authorization with role inheritance and owner/tenant relationships
- Interceptors, pipelines, and execution flows
- Metadata propagation and scoped request details

| Interface | Auth | Cache | Owner-Gated | Use Case |
|-----------|:----:|:-----:|:-----------:|----------|
| `IRequest` / `IRequest<T>` | ✗ | ✗ | ✗ | Internal / anonymous |
| `ICacheableQuery<T>` | ✗ | ✓ | ✗ | Public cached lookups |
| `IAuthorizableCommand` / `IAuthorizableCommand<T>` | ✓ | ✗ | ✗ | Protected state mutations |
| `IAuthorizableQuery<T>` | ✓ | ✗ | ✗ | Protected reads |
| `IAuthorizableCacheableQuery<T>` | ✓ | ✓ | ✗ | Protected high-freq reads |
| `IAuthorizableOwnerScopedCommand` / `<T>` | ✓ | ✗ | ✓ | Protected mutations of owned resources |
| `IAuthorizableOwnerScopedQuery<T>` | ✓ | ✗ | ✓ | Protected reads of owned resources |
| `IAuthorizableOwnerScopedCacheableQuery<T>` | ✓ | ✓ | ✓ | Protected, cached reads of owned resources |
| `IGrantedCommand<TDomain>` / `<TDomain, T>` | ✓ | ✗ | ✓ | Grant-aware writes (ReBAC) |
| `IGrantedRead<TDomain, T>` | ✓ | ✗ | ✓ | Grant-aware point-reads (ReBAC) |
| `IGrantedCacheableRead<TDomain, T>` | ✓ | ✓ | ✓ | Grant-aware cached reads (ReBAC) |
| `IGrantedList<TDomain, T>` | ✓ | ✗ | ✓ | Grant-aware cross-owner queries (ReBAC) |

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
Built-in support for OpenTelemetry, structured logging, and distributed tracing through context propagation.

## Architecture

### Context Composition

```text
OperationContext (Single Source of Truth)
    ├─> WHO: UserState, UserId, UserName, TenantId, Provider, AccessScope
    ├─> WHEN: Timestamp, StartTimestamp, Elapsed
    ├─> WHERE: Environment, RuntimeType
    └─> TIMING: High-precision duration calculation

RequestContext (Pipeline Context)
    ├─> Composes: OperationContext
    ├─> Adds: Request, RequestType
    └─> Delegates: All user/timing properties to Operation

AuthorizationContext (Authorization Decisions)
    ├─> Composes: OperationContext
    ├─> Adds: EffectiveRoles, Resource
    └─> Delegates: All user/scope/timing properties to Operation
```

### Zero-Allocation Timing

```csharp
// Captured once at operation start
long startTimestamp = Stopwatch.GetTimestamp();

// Computed on demand, zero allocation
TimeSpan elapsed = operation.Elapsed;
double milliseconds = operation.ElapsedMilliseconds;

// Available throughout pipeline via delegation
TimeSpan requestElapsed = requestContext.ElapsedDuration;
```

### Authorization Flow

```csharp
// Ad-hoc authorization (evaluator builds OperationContext)
var result = await evaluator.Evaluate(resource);

// Pipeline authorization (reuses OperationContext built by RequestHandlerWrapperImpl<T>)
var result = await evaluator.Evaluate(resource, operationContext);
```

Resource authorizers derive from `ResourceAuthorizerBase<TResource>`, which
is an `AbstractValidator<AuthorizationContext<TResource>>`. Each resource
type has **exactly one** registered authorizer (mirroring FluentValidation's
one-validator-per-type contract):

```csharp
public sealed class DocumentAuthorizer
    : ResourceAuthorizerBase<DocumentResource>
{
    public DocumentAuthorizer()
    {
        // Owner can always access; otherwise admin-role required
        this.RuleFor(context => context)
            .Must(context =>
                context.Resource.OwnerId == context.UserId
                || context.EffectiveRoles.Any(r => r.Name == "Admin"))
            .WithMessage("You don't have permission to access this document");
    }
}
```

Cross-cutting runtime rules (hours, quotas, kill-switches) live in
`IPolicyValidator` implementations evaluated in Stage 3. Owner/tenant and
access-scope gates live in `IScopeEvaluator` / `OwnerScopeEvaluatorBase`
implementations evaluated in Stage 1.

### Grants (ReBAC)

For multi-tenant applications requiring relationship-based access control,
Grants answers *"which owners can this caller reach?"* without the handler
knowing about grant tables:

```csharp
// 1. Declare a domain with a permission namespace
[GrantDomain("issues")]
public interface IIssueOperation;

// 2. Define grant-aware requests
[RequiresPermission("delete")]
public sealed record DeleteIssue(string Id) : IGrantedCommand<IIssueOperation> {
    public string? OwnerId { get; set; }
}

// 3. Implement a grant resolver (the only app code)
public class IssueGrantResolver : IGrantResolver<IIssueOperation> {
    public async ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
        AuthorizationContext<TResource> context, CancellationToken ct)
        where TResource : IAuthorizableResource {
        // Query your grants table
        var ownerIds = await db.GetGrantedOwners(context.UserId, context.RequiredPermissions);
        return new GrantedReach(ownerIds);
    }
}

// 4. Register
services.AddAccessGrants<IIssueOperation, IssueGrantResolver>();
```

See [Grants README](src/Cirreum.Core/Authorization/Grants/README.md) for the
full architecture, CRL enforcement rules, caching strategy, and configuration.

## Usage

### Installation

```bash
dotnet add package Cirreum.Core
```

### Basic Context Usage

```csharp
// 1. Create operation context (done once in RequestHandlerWrapperImpl<T>)
var operation = OperationContext.Create(
    userState: currentUserState,
    operationId: Guid.NewGuid().ToString(),
    correlationId: Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString(),
    startTimestamp: Stopwatch.GetTimestamp()
);

// 2. Use in request pipeline
var requestContext = RequestContext<MyRequest>.Create(
    userState: currentUserState,
    request: myRequest,
    requestType: nameof(MyRequest),
    requestId: operation.OperationId,
    correlationId: operation.CorrelationId,
    startTimestamp: operation.StartTimestamp
);

// 3. Access context throughout pipeline
logger.LogInformation(
    "Processing {RequestType} for user {UserId} - {Elapsed}ms",
    requestContext.RequestType,
    requestContext.UserId,
    requestContext.ElapsedDuration.TotalMilliseconds
);
```

### Authorization

```csharp
// Define authorizable resource (often the request itself via IAuthorizableRequestBase)
public sealed record GetDocumentQuery : IAuthorizableQuery<DocumentDto>
{
    public required string DocumentId { get; init; }
    public required string OwnerId { get; init; }
}

// Exactly one ResourceAuthorizerBase<T> per resource type (FluentValidation-style)
public sealed class GetDocumentQueryAuthorizer
    : ResourceAuthorizerBase<GetDocumentQuery>
{
    public GetDocumentQueryAuthorizer()
    {
        // Owner can always access; otherwise Admin role required
        this.RuleFor(context => context)
            .Must(context =>
                context.Resource.OwnerId == context.UserId
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

- **Cirreum.Conductor** - Implements messaging abstractions with CQRS patterns
- **Cirreum.Authorization** - Provides default authorization evaluators
- **Cirreum.Runtime** - Supplies runtime-specific implementations
- **Cirreum.Components** - Builds on primitives for UI components
- **Cirreum.Messaging** - Extends messaging contracts for events/notifications

This library:

- Defines the shared vocabulary of the framework
- Establishes conventions for request handling, authorization, and behaviors
- Acts as the foundational contract layer between domain-agnostic logic and implementation-specific libraries

## Documentation

- [Context Architecture](src/Cirreum.Core/OPERATION-CONTEXT.md) - Detailed context composition guide
- [Authorization Flow](src/Cirreum.Core/Authorization/FLOW.md) - High-level request → authorization flow
- [Authorization Sequence](src/Cirreum.Core/Authorization/SEQUENCE.md) - Detailed three-stage pipeline
- [Grants (ReBAC)](src/Cirreum.Core/Authorization/Grants/README.md) - Grant-based access control with CRL enforcement
- [Conductor](src/Cirreum.Core/Conductor/README.md) - In-process dispatcher + intercept pipeline

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