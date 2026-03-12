namespace Cirreum.State;
/// <summary>
/// Tracks the state of application initialization during startup.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides observable state for displaying initialization progress
/// to users, typically on a splash screen. It tracks the current task, completion
/// progress, and any errors that occurred during initialization.
/// </para>
/// <para>
/// The <see cref="IInitializationOrchestrator"/> updates this state as it processes
/// application user loading, profile enrichment, and registered <see cref="IInitializable"/>
/// services (including data stores). Components that inherit from
/// <c>StateComponentBase&lt;IInitializationState&gt;</c> automatically receive updates
/// for rendering splash screens and loading indicators.
/// </para>
/// <para>
/// Progress can be calculated as <c>CompletedTasks / TotalTasks</c> to drive
/// a deterministic progress bar.
/// </para>
/// </remarks>
public interface IInitializationState : IScopedNotificationState {

	/// <summary>
	/// Gets a value indicating whether initialization is currently in progress.
	/// </summary>
	/// <remarks>
	/// This property is <see langword="true"/> after <see cref="SetTotalTasks"/> is called
	/// and remains <see langword="true"/> until all tasks have completed.
	/// </remarks>
	bool IsInitializing { get; }

	/// <summary>
	/// Gets the current status message to display to users.
	/// </summary>
	/// <remarks>
	/// This message is updated as each service begins initialization,
	/// typically in the format "Loading Events..." or similar.
	/// Cleared when all tasks complete.
	/// </remarks>
	string DisplayStatus { get; }

	/// <summary>
	/// Gets the total number of initialization tasks to process.
	/// </summary>
	/// <remarks>
	/// Set once by the orchestrator at the start of initialization.
	/// Includes all tasks regardless of whether they are later skipped.
	/// </remarks>
	int TotalTasks { get; }

	/// <summary>
	/// Gets the number of initialization tasks that have completed.
	/// </summary>
	/// <remarks>
	/// Includes both successfully completed and skipped tasks.
	/// Progress can be calculated as <c>CompletedTasks / TotalTasks</c>.
	/// </remarks>
	int CompletedTasks { get; }

	/// <summary>
	/// Sets the total number of initialization tasks and begins tracking progress.
	/// </summary>
	/// <param name="total">The total number of tasks that will be processed.</param>
	void SetTotalTasks(int total);

	/// <summary>
	/// Updates the current display status message.
	/// </summary>
	/// <param name="status">The status message to display.</param>
	/// <remarks>
	/// Called by the orchestrator when starting each task, but can also be called
	/// by an <see cref="IInitializable"/> to update progress within its own work.
	/// </remarks>
	void SetDisplayStatus(string status);

	/// <summary>
	/// Signals the completion of an initialization task and advances progress.
	/// </summary>
	/// <remarks>
	/// When all tasks complete (<see cref="CompletedTasks"/> equals <see cref="TotalTasks"/>),
	/// the display status is cleared.
	/// </remarks>
	void CompleteTask();

	/// <summary>
	/// Gets the collection of errors that occurred during initialization.
	/// </summary>
	IReadOnlyList<InitializationError> Errors { get; }

	/// <summary>
	/// Gets a value indicating whether any errors occurred during initialization.
	/// </summary>
	bool HasErrors { get; }

	/// <summary>
	/// Gets the count of errors that occurred during initialization.
	/// </summary>
	int ErrorCount { get; }

	/// <summary>
	/// Logs an error that occurred during store initialization.
	/// </summary>
	/// <param name="storeName">The display name of the service that failed.</param>
	/// <param name="exception">The exception that occurred.</param>
	void LogError(string storeName, Exception exception);

	/// <summary>
	/// Clears all recorded initialization errors.
	/// </summary>
	void ClearErrors();

}