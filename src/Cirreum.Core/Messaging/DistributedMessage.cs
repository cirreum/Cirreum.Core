namespace Cirreum.Messaging;

using Cirreum.Conductor;
using System.Text.Json.Serialization;

/// <summary>
/// Base implementation of a distributable message that can be sent to external systems.
/// </summary>
/// <remarks>
/// <para>
/// This abstract record defines messages intended to be sent outside the application boundary
/// to other systems or components through messaging infrastructure such as message queues, 
/// service buses, and pub/sub topics.
/// </para>
/// <para>
/// The distribution properties provide control over:
/// <list type="bullet">
///   <item><description>Delivery mode: background (asynchronous) or immediate (synchronous)</description></item>
///   <item><description>Message priority for queue processing</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important:</b> All implementations must be decorated with the 
/// <see cref="MessageDefinitionAttribute"/> to define the message's identifier and version.
/// Once a message type is deployed to production, its structure should not be modified.
/// To maintain backward compatibility when changes are needed, create a new message type with
/// an incremented version number while keeping the same identifier.
/// </para>
/// </remarks>
public abstract record DistributedMessage : INotification {

	/// <summary>
	/// Gets or sets a value indicating whether this message should use background delivery
	/// or immediate delivery when being published to an external system.
	/// </summary>
	/// <value>
	/// Defaults to <see langword="null"/>.
	/// </value>
	/// <remarks>
	/// <para>
	/// When <see langword="true"/>, the message is queued for background processing with batching support,
	/// enabling parallel processing and optimizing throughput. The <c>PublishAsync</c> call returns
	/// immediately after queueing, without waiting for delivery.
	/// </para>
	/// <para>
	/// When <see langword="false"/>, the message is processed synchronously in sequential order,
	/// providing immediate delivery with stronger consistency guarantees. The <c>PublishAsync</c> call
	/// does not return until the <see cref="IDistributedTransportPublisher"/> has delivered the message
	/// to the external messaging system.
	/// </para>
	/// <para>
	/// When <see langword="null"/>, the behavior is determined by application configuration.
	/// </para>
	/// </remarks>
	[JsonIgnore]
	public virtual bool? UseBackgroundDelivery { get; set; } = null;

	/// <summary>
	/// Gets or sets the priority level for this message when using background delivery.
	/// </summary>
	/// <value>
	/// A <see cref="DistributedMessagePriority"/> value. 
	/// Defaults to <see cref="DistributedMessagePriority.Standard"/>.
	/// </value>
	/// <remarks>
	/// <para>
	/// This property only affects messages that use background delivery
	/// (when <see cref="UseBackgroundDelivery"/> is <see langword="true"/> or defaults to <see langword="true"/>).
	/// Messages with <see cref="UseBackgroundDelivery"/> set to <see langword="false"/> are processed
	/// immediately and bypass the prioritization system.
	/// </para>
	/// <para>
	/// Priority determines processing order during periods of high load or back pressure.
	/// Higher priority messages are processed before lower priority ones when the system is under load.
	/// </para>
	/// <para>
	/// Reserve <see cref="DistributedMessagePriority.System"/> for critical infrastructure messages
	/// such as health monitoring, circuit breaker notifications, and similar system-level concerns.
	/// </para>
	/// </remarks>
	[JsonIgnore]
	public virtual DistributedMessagePriority Priority { get; set; } = DistributedMessagePriority.Standard;
}