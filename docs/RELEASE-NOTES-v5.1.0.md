# Cirreum.Core 5.1.0 — `IRemoteConnection` joins the `RemoteServices` family

Adds a caller-side typed handle for long-lived bidirectional connections — `IRemoteConnection` — alongside the existing `RemoteClient` in the same `Cirreum.RemoteServices` namespace. Where `RemoteClient` abstracts request/response HTTP calls, `IRemoteConnection` abstracts persistent bidirectional channels (SignalR Hub clients, raw WebSocket clients, gRPC streaming, and similar).

Strictly additive. No changes to existing types or behavior.

---

## Why this release exists

`Cirreum.Core`'s `Cirreum.RemoteServices` namespace already centralizes the abstractions for "the caller's view of remote resources":

- `RemoteClient` — abstract base for typed outbound HTTP clients
- `RemoteClientLogging`, `RemoteClientTelemetry` — observability scaffolding
- `RemoteServiceOptions`, `AuthorizationHeaderSettings`, `ResponseWithHeadersT` — supporting types

The shape is request/response-flavored. But callers also need long-lived bidirectional connections — WASM apps subscribing to backend pushes via SignalR, server-side microservices subscribing to events from another service via WebSockets, voice/IVA pipelines streaming over raw WS. Those callers had no equivalent abstraction in the framework. They either used SignalR's `HubConnection` directly (locking themselves to SignalR specifically) or rolled their own typed wrappers.

Joining `IRemoteConnection` to the `RemoteServices` family closes that gap. Same namespace, same package, same family vocabulary as `RemoteClient`. Both are caller-side outbound abstractions; they differ only in shape (request/response vs. long-lived bidirectional).

---

## What's new

All new types are in `Cirreum.RemoteServices`.

### `IRemoteConnection`

```csharp
public interface IRemoteConnection {
    string ConnectionId { get; }
    RemoteConnectionState State { get; }
    event EventHandler<RemoteConnectionStateChangedEventArgs>? StateChanged;
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    IDisposable On<T>(string method, Func<T, Task> handler);
    Task SendAsync<T>(string method, T payload, CancellationToken cancellationToken = default);
}
```

The interface is **transport-agnostic** — concrete impls in the `Cirreum.Runtime.Invocation.{Source}.Wasm` family adapt SignalR's `HubConnection`, raw `ClientWebSocket`, or other transports onto this contract.

### `RemoteConnectionBase`

Abstract base providing the public-surface state machine and `StateChanged` event plumbing. Derived impls in transport-specific packages call `TransitionTo(newState)` when their underlying transport's state changes.

```csharp
public abstract class RemoteConnectionBase : IRemoteConnection {
    protected RemoteConnectionBase(ILogger logger);
    protected ILogger Logger { get; }
    public RemoteConnectionState State { get; }
    public event EventHandler<RemoteConnectionStateChangedEventArgs>? StateChanged;
    public abstract string ConnectionId { get; }
    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
    public abstract Task DisconnectAsync(CancellationToken cancellationToken = default);
    public abstract IDisposable On<T>(string method, Func<T, Task> handler);
    public abstract Task SendAsync<T>(string method, T payload, CancellationToken cancellationToken = default);
    protected void TransitionTo(RemoteConnectionState newState);
}
```

### `RemoteConnectionState`

```csharp
public enum RemoteConnectionState {
    Disconnected = 0,
    Connecting   = 1,
    Connected    = 2,
    Reconnecting = 3,
    Disconnecting = 4,
}
```

### `RemoteConnectionStateChangedEventArgs`

```csharp
public sealed record RemoteConnectionStateChangedEventArgs(
    RemoteConnectionState PreviousState,
    RemoteConnectionState NewState);
```

---

## How it pairs with `RemoteClient`

```csharp
using Cirreum.RemoteServices;

public sealed class ChatPage(
    IRemoteConnection chat,           // long-lived SignalR/WS connection to the backend
    ChatApiClient api                 // typed outbound HTTP client (derives from RemoteClient)
) {
    public async Task Initialize() {
        var history = await api.LoadHistoryAsync();   // request/response — RemoteClient family
        await chat.ConnectAsync();
        chat.On<ChatMessage>("Receive", async msg => { /* ... */ });
        await chat.SendAsync("SendMessage", "hello"); // bidirectional/push — IRemoteConnection family
    }
}

public sealed class ChatApiClient(HttpClient http, ILogger<ChatApiClient> log, IDomainEnvironment env)
    : RemoteClient(http, log, env) { /* existing pattern */ }
```

One `using` covers both abstractions. Callers compose request/response and long-lived patterns side-by-side without crossing framework boundaries.

---

## Cross-host applicability

Both abstractions in the `RemoteServices` family are cross-host — they describe **the caller's view of remote resources**, regardless of whether the caller is a WASM app, a server-side microservice, a serverless function, or anything else. Concrete impls of `IRemoteConnection` ship in the `Cirreum.Runtime.Invocation.{Source}.Wasm` family for the WASM case; the same interface is consumable by server-side code that needs long-lived outbound connections (e.g., service-to-service event subscriptions).

---

## Why this lives in `Cirreum.Core` rather than a separate package

Earlier sketches placed the long-lived connection abstractions in a separate package (`Cirreum.Connection.Client`). Putting them in `Cirreum.Core` is honest because:

- They genuinely belong to the same family as `RemoteClient` (caller-side, outbound, cross-host)
- True same-package pairing — no cross-package namespace extension
- One package, one namespace, one `using` — maximum discoverability for consumers
- Independent versioning would be a *cost* not a benefit; family members evolve together
- "Dead weight" concern doesn't apply: the abstraction is genuinely useful in any host
- Three small files; trivial size addition

---

## Coordinated downstream work

This release unblocks the L5 client-side adapters in the Invocation family rework (per [ADR-0002](https://github.com/cirreum/Cirreum.DevOps/blob/main/docs/adr/0002-unified-invocation-context.md)):

- `Cirreum.Runtime.Invocation.SignalR.Wasm` (next) — derives from `RemoteConnectionBase`, wraps SignalR's `HubConnection` to provide an `IRemoteConnection` impl.
- `Cirreum.Runtime.Invocation.WebSockets.Wasm` (next) — same shape against `ClientWebSocket`.

Each adapter package references `Cirreum.Core 5.1.0+` for `IRemoteConnection` and friends. No new abstraction package is needed.

---

## Architectural principle

> **Caller-side outbound abstractions live together in `Cirreum.RemoteServices`, regardless of shape.**

Request/response (`RemoteClient`) and long-lived bidirectional (`IRemoteConnection`) are both *the caller's view of remote resources*. They differ in shape but share a concern. Co-locating them in one namespace and package gives consumers a single mental model for "I am calling something remote" — and a single `using` to access both flavors.

---

## Compatibility

- **Strictly additive.** No existing types, properties, or behaviors changed. Source-compatible with `5.0.x`.
- **Default interface methods** are not used; `IRemoteConnection` is a clean abstract surface.
- **No new dependencies.** All new types use `System.*` and `Microsoft.Extensions.Logging` already referenced by `Cirreum.Core`.

---

## See also

- `CHANGELOG.md` — condensed change list for `5.1.0`.
