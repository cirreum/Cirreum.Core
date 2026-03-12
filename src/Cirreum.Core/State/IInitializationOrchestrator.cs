namespace Cirreum.State;

/// <summary>
/// Orchestrates application initialization, coordinating all registered
/// <see cref="IInitializable"/> services and reporting progress through
/// <see cref="IInitializationState"/>.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator is triggered by the application's route view when it
/// determines that authentication has settled (or is not required) and
/// initialization work needs to be performed.
/// </para>
/// <para>
/// Initialization runs in two phases:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       <strong>Phase 1 — Cirreum-controlled:</strong> Application user loading
///       and profile enrichment (if registered and the user is authenticated).
///       These run in a fixed order before any app-registered initializers.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Phase 2 — App-registered:</strong> All <see cref="IInitializable"/>
///       services that return <see langword="true"/> from <see cref="IInitializable.ShouldInitialize"/>,
///       executed in <see cref="IInitializable.Order"/> sequence.
///     </description>
///   </item>
/// </list>
/// <para>
/// The <see cref="Start"/> method synchronously sets <see cref="IInitializationState.IsInitializing"/>
/// to <see langword="true"/> before returning, ensuring that no rendering gap exists where
/// the application could briefly appear ready before initialization begins.
/// </para>
/// </remarks>
public interface IInitializationOrchestrator {

	/// <summary>
	/// Gets a value indicating whether initialization has been triggered.
	/// </summary>
	bool HasStarted { get; }

	/// <summary>
	/// Gets a value indicating whether all initialization work has completed.
	/// </summary>
	bool HasCompleted { get; }

	/// <summary>
	/// Triggers the initialization pipeline.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is idempotent — calling it after initialization has already
	/// started has no effect.
	/// </para>
	/// <para>
	/// The method synchronously marks initialization as started (setting
	/// <see cref="IInitializationState.IsInitializing"/> to <see langword="true"/>)
	/// and then begins the asynchronous initialization work. This ensures that
	/// callers can check <see cref="IInitializationState.IsInitializing"/> immediately
	/// after calling <see cref="Start"/> without a race condition.
	/// </para>
	/// </remarks>
	void Start();

}