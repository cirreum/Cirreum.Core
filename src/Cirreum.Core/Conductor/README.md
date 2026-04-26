# 📘 **CONDUCTOR.md**

## The Operation Pipeline for Cirreum Domain Applications

Conductor is the core runtime engine behind Cirreum’s domain application model
(`DomainApplication` / `DomainApplicationBuilder`). It provides the operation
pipeline that every domain service runs through — validation, multi-stage
authorization, caching, telemetry — across any host (ASP.NET, Azure Functions,
Blazor WASM) and any cloud (Azure, AWS).

It provides:

- A fast, allocation-minimal dispatcher
- Automatic handler discovery from assemblies
- A deterministic intercept pipeline (validation → authorization → performance → caching)
- A flexible publisher for operation-side notifications
- A consistent DI-based lifetime model
- A clean API surface for registering and composing behavior

Conductor is designed to work in both low-level scenarios (raw control) and high-level domain application setups (via `AddDomainServices`).

---

## 🧩 Table of Contents

1. What Conductor Is
2. Key Concepts
3. Registration Modes
4. Settings & Configuration
5. DI Lifetime Model
6. Intercept Pipeline
7. Hot-Path Characteristics
8. Handler Discovery
9. Notification Publishing
10. Custom Intercepts
11. Error Handling Model
12. Unit Testing Guarantees
13. Appendix: DI Contract Table

---

## 🎯 What Conductor Is

Conductor is the **seam between ASP.NET infrastructure and Cirreum's domain
processing**. Everything before Conductor is traditional ASP.NET — routing,
model binding, middleware, endpoint authorization. The moment an endpoint
calls `DispatchAsync`, the operation enters Cirreum's domain pipeline:
validation, multi-stage authorization (including grants and resource ACLs),
performance tracking, caching, and telemetry.

This is not a mediator. Mediators like MediatR route messages to handlers.
Conductor orchestrates a full operation lifecycle where the handler itself
is an active participant — extending the authorization pipeline into
data-time resource ACL checks.

Designed around:

- CQRS patterns  
- Railway-oriented programming (`Result<T>`)  
- Deterministic pre- and post-handler intercepts  
- Runtime-agnostic behavior (Server, Functions, WASM)  
- Host-independent domain logic via `DomainApplication` / `DomainApplicationBuilder`

An operation runs through nested interceptors. The default domain
pipeline wraps like this:

```text
┌─ Validation ──────────────────────────────────────────────────────────────┐
│  ┌─ Authorization ─────────────────────────────────────────────────────┐  │
│  │  Stage 1 — Grants + Constraints (pre-handler)                       │  │
│  │  Stage 2 — Object Authorizers (pre-handler)                         │  │
│  │  Stage 3 — Policy Validators (pre-handler)                          │  │
│  │  ┌─ GrantedLookupAudit (Pattern C audit, post-handler) ──────────┐  │  │
│  │  │  ┌─ (Custom Intercepts) ────────────────────────────────────┐ │  │  │
│  │  │  │  ┌─ HandlerPerformance ───────────────────────────────┐  │ │  │  │
│  │  │  │  │  ┌─ QueryCaching ──────────────────────────────┐   │  │ │  │  │
│  │  │  │  │  │                                             │   │  │ │  │  │
│  │  │  │  │  │  ┌─ Handler ────────────────────────────┐   │   │  │ │  │  │
│  │  │  │  │  │  │  Stage 4 — Resource ACLs (in-handler)│   │   │  │ │  │  │
│  │  │  │  │  │  │  (IResourceAccessEvaluator)          │   │   │  │ │  │  │
│  │  │  │  │  │  └──────────────────────────────────────┘   │   │  │ │  │  │
│  │  │  │  │  │                                             │   │  │ │  │  │
│  │  │  │  │  └─────────────────────────────────────────────┘   │  │ │  │  │
│  │  │  │  └────────────────────────────────────────────────────┘  │ │  │  │
│  │  │  └──────────────────────────────────────────────────────────┘ │  │  │
│  │  └───────────────────────────────────────────────────────────────┘  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

Authorization is not a single layer — it's a pipeline within the pipeline.
Stages 1–3 run as a pre-handler intercept, gating the operation before
the handler executes. `GrantedLookupAudit` runs after Authorization to
catch Pattern C bypasses — when an `IGrantableLookupBase` operation
completes without the handler reading `IOperationGrantAccessor.Current`,
it emits a structured warning + OTel tag for observability (does not
deny — handler has already returned). Stage 4 (Resource ACLs) runs
*inside* the handler itself — the handler loads data, then calls
`IResourceAccessEvaluator` to check object-level permissions. The
handler is an active participant in authorization, not just the thing
being protected.

Each interceptor has a **pre-** phase (code before `await next()`) and a
**post-** phase (code after). For example, `QueryCaching` checks the
cache in its pre-phase, invokes the handler via `next()`, then populates
the cache in its post-phase. `HandlerPerformance` stamps the start time
pre-, then records duration post-.

All intercepts are opt-in and configurable. Notifications are a separate
concern — handlers produce them, and the `Publisher` fans them out after
the handler returns (see [Notification Publishing](#-notification-publishing)).

---

## 📐 Key Concepts

| Concept | Description |
|--------|-------------|
| **Dispatcher** | The central executor for operations. |
| **Publisher** | Publishes notifications generated by operation handlers. |
| **Intercepts** | Middleware-like components that wrap handler execution. Must call `next` at most once per invocation. |
| **Handlers** | Business logic units that execute the operation. |
| **OperationContext&lt;T&gt;** | Per-operation envelope. Owns the caller identity (`IUserState`), operation payload, timing, and correlation identifiers as first-class fields. Flows through the intercept chain. |
| **OperationHandlerWrapper** | Per-`TOperation` dispatcher wrapper: resolves the handler, materializes the intercept array, creates `OperationContext`, walks the pipeline, and records telemetry. |
| **PipelineCursor** | Walks the intercept chain with one allocation + one reusable delegate per operation, replacing a per-interceptor closure chain. |
| **Settings** | Publisher strategy, caching options, etc. |
| **ConductorBuilder** | Registers handlers & intercepts (raw mode). |
| **ConductorOptionsBuilder** | Configures lifetime, settings, domain intercepts. |

---

## 🛠️ Registration Modes

Conductor intentionally supports **three** layers of registration.

---

### 1. Raw Registration (`AddConductor`)

Use this when you want full manual control.

```csharp
services.AddConductor(
    builder => {
        builder.RegisterFromAssemblies(typeof(Startup).Assembly)
               .AddOpenIntercept(typeof(MyAudit<,>));
    },
    options => {
        options.WithLifeTime(ServiceLifetime.Transient);
        options.WithSetting(customSettings);
    });
```

**Raw mode characteristics**  

- You manually build the intercept pipeline  
- You manually register all default intercepts  
- No automatic pipeline is applied  
- `AddCustomIntercepts` is **not allowed** here  
- Best for advanced frameworks / library scenarios  

---

### 2. Application Mode (`AddDomainServices`)

This gives you the opinionated, safe defaults:

```csharp
services.AddDomainServices(Configuration);
```

Automatically includes:

- Handler assembly scanning  
- Full default pipeline  
- Settings bound from configuration  
- Dispatcher lifetime (default: Transient)  
- Custom-intercept insertion point  

Extend the pipeline safely:

```csharp
services.AddDomainServices(Configuration, options => {
    options.AddCustomIntercepts(builder =>
        builder.AddOpenIntercept(typeof(DomainAudit<,>)));
});
```

---

### 3. DomainBuilder Integration — Recommended

Highest-level integration for apps that should not touch Conductor internals directly:

```csharp
builder.ConfigureConductor(opts => {
    opts.WithLifeTime(ServiceLifetime.Scoped);
    opts.AddCustomIntercepts(b => b.AddOpenIntercept(typeof(DomainAudit<,>)));
});
```

This is ideal for application developers. Framework/library code wires Conductor; applications configure via a simplified surface.

---

## ⚙️ Settings & Configuration

Conductor settings control runtime behavior:

| Setting | Description |
|---------|-------------|
| `PublisherStrategy` | Determines how notification handlers are invoked. |
| `Cache.Provider` | InMemory, Hybrid, Distributed, None. |
| `Cache.DefaultExpiration` | Global TTL for cached queries. |

### From configuration

```csharp
services.AddConductor(Configuration);
```

### Or manually

```csharp
services.AddConductor(
    _ => { },
    opts => opts.WithSetting(mySettings));
```

Settings are ultimately materialized into a singleton `ConductorSettings` instance and also consumed during registration (e.g., cache setup).

---

## 🧬 DI Lifetime Model

Lifetimes are part of Conductor’s public contract and are enforced by tests.

### Default Lifetimes

| Component | Lifetime | Notes |
|----------|----------|-------|
| **Dispatcher** | Transient | Overrideable via options. |
| **Publisher** | Mirrors Dispatcher | Enforced by tests. |
| **Handlers** | Transient | Always. |
| **Intercepts** | Same as Dispatcher | Keeps pipeline behavior consistent. |
| **Settings** | Singleton | Immutable runtime configuration. |
| **Facades** | Same as Dispatcher | `IDispatcher`, `IPublisher`, `IConductor`. |

### Scope validation

When `ServiceProviderOptions.ValidateScopes = true`:

- Resolving a **scoped** dispatcher from the **root** container throws.  
- Having a **singleton** that depends on a **scoped** dispatcher throws.

These behaviors are covered by tests such as:

- `ScopedDispatcher_ResolvedFromRoot_ThrowsOnValidateScopes`  
- `ScopedDispatcher_UsedFromSingleton_ThrowsOnValidateScopes`

---

## 🧵 Intercept Pipeline

### Raw Mode

In raw `AddConductor`, you fully control the intercept pipeline:

```csharp
services.AddConductor(
    builder => {
        builder.RegisterFromAssemblies(typeof(Startup).Assembly)
               .AddOpenIntercept(typeof(Validation<,>))
               .AddOpenIntercept(typeof(Authorization<,>))
               .AddOpenIntercept(typeof(HandlerPerformance<,>))
               .AddOpenIntercept(typeof(QueryCaching<,>));
    },
    options => {
        options.WithSetting(customSettings);
    });
```

The order is exactly the order you register.

> [!NOTE]
> Raw mode does **not** auto-wire `GrantedLookupAudit<,>`. If you use the grant
> pipeline (`IGrantableLookupBase` operations) and want the Pattern C runtime
> audit, register it manually after `Authorization<,>`:
> `.AddOpenIntercept(typeof(Cirreum.Authorization.Operations.Grants.GrantedLookupAudit<,>))`.
> `AddDomainServices` registers it automatically as part of the standard pipeline.

### Domain Mode (Deterministic)

When using `AddDomainServices`, Conductor applies a standard domain
pipeline. Registration order runs **outermost → innermost** (the first
listed wraps everything below it; the last listed wraps the handler):

```text
Validation              ← outermost (runs first pre-handler, last post-handler)
→ Authorization
→ GrantedLookupAudit    (Pattern C audit; auto-registered by AddDomainServices)
→ (Custom Intercepts)
→ HandlerPerformance
→ QueryCaching          ← innermost (wraps the handler directly)
```

This is enforced by tests:

- `DomainPipeline_Intercepts_AreRegisteredInExpectedOrder`  
- `Intercepts_AreRegisteredInExpectedOrder_ForRawAddConductor`

---

## ⚡ Hot-Path Characteristics

The dispatcher is engineered as a "center-of-the-sun" hot path. Every
authorized operation walks it, so allocations and branches matter.

### Two Execution Paths

```text
BYPASS (rare) — zero intercepts registered
    └─> Directly invoke handler.HandleAsync(operation, ct)
        • No OperationContext, no pipeline walk
        • Reserved for frameworks that ship no default intercepts

TYPICAL — at least one intercept registered (the default: 4 intercepts)
    └─> Materialize intercept array (cast, do not copy)
        └─> GetUser() + OperationContext.Create(UserState, Operation, ...)
            └─> PipelineCursor walks the chain
                └─> Handler executes at the end of the chain
```

### Allocation Budget (per operation, typical path)

| Allocation | Purpose |
|---|---|
| `OperationContext<T>` | Per-operation envelope (UserState, Operation, IDs, timing) |
| `PipelineCursor<T[,TResultValue]>` | Pipeline walker |
| 1 × bound delegate | Cursor's `NextDelegate`, reused for every hop |
| `Result<T>` | Terminal return value |

**~5 allocations per operation**, independent of interceptor count. Each
interceptor contributes zero additional closure allocations because the
cursor's bound delegate is reused at every hop.

### Skipped Work

- **`Unsafe.As<T>(operation)`** instead of `(T)operation` — dispatcher cache
  is keyed by `typeof(TOperation)`, so the isinst check is guaranteed
  redundant.
- **`GetService<IEnumerable<T>>()!`** instead of `GetServices<T>()` —
  skips `GetRequiredService`'s null-guard + throw-helper. `IEnumerable<T>`
  is always registered by MS.DI (empty array if no components).
- **DI array cast** (`as T[] ?? [..src]`) — MS.DI materializes the
  enumerable as `T[]` internally; we skip the extra `.ToArray()` copy.
- **`ICollection<T> { Count: 0 }` early-exit** — triggers the BYPASS path
  without enumerating.

### Interceptor Contract

> Interceptors MUST invoke `next` **at most once** per `HandleAsync`
> invocation. The cursor uses a shared mutable index; calling `next`
> twice advances past the intended interceptor.

All built-in interceptors comply. Custom interceptors that need
retry/loop/fan-out must snapshot state and build their own cursor.

---

## 📊 Telemetry

Conductor publishes metrics and distributed traces via OpenTelemetry. All
instrumentation is zero-cost when no OTel listeners are attached.

### Operation Metrics (`OperationTelemetry`)

| Metric | Type | Description |
|--------|------|-------------|
| `conductor.requests.total` | Counter | Total dispatched operations |
| `conductor.requests.failed` | Counter | Failed operations (excludes cancellations) |
| `conductor.requests.canceled` | Counter | Canceled operations |
| `conductor.requests.duration` | Histogram (ms) | Pipeline processing duration |

All metrics are tagged with `operation.type`, `operation.status` (success/failure/canceled),
and `error.type` on failure.

### Activity Tracing

The dispatcher creates a child `Activity` per operation (source: `Cirreum.Conductor.Dispatcher`).
The activity carries `operation.type`, `response.type`, and status tags. Authorization,
grant resolution, and cache operations appear as nested spans under the same trace.

### Cache Metrics (`CacheTelemetry`)

Query caching telemetry is handled by the `InstrumentedCacheService` decorator —
not inline in the `QueryCaching` intercept. The decorator wraps any `ICacheService`
implementation and records:

| Metric | Type | Description |
|--------|------|-------------|
| `cirreum.cache.operations` | Counter | Total cache operations |
| `cirreum.cache.duration` | Histogram (ms) | Operation duration |

| Tag | Values | Description |
|-----|--------|-------------|
| `cirreum.cache.status` | `hit`, `miss` | Whether the value was served from cache |
| `cirreum.cache.caller` | *(cache key)* | The cache key identifying the operation |
| `cirreum.cache.consumer` | `query-caching`, `grant-resolution`, `other` | Subsystem that triggered the cache operation |

Each known subsystem gets its own keyed `ICacheService` instance with the consumer
tag baked in at registration time (see `CacheConsumers`). Any code that injects a
plain (non-keyed) `ICacheService` is tagged `"other"`. This means all consumers —
query caching, grant resolution, and any future consumers — get cache metrics for
free, and dashboards can slice by subsystem at zero runtime cost.

### Wiring

```csharp
builder.Services.AddOpenTelemetry()
    .AddCirreum()       // registers all Cirreum sources + meters
    .UseOtlpExporter();
```

---

## 🧲 Handler Discovery

Conductor automatically discovers handlers from assemblies via:

```csharp
builder.RegisterFromAssemblies(assemblies);
```

This wiring registers:

- `IOperationHandler<TOperation, TResultValue>` implementations  
- `INotificationHandler<TNotification>` implementations  

All handlers are registered as **Transient** to avoid stale state and ensure operation isolation.

This is enforced by tests such as:

- `OperationHandlers_AreAlwaysTransient`

---

## 📡 Notification Publishing

The `Publisher` is responsible for notifying `INotificationHandler<TNotification>` implementations.  
Its behavior is controlled by `PublisherStrategy`:

```csharp
public enum PublisherStrategy {
    /// <summary>
    /// Handlers are invoked one at a time, in order. 
    /// If one fails, subsequent handlers still execute.
    /// </summary>
    Sequential,

    /// <summary>
    /// Handlers are invoked one at a time, in order. 
    /// If one fails, subsequent handlers will not be executed.
    /// </summary>
    FailFast,

    /// <summary>
    /// All handlers are invoked simultaneously.
    /// Waits for all to complete before returning.
    /// </summary>
    Parallel,

    /// <summary>
    /// Handlers are invoked asynchronously without waiting for completion.
    /// If one fails, subsequent handlers still execute.
    /// Returns immediately after queueing.
    /// </summary>
    FireAndForget
}
```

### Sequential

- Handlers execute **one at a time**, in registration order.  
- If a handler throws, the exception is captured according to the error model.  
- Remaining handlers **still run**.

### FailFast

- Handlers execute **one at a time**, in registration order.  
- If a handler throws, publishing **stops immediately**.  
- Remaining handlers are **not invoked**.

### Parallel

- Handlers execute **concurrently**.  
- Publishing waits for **all** handlers to complete.  
- Exceptions are aggregated/handled according to the error model.

### FireAndForget

- Handlers are queued to run asynchronously.  
- Publishing **returns immediately** and does not await completion.  
- Best for non-critical notifications where latency is more important than completion guarantees.

Your unit tests validate that different `ConductorSettings` presets correctly map to these strategies and that the registration pipeline wires `Publisher` with the configured strategy.

---

## 🔌 Custom Intercepts

Custom intercepts allow you to extend the pipeline with domain-specific concerns  
(e.g., tenant isolation, auditing, extra validation).

## Custom Mode

In raw `AddConductor`, you control the pipeline directly using `ConductorBuilder`:

```csharp
services.AddConductor(
    builder => {
        builder.RegisterFromAssemblies(typeof(Startup).Assembly)
               .AddOpenIntercept(typeof(MyCustomIntercept<,>));
    },
    options => {
        options.WithSetting(customSettings);
    });
```

> ❗ `ConductorOptionsBuilder.AddCustomIntercepts(...)` is **not allowed** in this mode and will throw.  
> Use `builder.AddOpenIntercept(...)` instead.

## Domain Mode

In `AddDomainServices`, you get a safe extension point between Authorization and Performance:

```csharp
services.AddDomainServices(Configuration, options => {
    options.AddCustomIntercepts(builder => {
        builder.AddOpenIntercept(typeof(DomainAudit<,>));
        builder.AddOpenIntercept(typeof(TenantIsolation<,>));
    });
});
```

Here:

- Validation and Authorization are wired by the framework.  
- Your custom intercepts are inserted after Authorization.  
- Performance and caching intercepts still run afterward.

---

## 🛑 Error Handling Model

Conductor’s dispatcher and publisher follow a consistent error model:

### Non-Fatal Exceptions

- Typically domain or validation failures.  
- Captured and converted into `Result<T>.Failure`.  
- Calling code can pattern-match on the failure without throwing.

### Fatal Exceptions

- Out-of-memory, thread-abort, and other catastrophic errors.  
- Re-thrown and **bubble out** of the pipeline.  
- Not converted into `Result<T>` failures.

### Cancellation

- If the handler honors `CancellationToken`, cancellation flows naturally.  
- The dispatcher ensures `OperationCanceledException` is preserved.  

These behaviors are tested in:

- `DispatchAsync_FatalException_IsNotConvertedToResultFailure`  
- `DispatchAsync_PropagatesCancellation_WhenHandlerHonorsToken`  
- `DispatchAsync_NonFatalException_IsConvertedToResultFailure`

---

## 🧪 Unit Testing Guarantees

Conductor has a comprehensive test suite around:

### Lifetimes

- Default dispatcher is Transient.  
- Dispatcher lifetime mirrors into Publisher and facades.  
- `AddDomainServices` can override lifetime and tests confirm the override.  
- Misconfigured scopes (scoped-used-from-singleton, root-resolved scoped) throw.

### Pipelines

- Raw pipeline: order matches registration.  
- Domain pipeline: Validation → Authorization → Custom → Performance → Caching.  
- `AddConductor` cannot be called twice on the same `IServiceCollection`.

### Settings

- Settings passed via `WithSetting` are honored.  
- Cache configuration flows to the cache layer.  

These tests collectively ensure that Conductor’s DI and pipeline contracts cannot regress silently.

---

## 📋 Appendix: DI Contract Table

| Service | Lifetime | Notes |
|--------|----------|-------|
| `Dispatcher` | Configurable (default: Transient) | Core executor. |
| `Publisher` | Mirrors Dispatcher | Uses configured `PublisherStrategy`. |
| `IDispatcher` | Matches Dispatcher | Facade over `Dispatcher`. |
| `IPublisher` | Matches Dispatcher | Facade over `Publisher`. |
| `IConductor` | Matches Dispatcher | Facade over `Dispatcher`. |
| `IOperationHandler<,>` | Transient | Business logic handlers. |
| `INotificationHandler<>` | Transient | Notification handlers. |
| `IIntercept<,>` | Matches Dispatcher | Pipeline components. |
| `ConductorSettings` | Singleton | Immutable configuration snapshot. |

---

**End of CONDUCTOR.md**  
