namespace Cirreum.State;
/// <summary>
/// Defines the core contract for a remote state object that manages in-memory domain data
/// fetched from backend services.
/// </summary>
/// <remarks>
/// <para>
/// Remote state is a specialized type of application state designed for managing domain data
/// that is loaded from APIs and cached in memory for the lifetime of the application. It
/// leverages the State notification system to propagate changes to subscribers.
/// </para>
/// <para>
/// While other state types like <see cref="ISessionState"/> and <see cref="ILocalState"/>
/// manage application state with browser storage persistence, remote state focuses on
/// transient domain data with no persistence — the data is fetched fresh on each
/// application startup.
/// </para>
/// <para>
/// Remote state provides a caching layer for API responses, eliminating the need
/// for individual pages to manage their own loading states and data fetching logic.
/// Combined with <see cref="IInitializableRemoteState"/>, it integrates with the startup
/// pipeline to pre-load data before the user interacts with the application.
/// </para>
/// </remarks>
public interface IRemoteState : IApplicationState {

	/// <summary>
	/// Gets a value indicating whether the data has been successfully loaded.
	/// </summary>
	/// <remarks>
	/// This property is <c>true</c> after the initial load operation completes successfully,
	/// and remains <c>true</c> even during subsequent refresh operations.
	/// </remarks>
	bool IsLoaded { get; }

	/// <summary>
	/// Gets a value indicating whether the initial data loading operation is in progress.
	/// </summary>
	/// <remarks>
	/// This property is <c>true</c> only during the first load operation.
	/// For subsequent data fetches, see <see cref="IsRefreshing"/>.
	/// </remarks>
	bool IsLoading { get; }

	/// <summary>
	/// Gets a value indicating whether the data is being refreshed.
	/// </summary>
	/// <remarks>
	/// This property is <c>true</c> when fetching updated data after the initial load.
	/// Use this to show non-blocking refresh indicators while preserving existing data display.
	/// </remarks>
	bool IsRefreshing { get; }

	/// <summary>
	/// Loads data if not already loaded or loading.
	/// </summary>
	Task LoadAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Refreshes data if not already loading or refreshing.
	/// </summary>
	Task RefreshAsync(CancellationToken cancellationToken = default);

}