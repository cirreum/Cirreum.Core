namespace Cirreum.Messaging.Options;

/// <summary>
/// Defines a profile for time-based batch interval scaling to optimize message processing
/// based on expected traffic patterns during different time periods.
/// </summary>
/// <remarks>
/// <para>
/// A time batching profile contains rules that adjust the <see cref="BackgroundDeliveryOptions.BatchFillWaitTime"/>
/// based on the day of week and time of day, allowing for optimized message processing during different
/// operational periods.
/// </para>
/// <para>
/// Each profile can have multiple rules covering different time periods. When multiple profiles
/// are defined, the active profile is determined by the <see cref="BackgroundDeliveryOptions.ActiveTimeBatchingProfile"/>
/// setting.
/// </para>
/// </remarks>
public class TimeBatchingProfile {

	/// <summary>
	/// Gets or sets the display name of this profile.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The name should be descriptive of the profile's purpose, such as "EventWeekend" or "OnlineBusiness".
	/// </para>
	/// <para>
	/// This name is primarily used for logging and diagnostic purposes.
	/// </para>
	/// </remarks>
	public string Name { get; set; } = "";

	/// <summary>
	/// Gets or sets the default scaling factor to apply when no rules match the current time.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This scaling factor is applied to the <see cref="BackgroundDeliveryOptions.BatchFillWaitTime"/>
	/// during periods not covered by any specific rule.
	/// </para>
	/// <para>
	/// A value of 1.0 means no adjustment to the base wait time.
	/// Values greater than 1.0 increase the wait time (appropriate for low-traffic periods).
	/// Values less than 1.0 decrease the wait time (appropriate for high-traffic periods).
	/// </para>
	/// <para>
	/// Default: 1.0 (no adjustment)
	/// </para>
	/// </remarks>
	public double DefaultScalingFactor { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets the collection of time-based scaling rules that define when specific
	/// scaling factors should be applied.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Rules are evaluated in order. The first rule that matches the current day and time
	/// will be applied. If no rules match, the <see cref="DefaultScalingFactor"/> is used.
	/// </para>
	/// <para>
	/// Each rule specifies days of the week, a time range, and a scaling factor to apply
	/// during that period.
	/// </para>
	/// </remarks>
	public List<TimeScalingRule> Rules { get; set; } = [];
}