namespace Cirreum.State;

using Cirreum.Security;

/// <summary>
/// Defines a service that participates in application initialization during startup.
/// </summary>
/// <remarks>
/// <para>
/// Services implementing this interface are discovered and initialized by the
/// <see cref="IInitializationOrchestrator"/> during application startup. They integrate
/// with <see cref="IInitializationState"/> to provide progress updates for splash screens
/// or loading indicators.
/// </para>
/// <para>
/// The <see cref="DisplayName"/> and <see cref="InitializationMessage"/> properties
/// provide user-friendly status updates, while <see cref="Order"/> controls the
/// initialization sequence. The <see cref="ShouldInitialize"/> method allows services
/// to opt out of initialization based on runtime conditions.
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
/// <seealso cref="IInitializationState"/>
/// <seealso cref="IInitializationOrchestrator"/>
public interface IInitializable {

	/// <summary>
	/// Gets the display name of this initializable service.
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// Gets the message that describes the initialization process.
	/// </summary>
	/// <example>"Loading Events..."</example>
	string InitializationMessage { get; }

	/// <summary>
	/// Gets the order in which this service is initialized relative to others.
	/// Lower values execute first. Default convention is 1000.
	/// </summary>
	int Order { get; }

	/// <summary>
	/// Determines whether this service should participate in the current
	/// initialization cycle.
	/// </summary>
	/// <param name="userState">
	/// The current user state, enabling conditional initialization based on
	/// authentication or user context.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the service should initialize;
	/// <see langword="false"/> to skip initialization.
	/// </returns>
	/// <remarks>
	/// Use this to conditionally opt out of initialization based on runtime state.
	/// For example, a service that requires authentication can return <see langword="false"/>
	/// when <see cref="IUserState.IsAuthenticated"/> is <see langword="false"/>.
	/// </remarks>
	bool ShouldInitialize(IUserState userState);

	/// <summary>
	/// Performs the initialization work for this service.
	/// </summary>
	/// <param name="updateStatus">
	/// A callback to update the splash screen status message during initialization.
	/// Call this to provide granular progress within a long-running task
	/// (e.g., <c>updateStatus("Loading page 2 of 5...")</c>). The orchestrator
	/// sets the initial status from <see cref="InitializationMessage"/> before
	/// calling this method, so calling <paramref name="updateStatus"/> is optional.
	/// </param>
	/// <param name="cancellationToken">
	/// A token to monitor for cancellation requests.
	/// </param>
	Task InitializeAsync(Action<string> updateStatus, CancellationToken cancellationToken = default);

}