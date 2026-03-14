namespace Cirreum.State;

/// <summary>
/// Defines how application activity should be interpreted by consumers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ActivityMode"/> determines whether the current activity cycle
/// can report measurable progress or should be treated as indeterminate.
/// </para>
/// <para>
/// In <see cref="Indeterminate"/> mode, work is occurring but the total amount
/// of work is unknown or not meaningful to represent as a percentage. Consumers
/// should render a non-quantified indicator such as a spinner or loading overlay.
/// </para>
/// <para>
/// In <see cref="Deterministic"/> mode, the total amount of work is known and
/// progress can be calculated from <see cref="IActivityState.CompletedTasks"/>
/// and <see cref="IActivityState.TotalTasks"/>. Consumers may render a progress
/// bar or other quantified indicator.
/// </para>
/// <para>
/// Activity mode is typically controlled by higher-level coordinators such as
/// <see cref="IInitializationOrchestrator"/>, which may know the total number of
/// initialization steps to execute. Most producers that simply participate in
/// activity tracking do not need to set this mode explicitly.
/// </para>
/// </remarks>
/// <seealso cref="IActivityState"/>
/// <seealso cref="IInitializationOrchestrator"/>
public enum ActivityMode {

	/// <summary>
	/// Indicates that activity is occurring but the total amount of work is not known.
	/// </summary>
	/// <remarks>
	/// Consumers should treat the activity as indeterminate and render a spinner,
	/// loading overlay, or other non-quantified indicator. In this mode,
	/// <see cref="IActivityState.ProgressPercent"/> will return <see langword="null"/>.
	/// </remarks>
	Indeterminate = 0,

	/// <summary>
	/// Indicates that the total amount of work is known and progress can be measured.
	/// </summary>
	/// <remarks>
	/// Consumers may calculate progress using <see cref="IActivityState.CompletedTasks"/>
	/// and <see cref="IActivityState.TotalTasks"/>, or read
	/// <see cref="IActivityState.ProgressPercent"/> directly to display a progress bar
	/// or similar quantified indicator.
	/// </remarks>
	Deterministic = 1
}