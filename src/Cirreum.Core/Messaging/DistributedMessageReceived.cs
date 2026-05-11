namespace Cirreum.Messaging;

using Cirreum.Conductor;

/// <summary>
/// Wraps an inbound distributed message for dispatch via Conductor's notification
/// pipeline. App handlers implement <see cref="INotificationHandler{TNotification}"/>
/// over this wrapper type to react to received messages.
/// </summary>
/// <typeparam name="TMessage">The concrete <see cref="DistributedMessage"/> type
/// deserialized from the envelope payload.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DistributedMessage"/> itself implements <see cref="INotification"/> and is
/// intercepted by the outbound <c>DistributedMessageHandler&lt;T&gt;</c> notification handler
/// to route publishes through the configured transport. Publishing a deserialized
/// <see cref="DistributedMessage"/> directly through Conductor would re-trigger that
/// outbound interceptor — re-publishing the received message back to the bus in an
/// infinite loop. <see cref="DistributedMessageReceived{TMessage}"/> is a distinct
/// notification shape that the outbound interceptor does not catch, so inbound dispatch
/// flows to app handlers without re-entering the send path.
/// </para>
/// <para>
/// The wrapper carries both the deserialized typed message and the original envelope
/// so handlers can inspect wire-level metadata (producer ID, message identifier, version,
/// publish timestamp) without re-deserializing or threading additional context state.
/// </para>
/// <para>
/// Typical handler shape:
/// <code>
/// public sealed class EvidenceInstanceChangeHandler
///     : INotificationHandler&lt;DistributedMessageReceived&lt;EvidenceInstanceChangedV1&gt;&gt;
/// {
///     public Task HandleAsync(
///         DistributedMessageReceived&lt;EvidenceInstanceChangedV1&gt; notification,
///         CancellationToken ct)
///     {
///         var message  = notification.Message;
///         var producer = notification.Envelope.ProducerId;
///         /* react to the change locally */
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </para>
/// <para>
/// The L5 <c>DistributedMessageReceiver</c> hosted service constructs the wrapper from
/// the deserialized envelope and publishes it via <c>IConductor</c>. Handler discovery,
/// DI scoping, and pipeline behaviors follow Conductor's existing notification conventions.
/// </para>
/// </remarks>
/// <param name="Message">The deserialized message payload.</param>
/// <param name="Envelope">The original wire-format envelope including identifier, version,
/// producer ID, and publish timestamp.</param>
public sealed record DistributedMessageReceived<TMessage>(
	TMessage Message,
	DistributedMessageEnvelope Envelope
) : INotification
	where TMessage : notnull, DistributedMessage;
