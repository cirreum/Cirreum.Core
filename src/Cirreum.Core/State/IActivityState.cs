namespace Cirreum.State;

/// <summary>
/// Represents application-wide activity and progress state used to coordinate
/// global UI indicators such as splash screens, loading overlays, and progress bars.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IActivityState"/> provides a centralized way for components and services
/// to signal that work is occurring. Consumers can observe this state to render global
/// activity indicators such as splash screens, spinners, or progress bars.
/// </para>
/// <para>
/// Activity may be either <see cref="ActivityMode.Indeterminate"/> (work is occurring but
/// the total amount is unknown) or <see cref="ActivityMode.Deterministic"/> (the total
/// amount of work is known and progress can be measured).
/// </para>
/// <para>
/// Most producers should simply call <see cref="StartTask"/> and <see cref="CompleteTask"/>
/// to participate in activity tracking. Higher-level workflows—such as
/// <see cref="IInitializationOrchestrator"/>—may call <see cref="BeginTasks"/> and
/// <see cref="SetMode"/> to provide deterministic progress reporting.
/// </para>
/// <para>
/// When deterministic progress is available, consumers can calculate progress using
/// <see cref="CompletedTasks"/> and <see cref="TotalTasks"/>, or read
/// <see cref="ProgressPercent"/> directly. When activity is indeterminate,
/// <see cref="ProgressPercent"/> will return <see langword="null"/> and consumers
/// should render a non-quantified indicator such as a spinner.
/// </para>
/// </remarks>
/// <seealso cref="ActivityMode"/>
/// <seealso cref="IInitializationOrchestrator"/>
public interface IActivityState : IScopedNotificationState {

	/// <summary>
	/// Gets a value indicating whether any activity is currently in progress.
	/// </summary>
	/// <remarks>
	/// This property becomes <see langword="true"/> when <see cref="StartTask"/> or
	/// <see cref="BeginTasks"/> is called and remains <see langword="true"/> until
	/// all tasks have completed or <see cref="ResetTasks"/> is invoked.
	/// </remarks>
	bool IsActive { get; }

	/// <summary>
	/// Gets the current activity mode.
	/// </summary>
	/// <remarks>
	/// <see cref="ActivityMode.Indeterminate"/> indicates that work is occurring
	/// but progress cannot be measured. <see cref="ActivityMode.Deterministic"/>
	/// indicates that progress can be calculated from task counts.
	/// </remarks>
	ActivityMode Mode { get; }

	/// <summary>
	/// Gets a value indicating whether deterministic progress reporting is active.
	/// </summary>
	bool IsDeterministic { get; }

	/// <summary>
	/// Gets the current status message describing the activity being performed.
	/// </summary>
	/// <remarks>
	/// The status message may be set by producers via <see cref="StartTask"/>,
	/// <see cref="BeginTasks"/>, or <see cref="SetDisplayStatus"/>.
	/// It is typically displayed in splash screens or loading overlays.
	/// </remarks>
	string DisplayStatus { get; }

	/// <summary>
	/// Gets the total number of tasks being tracked for the current activity cycle.
	/// </summary>
	/// <remarks>
	/// This value increases when <see cref="StartTask"/> or <see cref="BeginTasks"/>
	/// is called and resets to zero when all tasks complete or <see cref="ResetTasks"/>
	/// is invoked.
	/// </remarks>
	int TotalTasks { get; }

	/// <summary>
	/// Gets the number of tasks that have completed.
	/// </summary>
	/// <remarks>
	/// Completed tasks may represent successful operations or intentionally skipped
	/// work. When deterministic progress is active, this value contributes to the
	/// calculation of <see cref="ProgressPercent"/>.
	/// </remarks>
	int CompletedTasks { get; }

	/// <summary>
	/// Gets the current progress percentage when deterministic activity tracking
	/// is enabled; otherwise <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// When <see cref="Mode"/> is <see cref="ActivityMode.Deterministic"/>, this value
	/// represents the ratio of <see cref="CompletedTasks"/> to <see cref="TotalTasks"/>
	/// as a number between 0.0 and 1.0. When activity is indeterminate, consumers
	/// should treat this property as unavailable and render a spinner or other
	/// non-quantified indicator instead.
	/// </remarks>
	double? ProgressPercent { get; }

	/// <summary>
	/// Signals the start of a single task and optionally updates the display status.
	/// </summary>
	/// <remarks>
	/// This method is typically used by producers that want to participate in
	/// activity tracking but do not know the total amount of work involved.
	/// Calling this method does not imply deterministic progress.
	/// </remarks>
	/// <param name="status">
	/// Optional status text describing the work being performed.
	/// </param>
	void StartTask(string? status = null);

	/// <summary>
	/// Signals the start of multiple tasks and optionally updates the display status.
	/// </summary>
	/// <remarks>
	/// This method is commonly used by orchestrators or coordinated workflows that
	/// know the total amount of work to be tracked. Deterministic progress reporting
	/// can be enabled by calling <see cref="SetMode"/> with
	/// <see cref="ActivityMode.Deterministic"/>.
	/// </remarks>
	/// <param name="count">The number of tasks to add to the activity tracker.</param>
	/// <param name="status">Optional status text describing the work being performed.</param>
	void BeginTasks(int count, string? status = null);

	/// <summary>
	/// Signals the completion of a single task and advances activity progress.
	/// </summary>
	/// <remarks>
	/// When <see cref="CompletedTasks"/> reaches <see cref="TotalTasks"/>, the activity
	/// cycle is considered complete and implementations should reset task counters
	/// and return to the default <see cref="ActivityMode.Indeterminate"/> mode.
	/// </remarks>
	void CompleteTask();

	/// <summary>
	/// Updates the current status message describing the active work.
	/// </summary>
	/// <param name="status">The status message to display.</param>
	void SetDisplayStatus(string status);

	/// <summary>
	/// Sets the activity mode used to interpret the current work.
	/// </summary>
	/// <remarks>
	/// This method is typically used by orchestrators such as
	/// <see cref="IInitializationOrchestrator"/> when deterministic progress
	/// reporting is possible.
	/// </remarks>
	/// <param name="mode">The activity mode to apply.</param>
	void SetMode(ActivityMode mode);

	/// <summary>
	/// Resets task tracking counters, display status, and activity mode.
	/// </summary>
	/// <remarks>
	/// This method clears the current activity cycle but does not clear
	/// <see cref="Errors"/>.
	/// </remarks>
	void ResetTasks();

	/// <summary>
	/// Gets the collection of activity errors recorded during the current or previous
	/// activity cycles.
	/// </summary>
	IReadOnlyList<ActivityError> Errors { get; }

	/// <summary>
	/// Records an activity error.
	/// </summary>
	/// <param name="error">
	/// The activity error to record.
	/// </param>
	void LogError(ActivityError error);

	/// <summary>
	/// Records an exception that occurred during activity execution.
	/// </summary>
	/// <remarks>
	/// This is a convenience overload that creates an <see cref="ActivityError"/>
	/// using the provided values.
	/// </remarks>
	/// <param name="sourceName">
	/// The display name of the component, service, or store that encountered the error.
	/// </param>
	/// <param name="exception">
	/// The exception that occurred.
	/// </param>
	/// <param name="displayMessage">
	/// An optional user-friendly message suitable for display in the UI.
	/// </param>
	/// <param name="severity">
	/// The severity of the recorded activity error.
	/// </param>
	void LogError(
		string sourceName,
		Exception exception,
		string? displayMessage = null,
		ActivityErrorSeverity severity = ActivityErrorSeverity.Error);

	/// <summary>
	/// Clears all recorded activity errors.
	/// </summary>
	void ClearErrors();

}