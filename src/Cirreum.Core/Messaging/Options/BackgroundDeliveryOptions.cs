namespace Cirreum.Messaging.Options;

/// <summary>
/// Configuration options for message delivery in the distributed messaging system.
/// These settings control how messages are processed and delivered to the messaging infrastructure.
/// </summary>
/// <remarks>
/// These options allow fine-tuning of the message delivery behavior, including
/// performance characteristics and delivery guarantees.
/// </remarks>
public class BackgroundDeliveryOptions {

	/// <summary>
	/// The key name of the section.
	/// </summary>
	public const string ConfigurationName = "BackgroundDelivery";

	/// <summary>
	/// Gets or sets whether background delivery is used by default when the message-specific setting is not specified.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This property serves as the fallback delivery mode when <see cref="DistributedMessage.UseBackgroundDelivery"/> is null.
	/// </para>
	/// <para>
	/// When set to <see langword="true"/>: Messages are queued and processed asynchronously in the background,
	/// which can improve throughput at the cost of immediate delivery guarantees.
	/// </para>
	/// <para>
	/// When set to <see langword="false"/> (default): Messages are processed synchronously,
	/// providing stronger consistency guarantees but potentially lower throughput.
	/// </para>
	/// <para>
	/// The message-specific setting <see cref="DistributedMessage.UseBackgroundDelivery"/>, when specified (not null),
	/// always takes precedence over this default setting. Therefore, background delivery configuration
	/// (QueueCapacity, BatchCapacity, etc.) must always be provided, as individual messages may opt into
	/// background delivery even when this default is set to <see langword="false"/>.
	/// </para>
	/// </remarks>
	public bool UseBackgroundDeliveryByDefault { get; set; }

	/// <summary>
	/// Gets or sets the maximum rate of high-priority messages allowed per minute.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This rate limit applies to TimeSensitive and SystemHealth priority messages combined.
	/// It prevents priority inflation by enforcing a budget for high-priority messages.
	/// </para>
	/// <para>
	/// Messages that exceed this rate will be processed at Standard priority.
	/// </para>
	/// <para>
	/// Default: 100 per minute
	/// </para>
	/// </remarks>
	public int PriorityMessageRateLimit { get; set; } = 100;

	/// <summary>
	/// Gets or sets the maximum age in seconds before a message's effective priority is increased.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This prevents starvation of lower-priority messages during sustained high-load periods.
	/// After waiting in the queue for this duration, a message's effective priority will be
	/// increased by one level (e.g., Standard to TimeSensitive).
	/// </para>
	/// <para>
	/// Set to 0 to disable age-based priority promotion.
	/// </para>
	/// <para>
	/// Default: 60 seconds
	/// </para>
	/// </remarks>
	public int PriorityAgePromotionThreshold { get; set; } = 60;

	/// <summary>
	/// Gets or sets the number of consecutive failures before the circuit breaker opens.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When this number of consecutive failures is reached, the circuit breaker will
	/// temporarily stop sending messages to prevent cascading failures.
	/// </para>
	/// <para>
	/// Default: 5
	/// </para>
	/// </remarks>
	public int CircuitBreakerThreshold { get; set; } = 5;

	/// <summary>
	/// Gets or sets the time period that the circuit breaker remains open before
	/// allowing another attempt.
	/// </summary>
	/// <remarks>
	/// <para>
	/// After the circuit opens due to consecutive failures, this is how long
	/// the system will wait before trying to send messages again.
	/// </para>
	/// <para>
	/// Default: 1 minute
	/// </para>
	/// </remarks>
	public TimeSpan CircuitResetTimeout { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Gets or sets the capacity of the background queue used for message processing.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only applicable when background delivery is used for a message, either via the default setting
	/// or when explicitly enabled via <see cref="DistributedMessage.UseBackgroundDelivery"/>.
	/// </para>
	/// <para>
	/// Specifies the maximum number of messages that can be queued for processing.
	/// When the queue is full, attempts to add more messages will wait until space becomes available.
	/// </para>
	/// <para>
	/// Higher values allow for better handling of traffic spikes but require more memory.
	/// </para>
	/// <para>
	/// Default: 1,000
	/// </para>
	/// </remarks>
	public int QueueCapacity { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the maximum number of messages to process in a single batch.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only applicable when background delivery is used for a message, either via the default setting
	/// or when explicitly enabled via <see cref="DistributedMessage.UseBackgroundDelivery"/>.
	/// </para>
	/// <para>
	/// Batching improves throughput by reducing the number of interactions with the messaging
	/// infrastructure. Higher values increase throughput but may increase latency for individual
	/// messages.
	/// </para>
	/// <para>
	/// Default: 10
	/// </para>
	/// </remarks>
	public int BatchCapacity { get; set; } = 10;

	/// <summary>
	/// Gets or sets the maximum time to wait for a batch to fill up before processing it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only applicable when background delivery is used for a message, either via the default setting
	/// or when explicitly enabled via <see cref="DistributedMessage.UseBackgroundDelivery"/>.
	/// </para>
	/// <para>
	/// Specifies how long the system will wait to collect messages into a batch before
	/// sending an incomplete batch. Lower values reduce latency at the cost of potentially
	/// reduced throughput.
	/// </para>
	/// <para>
	/// Default: 50 milliseconds
	/// </para>
	/// </remarks>
	public TimeSpan BatchFillWaitTime { get; set; } = TimeSpan.FromMilliseconds(50);

	/// <summary>
	/// Gets or sets the active time-based batching profile name to be used for dynamic batch adjustment.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Specifies which of the configured time batching profiles should be active for adjusting 
	/// the <see cref="BatchFillWaitTime"/> based on time of day and current load.
	/// </para>
	/// <para>
	/// The selected profile contains rules that adjust batch timing based on day of week and 
	/// hour of day, allowing for optimization during different operational periods.
	/// </para>
	/// <para>
	/// Default: "Default"
	/// </para>
	/// </remarks>
	public string ActiveTimeBatchingProfile { get; set; } = "Default";

	/// <summary>
	/// Gets or sets the collection of time-based batching profiles that define scaling factors
	/// for different operational periods.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only applicable when background delivery is used for a message, either via the default setting
	/// or when explicitly enabled via <see cref="DistributedMessage.UseBackgroundDelivery"/>.
	/// </para>
	/// <para>
	/// Each profile contains rules that specify scaling factors for the <see cref="BatchFillWaitTime"/>
	/// based on day of week and time of day. This allows for dynamic adjustment of batching behavior 
	/// to optimize for expected message patterns during different operational periods.
	/// </para>
	/// <para>
	/// Higher scaling factors increase wait times (appropriate for low-traffic periods),
	/// while lower scaling factors decrease wait times (appropriate for high-traffic periods).
	/// </para>
	/// <para>
	/// The active profile is determined by the <see cref="ActiveTimeBatchingProfile"/> setting.
	/// </para>
	/// </remarks>
	public Dictionary<string, TimeBatchingProfile> TimeBatchingProfiles { get; set; } = [];

}