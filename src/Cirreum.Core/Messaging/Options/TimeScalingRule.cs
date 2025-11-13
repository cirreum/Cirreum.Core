namespace Cirreum.Messaging.Options;

/// <summary>
/// Defines a rule for adjusting batch processing behavior during specific time periods.
/// </summary>
/// <remarks>
/// <para>
/// Time scaling rules allow for fine-tuned adjustment of the <see cref="BackgroundDeliveryOptions.BatchFillWaitTime"/> 
/// based on day of week and time of day, enabling optimization for expected message patterns during
/// different operational periods.
/// </para>
/// <para>
/// Rules are evaluated in order within a <see cref="TimeBatchingProfile"/>. The first rule that matches
/// the current day and time will be applied.
/// </para>
/// </remarks>
public class TimeScalingRule {

	/// <summary>
	/// Gets or sets the days of the week when this rule applies.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use <see cref="DayOfWeek"/> enumeration values to specify which days the rule applies to.
	/// For example, to apply a rule to weekends, include both <see cref="DayOfWeek.Saturday"/> and
	/// <see cref="DayOfWeek.Sunday"/> in the list.
	/// </para>
	/// <para>
	/// An empty list indicates the rule does not apply to any day.
	/// </para>
	/// </remarks>
	public List<DayOfWeek> Days { get; set; } = [];

	/// <summary>
	/// Gets or sets the starting hour of the day when this rule begins to apply (0-23).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Uses 24-hour format where 0 represents midnight (12:00 AM) and 23 represents 11:00 PM.
	/// </para>
	/// <para>
	/// The rule applies to times that are greater than or equal to <see cref="StartHour"/> and
	/// less than <see cref="EndHour"/>. Valid values are 0 through 23.
	/// </para>
	/// <para>
	/// For rules that span midnight, set <see cref="StartHour"/> greater than <see cref="EndHour"/>.
	/// For example, to define a rule from 10:00 PM to 6:00 AM, set StartHour = 22 and EndHour = 6.
	/// </para>
	/// </remarks>
	public int StartHour { get; set; }

	/// <summary>
	/// Gets or sets the ending hour of the day when this rule ceases to apply (0-24).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Uses 24-hour format where 0 represents midnight (12:00 AM) and 23 represents 11:00 PM.
	/// </para>
	/// <para>
	/// The rule applies to times that are greater than or equal to <see cref="StartHour"/> and
	/// less than <see cref="EndHour"/>. For a full 24-hour rule, use EndHour = 24.
	/// </para>
	/// <para>
	/// For rules that span midnight, set <see cref="StartHour"/> greater than <see cref="EndHour"/>.
	/// For example, to define a rule from 10:00 PM to 6:00 AM, set StartHour = 22 and EndHour = 6.
	/// </para>
	/// </remarks>
	public int EndHour { get; set; }

	/// <summary>
	/// Gets or sets the scaling factor to apply to the <see cref="BackgroundDeliveryOptions.BatchFillWaitTime"/>
	/// when this rule matches the current time.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A value of 1.0 means no adjustment to the base wait time.
	/// Values greater than 1.0 increase the wait time (appropriate for low-traffic periods).
	/// Values less than 1.0 decrease the wait time (appropriate for high-traffic periods).
	/// </para>
	/// <para>
	/// For example:
	/// - 2.0 means wait twice as long as the base wait time
	/// - 0.5 means wait half as long as the base wait time
	/// </para>
	/// </remarks>
	public double ScalingFactor { get; set; }

	/// <summary>
	/// Gets or sets a descriptive explanation of the rule's purpose.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This description is used for logging and diagnostic purposes to help understand
	/// why a particular scaling factor was applied.
	/// </para>
	/// <para>
	/// Examples: "Weekend scaling", "Evening hours (6 PM - 10 PM)", "Lunch hour rush"
	/// </para>
	/// </remarks>
	public string Description { get; set; } = "";
}