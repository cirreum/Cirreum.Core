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
// Created once in the dispatcher
var operation = OperationContext.Create(
    userState, operationId, correlationId, startTimestamp);

// Composed into request context
var requestContext = new RequestContext<TRequest>(
    operation, request, requestType);

// Composed into authorization context
var authContext = new AuthorizationContext<TResource>(
    operation, effectiveRoles, resource);
```

[See detailed context documentation](docs/OPERATION-CONTEXT.md)

### 🔐 Authorization Abstractions

Flexible, policy-based authorization with support for both RBAC and ABAC patterns:

- `IAuthorizationEvaluator` - Pluggable authorization evaluation
- `IAuthorizableResource` - Resources that can be authorized
- `IAuthorizationValidator` - Custom authorization logic
- Role resolution with inheritance support
- Built-in Authorization visualizers and documenters
- Flexible AuthN/AuthZ with support for OIDC/MSAL

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
- `IDomainCommand` / `IDomainCommand<TResponse>` - Commands with auditing and authorization
- `IDomainQuery<TResponse>` - Queries with auditing and authorization
- `IDomainCacheableQuery<TResponse>` - Cacheable queries with automatic cache management

**Built-in Intercepts**
- **Validation** - FluentValidation integration
- **Authorization** - Resource and policy-based authorization
- **Performance Monitoring** - Request timing and metrics
- **Query Caching** - Automatic result caching with configurable providers

**Extension Interfaces**
- `IAuditableRequest` / `IAuditableRequest<T>` - Automatic audit trail generation
- `IAuthorizableRequest` / `IAuthorizableRequest<T>` - Authorization evaluation
- `ICacheableQuery<T>` - Query result caching with sliding/absolute expiration

### 🏗️ Primitives & Utilities

Battle-tested building blocks:

- High-precision timing with `StartTimestamp`
- Environment and runtime context
- Identity and user state abstractions
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
- ABAC/RBAC authorization with role inheritance
- Interceptors, pipelines, and execution flows
- Metadata propagation and scoped request details

| Interface | Auth | Audit | Cache | Use Case |
|-----------|:----:|:-----:|:-----:|----------|
| `IRequest<T>` | ✗ | ✗ | ✗ | Internal/anonymous |
| `IAuthorizableRequest<T>` | ✓ | ✗ | ✗ | Protected, no logging |
| `IAuditableRequest<T>` | ✗ | ✓ | ✗ | Logged, open access |
| `ICacheableQuery<T>` | ✗ | ✗ | ✓ | Public cached lookups |
| `IAuthorizableCacheableQuery<T>` | ✓ | ✗ | ✓ | Protected high-freq reads |
| `IDomainQuery<T>` | ✓ | ✓ | ✗ | Sensitive reads |
| `IDomainCommand<T>` | ✓ | ✓ | ✗ | State mutations |
| `IDomainCacheableQuery<T>` | ✓ | ✓ | ✓ | Full coverage + cache |

### 4. Utilities & Helpers

Common functionality implemented using a curated set of stable dependencies:

- **SmartFormat** for formatting and templating
- **FluentValidation** for rule-based validation
- **Humanizer** for readable string and value transformations
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
- `Humanizer.Core` - Human-readable transformations
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
    ├─> WHO: UserState, UserId, UserName, TenantId, Roles
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
    └─> Delegates: All user properties to Operation
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
// Ad-hoc authorization (creates context)
var result = await evaluator.Evaluate(resource);

// Pipeline authorization (reuses context)
var result = await evaluator.Evaluate(resource, operationContext);

// Validators receive canonical AuthorizationContext
public class MyValidator : IAuthorizationValidator<MyResource>
{
    public Task<Result> Validate(
        AuthorizationContext<MyResource> context,
        CancellationToken ct)
    {
        // Access user, roles, resource, timing all in one place
        if (context.IsAuthenticated && 
            context.EffectiveRoles.Contains(requiredRole))
        {
            return Result.Success();
        }
        return Result.Failure("Unauthorized");
    }
}
```

## Logical Structure

```
Cirreum.Core/
├── Abstractions/
│   ├── Authorization/        # IAuthorizationEvaluator, IAuthorizableResource
│   ├── Identity/             # IUserState, IUserStateAccessor
│   ├── Messaging/            # Request/response contracts
│   └── Environment/          # Runtime and environment abstractions
├── Contexts/
│   ├── OperationContext.cs   # Canonical operational context
│   ├── RequestContext.cs     # Pipeline request context
│   └── AuthorizationContext.cs # Authorization decision context
├── Primitives/
│   ├── Identifiers/          # Operation IDs, correlation IDs
│   ├── Markers/              # Interface markers and tags
│   └── Metadata/             # Context metadata carriers
├── Patterns/
│   ├── Validators/           # Base validator implementations
│   ├── Interceptors/         # Pipeline interceptor patterns
│   └── Resources/            # Authorizable resource models
└── Utilities/
    ├── Formatting/           # SmartFormat helpers
    ├── Validation/           # FluentValidation extensions
    └── Csv/                  # CSV import/export utilities
```

## Usage

### Installation

```bash
dotnet add package Cirreum.Core
```

### Basic Context Usage

```csharp
// 1. Create operation context (done once in dispatcher)
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
// Define authorizable resource
public record DocumentResource : IAuthorizableResource
{
    public required string DocumentId { get; init; }
    public required string OwnerId { get; init; }
}

// Create validator
public class DocumentAccessValidator 
    : IAuthorizationValidator<DocumentResource>
{
    public Task<Result> Validate(
        AuthorizationContext<DocumentResource> context,
        CancellationToken ct)
    {
        // Owner can always access
        if (context.Resource.OwnerId == context.UserId)
            return Task.FromResult(Result.Success());

        // Admins can access
        if (context.EffectiveRoles.Any(r => r.Name == "Admin"))
            return Task.FromResult(Result.Success());

        return Task.FromResult(
            Result.Failure("You don't have permission to access this document"));
    }
}

// Evaluate authorization
var resource = new DocumentResource 
{ 
    DocumentId = "doc-123", 
    OwnerId = "user-456" 
};

var result = await authEvaluator.Evaluate(resource, operationContext);
if (result.IsFailure)
{
    return Forbid(result.Error);
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

- [Context Architecture](OPERATION-CONTEXT.md) - Detailed context composition guide
- [Authorization Flow](authorization/FLOW.md) - Authorization best practices
- [Authorization Sequence](authorization/SEQUENCE.md) - Authorization best practices

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