# Cirreum.Core 5.2.0 — Inbound distributed message dispatch foundations

Adds the L3 (Core) abstractions needed for framework-level inbound distributed message dispatch across multi-node and multi-head deployments. Strictly additive — no changes to existing types or behavior. The L5 runtime implementation (hosted receiver service, default node-ID resolution, `DefaultTransportPublisher` ApplicationProperty enrichment) ships in a coordinated follow-up `Cirreum.Runtime.Messaging` release once this version is on NuGet.

---

## Why this release exists

`Cirreum.Core`'s `Cirreum.Messaging` namespace is rich on the **send** side. The existing pipeline gives apps a clean, Conductor-integrated way to publish:

- `DistributedMessage` (implements `INotification`) — author authoring is identical to in-process events
- `MessageDefinitionAttribute` — stable identifier + version metadata
- `DistributedMessageEnvelope` — wire-format wrapper with type info, producer ID, serialized payload
- `IDistributedTransportPublisher` — transport seam (defaults to `EmptyTransportPublisher` when no provider is wired)
- `DistributedMessageHandler<T>` — open-generic notification interceptor that routes publishes through the transport

The **receive** side has primitives in `Cirreum.Messaging` (the cross-provider Common package) — `IMessagingSubscriptionReceiver`, `IMessagingClient.UseSubscription(...)`, `DistributedMessageEnvelope.FromJson(...)` — but no framework-level dispatch. Apps write their own subscribe loops, manual `if/else` ladders over `MessageIdentifier`/`MessageVersion`, and manual envelope deserialization per message type.

This works for one or two messages. It breaks down quickly when:

- A bank app has three deployed heads (API, Email, IVA) and needs each to consume head-specific messages plus broadcast messages
- Multi-replica deployments need to converge on dynamic state (registry sync for delegation evidence, ApiKey instances, signed-request keys) — every node of every head must receive every relevant change within seconds
- The same `if/else` ladder gets duplicated per head per registry, with no DI scoping, pipeline behaviors, or telemetry

The solution is the symmetric mirror of the send side: an open-generic notification interface for inbound, dispatched via Conductor exactly the way the outbound interceptor uses Conductor. Apps write per-type handler classes, drop them in, done. The framework owns the receive loop, envelope deserialization, self-echo prevention, and dispatch.

This release lands the three small Core abstractions plus the centralized `PublishedAt` stamp those abstractions depend on. The hosted receiver service that consumes them lives in `Cirreum.Runtime.Messaging` and ships next.

---

## What's new

All new types are in `Cirreum.Messaging`.

### `INodeIdProvider`

```csharp
namespace Cirreum.Messaging;

public interface INodeIdProvider {
    string NodeId { get; }
}
```

Provides a stable identifier for the **current process replica**, distinct from the existing `ProducerId` (which identifies the **application/head** and is shared across replicas of the same head).

The distinction matters for echo prevention. In a multi-replica deployment, every replica of every head receives every message published to the topic via the head's shared subscription. The replica that originated the publish has already applied the change locally — it must skip the redelivered echo. Other replicas of the same head (different `NodeId`, same `ProducerId`) must process it. Filtering on `ProducerId` alone would suppress messages across replicas of the same head, breaking convergence.

`NodeId` is consumed by the L5 `DefaultTransportPublisher` (stamps `cirreum.node` on the outgoing message's application properties) and by the L5 `DistributedMessageReceiver` (compares against incoming `cirreum.node` for cheap pre-deserialization self-echo skip). Apps that need deterministic NodeIds for testing — or that read node identity from a bespoke infrastructure metadata service — register a custom `INodeIdProvider` implementation in DI rather than threading values through configuration; this matches the rest of the framework's "swap-in via DI" customization pattern.

### `DefaultNodeIdProvider`

```csharp
namespace Cirreum.Messaging;

public sealed class DefaultNodeIdProvider : INodeIdProvider {

    public DefaultNodeIdProvider() {
        this.NodeId = Resolve();
    }

    public string NodeId { get; }

    private static string Resolve() {
        // 1. CONTAINER_APP_REPLICA_NAME (Azure Container Apps)
        // 2. WEBSITE_INSTANCE_ID (Azure App Service)
        // 3. HOSTNAME (Kubernetes, generic container)
        // 4. {MachineName}:{ProcessId} (local dev)
        // 5. Generated GUID (last resort)
        // ...
    }
}
```

Parameterless default implementation of `INodeIdProvider`. The resolution chain handles every common deployment platform without configuration; the GUID fallback ensures uniqueness in environments where no env hint exists (e.g., minimal local tests).

Ships in Core (not L5) because the impl has zero external dependencies — just `System.Environment`, `System.Guid`. Same precedent as `EmptyTransportPublisher` (the default `IDistributedTransportPublisher`) living in Core alongside its abstraction.

The L5 hosting extension registers it via `services.TryAddSingleton<INodeIdProvider, DefaultNodeIdProvider>()`. Apps that need custom resolution call `services.AddSingleton<INodeIdProvider>(...)` first with their own implementation; the framework's `TryAddSingleton` then no-ops.

### `DistributedMessageReceived<TMessage>`

```csharp
namespace Cirreum.Messaging;

using Cirreum.Conductor;

public sealed record DistributedMessageReceived<TMessage>(
    TMessage Message,
    DistributedMessageEnvelope Envelope)
    : INotification
    where TMessage : DistributedMessage;
```

Wraps an inbound distributed message for dispatch via Conductor's notification pipeline. Carries both the deserialized typed message and the original envelope so handlers can inspect metadata (`ProducerId`, `MessageIdentifier`, `MessageVersion`, `PublishedAt`) without extra lookups.

**Why a wrapper type:** `DistributedMessage` itself implements `INotification` and is already intercepted by the outbound `DistributedMessageHandler<T>` interceptor. Publishing a deserialized `DistributedMessage` directly via Conductor would trigger the outbound interceptor — re-publishing the received message back to the bus in an infinite loop. The wrapper type is a different `INotification` shape that the outbound interceptor doesn't catch, so inbound dispatch flows to app handlers without re-entering the send path.

**App-facing usage:**

```csharp
public sealed class EvidenceInstanceChangeHandler
    : INotificationHandler<DistributedMessageReceived<EvidenceInstanceChangedV1>>
{
    public Task HandleAsync(
        DistributedMessageReceived<EvidenceInstanceChangedV1> notification,
        CancellationToken ct)
    {
        var message  = notification.Message;
        var producer = notification.Envelope.ProducerId;   // for audit
        // ...react to the change locally...
        return Task.CompletedTask;
    }
}
```

No new framework-defined handler interface, no per-handler registration extensions, no custom dispatcher — Conductor's existing auto-discovery, DI scoping, and pipeline behaviors apply uniformly. App devs use the patterns they already use everywhere else in Cirreum.

### `ReceiverOptions`

```csharp
namespace Cirreum.Messaging;

public sealed class ReceiverOptions {

    public const string ConfigurationName = "Receiver";
    public const string ReceiverInstanceConfigurationName = "InstanceKey";
    public const string QueueConfigurationName = "QueueName";
    public const string TopicConfigurationName = "TopicName";
    public const string SubscriptionConfigurationName = "SubscriptionName";

    public required string InstanceKey { get; init; }

    public string? QueueName { get; init; }           // competing-consumer source
    public string? TopicName { get; init; }           // broadcast source
    public string? SubscriptionName { get; init; }    // required when TopicName is set

    public int MaxConcurrency { get; init; } = 1;
    public int PrefetchCount { get; init; } = 10;
    public TimeSpan MaxAutoLockRenewalDuration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan GracefulShutdownTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
```

Configuration class for the distributed message receiver (consumed by `Cirreum.Runtime.Messaging` in the follow-up release). Binds from `Cirreum:Messaging:Distribution:Receiver`.

**Symmetric with the send side.** Just as `SenderOptions` exposes both `QueueName` and `TopicName` so the publisher can route each message according to its `MessageDefinitionAttribute.Target`, `ReceiverOptions` exposes both a queue source and a topic source. Either or both may be configured; at least one is required.

**Queue vs Topic — when to use which:**

| Source | Delivery | Use Case |
|---|---|---|
| `QueueName` | Competing consumers — exactly one consumer processes each message | Work distribution (emails to send, payments to process, IVA call tasks) |
| `TopicName` + `SubscriptionName` | Broadcast — each subscription receives a copy of every published message | Cross-head event reactions (registry sync, kill switches, config changes) |

A worker head typically configures both — a queue for its work, a topic subscription for broadcast events. A read-only head (e.g., the API) typically configures only a topic subscription.

**`SubscriptionName` is the per-head differentiator for the topic source.** All heads run the same binary; each deployment configures a different `SubscriptionName` (e.g., `"api-head"`, `"email-head"`, `"iva-head"`). The broker delivers every published message to every subscription. Each head's filter rules — configured server-side in infrastructure-as-code, not Cirreum code — narrow what arrives at its subscription.

The L5 registration-site guard checks `IsNullOrEmpty(InstanceKey)` and the presence of at least one source (`QueueName` set, or both `TopicName` and `SubscriptionName` set). Incomplete configuration leaves the receiver unregistered — sender-only configuration is the default state.

Node identity is intentionally not a config concern. `DefaultNodeIdProvider`'s resolution chain handles every common deployment platform automatically; apps with bespoke requirements replace the implementation in DI. Threading node identity through `ReceiverOptions` would have created a footgun — a literal value in appsettings.json is shared by every replica reading that config, silently breaking the per-replica uniqueness echo prevention depends on.

### `DistributedMessageEnvelope.PublishedAt` *(modification, additive)*

```csharp
public record DistributedMessageEnvelope {
    // ... existing properties ...

    public DateTimeOffset? PublishedAt { get; init; }   // NEW — nullable for backward compat
}
```

Stamped at envelope creation by `Create<TMessage>(...)` and `CreateWithSerializer<TMessage>(...)`, populated with `DateTimeOffset.UtcNow`. Every envelope flows through these factories, so every publisher participates without per-sender opt-in.

Nullable to preserve backward compatibility: envelopes serialized prior to this release deserialize with `PublishedAt = null`. Handlers that care about it (latency telemetry, replay detection, audit timing) check `HasValue` and fall through gracefully for legacy envelopes.

**Why portable matters:** Service Bus has its own `EnqueuedTimeUtc`. AWS SNS has its own message timestamp. The envelope's `PublishedAt` works the same across brokers and survives broker-to-broker forwarding without translation. Apps that build cross-broker pipelines (typical in cloud-portable architectures) don't need broker-specific property reads to recover publish timing.

---

## How it pairs with the existing send side

Symmetric architecture, single mental model:

| Send side (existing) | Receive side (this release + L5 follow-up) |
|---|---|
| `IDistributedTransportPublisher` (framework-implemented seam) | `INotificationHandler<DistributedMessageReceived<T>>` (app-implemented) |
| `DistributedMessageHandler<T>` — Conductor-discovered open-generic outbound interceptor | App's `INotificationHandler<DistributedMessageReceived<T>>` — Conductor-discovered open-generic inbound handler |
| `DefaultBatchProcessor : IHostedService` (L5, outbound queue → broker) | `DistributedMessageReceiver : IHostedService` (L5, broker → handler invocations) |
| `SenderOptions` (existing) | `ReceiverOptions` (this release) |
| Default no-op: `EmptyTransportPublisher` (always registered) | Default behavior: no receiver registered unless `Receiver` config section is present |

Both sides flow through Conductor. Both use open-generic notification handlers. Both are auto-discovered. Authors of message types and authors of handlers use the same patterns they use elsewhere in Cirreum.

---

## Cross-provider compatibility

Everything in this release is cross-provider by construction. The new types sit at L3 (Core), reference only `Cirreum.Conductor` (already in Core), and don't touch any broker-specific surface. The L5 receiver consumes the cross-provider `IMessagingSubscriptionReceiver` and `IMessagingClient` from `Cirreum.Messaging` (Common layer) — same primitives the existing senders use.

The follow-up L5 release adds four application properties to outgoing messages via `OutboundMessage.Properties` (the cross-provider equivalent of "broker headers/attributes"):

- `cirreum.identifier` — message identifier (filterable for routing)
- `cirreum.version` — message version (filterable for version-aware routing)
- `cirreum.producer` — head/app identity (audit + head-level routing)
- `cirreum.node` — replica identity (echo prevention)

Each broker maps `OutboundMessage.Properties` to its native filterable property bag (Service Bus `ApplicationProperties`, AWS SNS message attributes, Kafka headers, NATS headers). The metadata is identical across brokers — only the filter expression syntax differs, and that lives in infrastructure-as-code per deployment.

---

## Routing convention

Filter rules live outside Cirreum, but the convention they apply against ships with the framework. The receiver-side filter mechanism is the broker's; the data being filtered is universal:

| Broker | Filter mechanism |
|---|---|
| Azure Service Bus | SQL filter rules on subscriptions, against `ApplicationProperties` |
| AWS SNS | Filter policies (JSON) on subscriptions |
| Kafka | Consumer-side filtering or partitioning by routing key |
| NATS JetStream | Subject-pattern filtering |

The natural routing axis is the **identifier hierarchy** — message identifiers like `email.send.v1`, `iva.process-call.v1`, `auth.evidence.changed.v1` make audience implicit in the name. A subscription rule like `cirreum.identifier LIKE 'auth.%' OR cirreum.identifier LIKE 'email.%'` routes a head's relevant traffic without inventing a separate "channel" or "audience" concept.

No new attribute property is required to enable this. The metadata is already on the wire.

---

## Coordinated downstream work

This release is the L3 half of a two-cycle delivery. The L5 follow-up in `Cirreum.Runtime.Messaging` ships:

- `DistributedMessageReceiver : IHostedService` — the receive loop, envelope deserialization, `DistributedMessageReceived<T>` wrapping, Conductor publish, self-echo skip
- `DefaultNodeIdProvider` — `INodeIdProvider` implementation running the env-var → machine-name → GUID resolution chain
- Modifications to `DefaultTransportPublisher` — stamps the four application properties on every outgoing `OutboundMessage`
- Modifications to `HostingExtensions.AddDistributedMessaging` — conditional receiver registration when the `Receiver` config section is present

The downstream package references `Cirreum.Core 5.2.0+` for `INodeIdProvider`, `DistributedMessageReceived<T>`, `ReceiverOptions`, and the envelope's `PublishedAt`. It can't ship until this release is on NuGet and indexed.

The first production consumer is the upcoming **Delegation / On-Behalf-Of (OBO) authorization track**, which uses `DistributedMessageReceived<EvidenceInstanceChangedV1>` (and similar) to propagate dynamic evidence-resolver-instance changes across heads. The same infrastructure benefits any future dynamic-registry pattern (ApiKey instance sync, signed-request instance sync, identity-provider instance sync, app-defined cross-head event reactions).

---

## Architectural principle

> **Inbound dispatch is a Conductor concern; the framework owns receive and envelope; apps own handlers.**

Cirreum already uses Conductor for in-process notifications and for outbound distributed publishes. Routing inbound through the same pipeline closes the loop:

- One mental model for authors of message types: `DistributedMessage` implements `INotification`
- One mental model for authors of handlers: `INotificationHandler<DistributedMessageReceived<T>>`
- One discovery mechanism: Conductor's existing assembly scan
- One DI scoping model: Conductor's per-dispatch scope
- One pipeline behavior model: Conductor's existing intercepts apply

No new "messaging handler" interface, no parallel dispatch infrastructure, no separate registration extensions. The work the receiver does is mechanical (loop, deserialize, wrap, publish) and lives in L5; everything reusable is in Core and is just data shapes.

---

## Compatibility

- **Strictly additive.** No existing types, properties, methods, or behaviors changed. Source-compatible with `5.1.x`.
- **`DistributedMessageEnvelope.PublishedAt` is nullable.** Envelopes serialized before this release deserialize with `PublishedAt = null` — no exceptions, no consumer breakage. Handlers that consume it check `HasValue`.
- **No new dependencies.** All new types use existing `Cirreum.Conductor` (already in Core) and `System.*`.
- **No default-behavior changes.** Apps not consuming the new types see no behavioral difference.

---

## See also

- `CHANGELOG.md` — condensed change list for `5.2.0`.
- `Cirreum.Runtime.Messaging` (next release) — hosted receiver, default `INodeIdProvider`, publisher property enrichment.
