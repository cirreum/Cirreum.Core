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
/// The <see cref="Start"/> method should synchronously begin tracked activity
/// before returning so that <see cref="IActivityState.IsActive"/> becomes
/// <see langword="true"/> immediately. This prevents a rendering gap where the
/// application could briefly appear ready before initialization begins.
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
	/// Triggers the initialization pipeline.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is idempotent — calling it after initialization has already
	/// started has no effect.
	/// </para>
	/// <para>
	/// The <see cref="Start"/> method should synchronously begin tracked activity
	/// before returning so that <see cref="IActivityState.IsActive"/> becomes
	/// <see langword="true"/> immediately. This prevents a rendering gap where the
	/// application could briefly appear ready before initialization work begins.
	/// Implementations typically accomplish this by starting an initial
	/// indeterminate task prior to launching the asynchronous initialization pipeline.
	/// </para>
	/// </remarks>
	void Start();

}