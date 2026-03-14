namespace Cirreum.State;

using Cirreum.Security;

/// <summary>
/// Defines a service that participates in application initialization during startup.
/// </summary>
/// <remarks>
/// <para>
/// Services implementing this interface are discovered and executed by the
/// <see cref="IInitializationOrchestrator"/> during application startup.
/// </para>
/// <para>
/// Initializables contribute to the overall startup workflow while reporting
/// user-facing status through <see cref="IActivityState"/>. This allows the
/// application to present splash screens, loading overlays, or progress indicators
/// while initialization work is being performed.
/// </para>
/// <para>
/// The <see cref="DisplayName"/> and <see cref="InitializationMessage"/> properties
/// provide user-friendly metadata for status reporting, while <see cref="Order"/>
/// controls execution order. <see cref="ShouldInitialize"/> allows an
/// implementation to opt out of the current initialization cycle based on
/// runtime conditions such as authentication state or user context.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SettingsInitializer(
///     ISettingsApi api,
///     IStateManager stateManager
/// ) : IInitializable {
///
///     public string DisplayName => "Settings";
///     public string InitializationMessage => "Loading settings...";
///     public int Order => 100;
///
///     public bool ShouldInitialize(IUserState userState) => true;
///
///     public async Task InitializeAsync(Action&lt;string&gt; updateStatus, CancellationToken cancellationToken) {
///         await api.LoadSettingsAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IActivityState"/>
/// <seealso cref="IInitializationOrchestrator"/>
public interface IInitializable {

	/// <summary>
	/// Gets the display name of this initializer.
	/// </summary>
	/// <remarks>
	/// This value is intended for user-facing and diagnostic scenarios, such as
	/// activity reporting and error logging.
	/// </remarks>
	string DisplayName { get; }

	/// <summary>
	/// Gets the default message that describes the initialization work being performed.
	/// </summary>
	/// <remarks>
	/// The orchestrator may use this value as the initial activity status before
	/// invoking <see cref="InitializeAsync"/>.
	/// </remarks>
	/// <example>"Loading Events..."</example>
	string InitializationMessage { get; }

	/// <summary>
	/// Gets the order in which this initializer executes relative to others.
	/// </summary>
	/// <remarks>
	/// Lower values execute first. The default convention is 1000.
	/// </remarks>
	int Order { get; }

	/// <summary>
	/// Determines whether this initializer should participate in the current
	/// initialization cycle.
	/// </summary>
	/// <param name="userState">
	/// The current user state, allowing the initializer to make decisions based on
	/// authentication or other user context.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if this initializer should run for the current cycle;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// Use this method to conditionally opt out of initialization at runtime.
	/// For example, an initializer that requires an authenticated user can return
	/// <see langword="false"/> when <see cref="IUserState.IsAuthenticated"/> is
	/// <see langword="false"/>.
	/// </remarks>
	bool ShouldInitialize(IUserState userState);

	/// <summary>
	/// Performs the initialization work for this service.
	/// </summary>
	/// <param name="updateStatus">
	/// A callback that updates the current activity status message.
	/// Implementations may call this to report finer-grained progress within
	/// a long-running initialization step, such as
	/// <c>updateStatus("Loading page 2 of 5...")</c>.
	/// The orchestrator should set the initial status from
	/// <see cref="InitializationMessage"/> before invoking this method, so
	/// calling <paramref name="updateStatus"/> is optional.
	/// </param>
	/// <param name="cancellationToken">
	/// A token to monitor for cancellation requests.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous initialization operation.
	/// </returns>
	Task InitializeAsync(Action<string> updateStatus, CancellationToken cancellationToken = default);
}