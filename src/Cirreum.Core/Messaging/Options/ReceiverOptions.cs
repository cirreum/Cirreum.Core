namespace Cirreum.Messaging;

/// <summary>
/// Configuration options for the distributed message receiver, defining the messaging
/// provider instance and the source(s) — queue and/or topic subscription — to consume from.
/// </summary>
/// <remarks>
/// <para>
/// The presence of this section in configuration (with valid <see cref="InstanceKey"/>
/// and at least one configured source) triggers receiver registration in the L5
/// <c>Cirreum.Runtime.Messaging</c> hosting extension. Absent or incomplete configuration
/// leaves the receiver unregistered — the default state, equivalent to "this process is
/// send-only or has no inbound concerns."
/// </para>
/// <para>
/// Mirrors the send-side symmetry: just as <see cref="SenderOptions.QueueName"/> and
/// <see cref="SenderOptions.TopicName"/> let a publisher route messages to either
/// destination based on each message's <see cref="MessageDefinitionAttribute.Target"/>,
/// <see cref="ReceiverOptions"/> lets a process consume from either source — or both.
/// </para>
/// <para>
/// <b>Queue vs Topic semantics:</b>
/// <list type="bullet">
///   <item><description><see cref="QueueName"/> — competing consumers; exactly one consumer processes each message. Used for work distribution (e.g., emails to send, payments to process). Multiple replicas of a head pull from the same queue and share the load.</description></item>
///   <item><description><see cref="TopicName"/> + <see cref="SubscriptionName"/> — broadcast; each subscription receives a copy of every published message. Used for cross-head event reactions (e.g., registry sync, kill switches). Each head/deployment configures a unique <see cref="SubscriptionName"/> so every head receives its own copy.</description></item>
/// </list>
/// </para>
/// <para>
/// At least one source must be configured. Both may be configured simultaneously — the
/// receiver consumes from whichever sources are present.
/// </para>
/// <para>
/// Example appsettings.json — topic only (broadcast-only head, e.g., the API head):
/// <code>
/// {
///   "Cirreum": {
///     "Messaging": {
///       "Distribution": {
///         "Receiver": {
///           "InstanceKey": "app-primary",
///           "TopicName": "app.notifications.v1",
///           "SubscriptionName": "api-head"
///         }
///       }
///     }
///   }
/// }
/// </code>
/// </para>
/// <para>
/// Example appsettings.json — queue + topic (worker head, e.g., email or IVA):
/// <code>
/// {
///   "Cirreum": {
///     "Messaging": {
///       "Distribution": {
///         "Receiver": {
///           "InstanceKey": "app-primary",
///           "QueueName": "app.emails.v1",
///           "TopicName": "app.notifications.v1",
///           "SubscriptionName": "email-head",
///           "MaxConcurrency": 4
///         }
///       }
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class ReceiverOptions {

	/// <summary>
	/// The key name of the section in configuration.
	/// </summary>
	public const string ConfigurationName = "Receiver";

	/// <summary>
	/// The configuration sub-key for <see cref="InstanceKey"/>, used by the
	/// L5 hosting extension to detect whether the receiver is configured.
	/// </summary>
	public const string ReceiverInstanceConfigurationName = "InstanceKey";

	/// <summary>
	/// The configuration sub-key for <see cref="QueueName"/>, used by the
	/// L5 hosting extension to detect a queue source.
	/// </summary>
	public const string QueueConfigurationName = "QueueName";

	/// <summary>
	/// The configuration sub-key for <see cref="TopicName"/>, used by the
	/// L5 hosting extension to detect a topic source.
	/// </summary>
	public const string TopicConfigurationName = "TopicName";

	/// <summary>
	/// The configuration sub-key for <see cref="SubscriptionName"/>, used by the
	/// L5 hosting extension when registering a topic source.
	/// </summary>
	public const string SubscriptionConfigurationName = "SubscriptionName";

	/// <summary>
	/// Gets the configured messaging provider instance key (resolves to a keyed
	/// <c>IMessagingClient</c> registration). Typically matches the corresponding
	/// <see cref="SenderOptions.InstanceKey"/> when the same process both publishes
	/// and receives on the same bus.
	/// </summary>
	public required string InstanceKey { get; init; }

	/// <summary>
	/// Gets the name of the queue this receiver consumes from, when configured.
	/// Queue consumption follows competing-consumer semantics — exactly one
	/// consumer (across all replicas pulling from the queue) processes each
	/// message. Use for work distribution. Optional; leave <see langword="null"/>
	/// if this receiver only consumes from a topic subscription.
	/// </summary>
	public string? QueueName { get; init; }

	/// <summary>
	/// Gets the name of the topic this receiver subscribes to, when configured.
	/// Pair with <see cref="SubscriptionName"/>. Should match the
	/// <see cref="SenderOptions.TopicName"/> used by upstream publishers on the
	/// same bus. Optional; leave <see langword="null"/> if this receiver only
	/// consumes from a queue.
	/// </summary>
	public string? TopicName { get; init; }

	/// <summary>
	/// Gets the subscription name on <see cref="TopicName"/>. <b>Unique per deployed
	/// head</b> — every head/deployment configures a different value so that each head
	/// has its own subscription and receives its own copy of every published message.
	/// Required when <see cref="TopicName"/> is set; otherwise ignored.
	/// </summary>
	public string? SubscriptionName { get; init; }

	/// <summary>
	/// Gets the maximum number of concurrent in-flight handler invocations per source.
	/// Default <c>1</c> preserves FIFO ordering within a subscription or queue, which
	/// is the correct default for registry-sync and convergence workloads where
	/// <c>register</c> followed by <c>unregister</c> must not be reordered.
	/// Raise explicitly for higher-throughput, order-independent workloads
	/// (e.g., email send queues).
	/// </summary>
	public int MaxConcurrency { get; init; } = 1;

	/// <summary>
	/// Gets the number of messages to prefetch from the broker into the receiver's
	/// local buffer. Higher values trade memory for throughput.
	/// </summary>
	public int PrefetchCount { get; init; } = 10;

	/// <summary>
	/// Gets how long the receiver auto-renews a message's broker-side lock while a
	/// handler is executing. Should comfortably exceed the longest expected handler
	/// duration.
	/// </summary>
	public TimeSpan MaxAutoLockRenewalDuration { get; init; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Gets the time the receiver waits for in-flight handlers to complete on
	/// graceful shutdown before cancelling them.
	/// </summary>
	public TimeSpan GracefulShutdownTimeout { get; init; } = TimeSpan.FromSeconds(30);

}
