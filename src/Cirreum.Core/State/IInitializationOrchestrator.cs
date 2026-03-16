namespace Cirreum.State;

/// <summary>
/// Orchestrates application initialization, coordinating all registered
/// <see cref="IInitializable"/> services and reporting progress through
/// <see cref="IActivityState"/>.
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
///       executed in ascending <see cref="IInitializable.Order"/> order.
///     </description>
///   </item>
/// </list>
/// <para>
/// Initialization errors should be tracked by <see cref="IActivityState"/> and can be
/// interrogated there after <see cref="HasCompleted"/> is <see langword="true"/>.
/// </para>
/// </remarks>
public interface IInitializationOrchestrator {

	/// <summary>
	/// Gets a value indicating whether initialization has started.
	/// </summary>
	bool HasStarted { get; }

	/// <summary>
	/// Gets a value indicating whether all initialization work has completed.
	/// </summary>
	bool HasCompleted { get; }

	/// <summary>
	/// Triggers the initialization pipeline asynchronously.
	/// </summary>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> that can be used to cancel the initialization
	/// pipeline. Typically provided by the calling component's lifetime scope so that
	/// in-progress initialization is cancelled if the component is disposed.
	/// </param>
	/// <remarks>
	/// <para>
	/// This method is idempotent — calling it after initialization has already
	/// started has no effect.
	/// </para>
	/// <para>
	/// The caller is responsible for ensuring <see cref="IActivityState"/> reflects
	/// active work before calling <see cref="Start"/> so that splash screens and
	/// loading indicators are visible immediately. In <c>AppRouteView</c>, this is
	/// accomplished by calling <see cref="IActivityState.StartTask"/> in
	/// <c>OnInitialized</c> before the orchestrator is started.
	/// </para>
	/// <para>
	/// The pipeline runs asynchronously — <see cref="Start"/> returns immediately
	/// after launching the background work. Observe <see cref="HasCompleted"/> or
	/// subscribe to <see cref="IActivityState"/> changes to detect when
	/// initialization finishes.
	/// </para>
	/// </remarks>
	void Start(CancellationToken cancellationToken = default);

}