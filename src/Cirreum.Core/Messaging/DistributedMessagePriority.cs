namespace Cirreum.Messaging;

/// <summary>
/// Defines the priority levels for distributed messages.
/// </summary>
/// <remarks>
/// <para>
/// Message priority influences processing order when the system is under load
/// or experiencing back pressure. Higher priority messages are processed before
/// lower priority ones in these situations.
/// </para>
/// <para>
/// The default priority is <see cref="Standard"/>. Use higher priorities sparingly and only
/// for messages where processing delays would impact system health or user experience.
/// </para>
/// </remarks>
public enum DistributedMessagePriority {
	/// <summary>
	/// Standard priority for most application messages.
	/// </summary>
	/// <remarks>
	/// This is the default priority and should be used for routine business operations.
	/// </remarks>
	Standard = 0,

	/// <summary>
	/// Higher priority for messages with time-sensitive requirements.
	/// </summary>
	/// <remarks>
	/// Use for user-facing operations where delays would be noticeable or impact experience,
	/// such as real-time notifications or interactive workflows.
	/// </remarks>
	TimeSensitive = 1,

	/// <summary>
	/// Highest priority reserved for critical system infrastructure messages.
	/// </summary>
	/// <remarks>
	/// Reserved for health monitoring, circuit breaker notifications, alerts, and recovery messages.
	/// Should not be used for regular application messages.
	/// </remarks>
	System = 2
}