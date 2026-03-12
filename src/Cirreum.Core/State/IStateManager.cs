namespace Cirreum.State;
/// <summary>
/// Central orchestrator for application state management, providing state
/// retrieval, subscription, and notification capabilities.
/// </summary>
/// <remarks>
/// <para>
/// The state manager serves as the primary interface for managing application state throughout
/// the lifecycle of your application. It provides a unified API for:
/// </para>
/// <list type="bullet">
///   <item>Retrieving registered state instances</item>
///   <item>Subscribing to state change notifications</item>
///   <item>Broadcasting state changes to subscribers</item>
///   <item>Managing subscription lifecycles with automatic cleanup</item>
/// </list>
/// <para>
/// All state types must implement <see cref="IApplicationState"/> to ensure type safety
/// and consistent behavior across the application. State types that require async notification
/// must implement <see cref="IAsyncApplicationState"/> and use <c>SubscribeAsync</c> and
/// <c>NotifySubscribersAsync</c>. This is enforced at compile time via generic type constraints.
/// </para>
/// <para>
/// <strong>Why two notification paths exist:</strong>
/// </para>
/// <para>
/// The sync and async paths are not interchangeable — they map to fundamentally different
/// runtime capabilities in Blazor WASM. In WASM, JavaScript runs on the same thread as .NET,
/// which enables direct synchronous JS interop calls with zero task scheduling overhead.
/// This makes sync subscribers ideal for JS interop, theme changes, and in-memory UI state
/// where immediate execution and low latency matter for perceived performance.
/// </para>
/// <para>
/// Collapsing both paths into a single async path would force sync JS calls through unnecessary
/// task machinery, eliminating this performance characteristic. The two lists are load-bearing
/// for WASM's performance model — not a design preference.
/// </para>
/// <para>
/// <strong>Choosing between sync and async subscriptions:</strong>
/// </para>
/// <list type="bullet">
///   <item>
///     Use <see cref="Subscribe{TState}(Action)"/> and <see cref="Subscribe{TState}(Action{TState})"/>
///     for synchronous work that benefits from immediate execution — JS interop calls, in-memory
///     UI state, theme or page state changes. State types must implement <see cref="IApplicationState"/>.
///   </item>
///   <item>
///     Use <see cref="SubscribeAsync{TState}(Func{Task})"/> and <see cref="SubscribeAsync{TState}(Func{TState, Task})"/>
///     for awaitable work — persistence, browser storage, API calls, navigation, or app user
///     state hydration. State types must implement <see cref="IAsyncApplicationState"/>.
///   </item>
/// </list>
/// <para>
///   Sync and async subscribers are maintained in separate lists and notified via their
///   respective notify methods. <see cref="NotifySubscribers{TState}(TState)"/> only fires
///   sync subscribers. <see cref="NotifySubscribersAsync{TState}(CancellationToken)"/> and
///   <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/> only fire async
///   subscribers — prefer the parameterless overload for convenience, use the instance overload
///   to tolerate casting or derived type scenarios.
/// </para>
/// </remarks>
public interface IStateManager {

	/// <summary>
	/// Retrieves the registered state instance of the specified type.
	/// </summary>
	/// <typeparam name="TState">
	/// The state type to retrieve. Must implement <see cref="IApplicationState"/>.
	/// </typeparam>
	/// <returns>The registered state instance.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the requested state type has not been registered.
	/// </exception>
	/// <remarks>
	/// This method provides convenient access to state instances without requiring
	/// direct service resolution. Both interface types and concrete types are supported,
	/// depending on how the state was registered.
	/// </remarks>
	/// <example>
	/// <code>
	/// // Retrieve state by interface
	/// var userState = stateManager.Get&lt;IUserState&gt;();
	/// var currentUser = userState.CurrentUser;
	/// 
	/// // Retrieve state by concrete type
	/// var cartState = stateManager.Get&lt;ShoppingCartState&gt;();
	/// </code>
	/// </example>
	TState Get<TState>() where TState : IApplicationState;

	// -------------------------------------------------------------------------
	// Sync Subscriptions
	// Use for JS interop, in-memory UI state, theme, page state.
	// Notified via NotifySubscribers.
	// -------------------------------------------------------------------------

	/// <summary>
	/// Subscribes synchronously to state change notifications for the specified state type.
	/// </summary>
	/// <typeparam name="TState">The state type to monitor for changes.</typeparam>
	/// <param name="handler">The action to invoke when the state changes.</param>
	/// <returns>
	/// A disposable subscription token. Dispose to unsubscribe and prevent memory leaks.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Use this overload when you only need to know that a state change occurred,
	/// without requiring access to the updated state instance. This is common in UI
	/// scenarios where a change notification triggers a re-render or a synchronous
	/// JS interop call.
	/// </para>
	/// <para>
	/// Sync subscribers are only notified via <see cref="NotifySubscribers{TState}(TState)"/>.
	/// For async work, use <see cref="SubscribeAsync{TState}(Func{Task})"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var subscription = stateManager.Subscribe&lt;IThemeState&gt;(() => ApplyTheme());
	/// subscription.Dispose(); // unsubscribe when done
	/// </code>
	/// </example>
	IDisposable Subscribe<TState>(Action handler) where TState : IApplicationState;

	/// <summary>
	/// Subscribes synchronously to state change notifications for the specified state type,
	/// receiving the updated state instance.
	/// </summary>
	/// <typeparam name="TState">The state type to monitor for changes.</typeparam>
	/// <param name="handler">
	/// The action to invoke when the state changes, receiving the updated state instance.
	/// </param>
	/// <returns>
	/// A disposable subscription token. Dispose to unsubscribe and prevent memory leaks.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Use this overload when you need access to the updated state instance in your handler
	/// for conditional logic, JS interop, or lightweight in-memory UI updates.
	/// </para>
	/// <para>
	/// Sync subscribers are only notified via <see cref="NotifySubscribers{TState}(TState)"/>.
	/// For async work, use <see cref="SubscribeAsync{TState}(Func{TState, Task})"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var subscription = stateManager.Subscribe&lt;IThemeState&gt;(state => 
	/// {
	///     jsModule.ApplyTheme(state.CurrentTheme);
	/// });
	/// subscription.Dispose();
	/// </code>
	/// </example>
	IDisposable Subscribe<TState>(Action<TState> handler) where TState : IApplicationState;

	// -------------------------------------------------------------------------
	// Async Subscriptions
	// Use for persistence, browser storage, API calls, navigation, app user state.
	// Notified via NotifySubscribersAsync.
	// -------------------------------------------------------------------------

	/// <summary>
	/// Subscribes asynchronously to state change notifications for the specified state type.
	/// </summary>
	/// <typeparam name="TState">The state type to monitor for changes.</typeparam>
	/// <param name="handler">The async function to invoke when the state changes.</param>
	/// <returns>
	/// A disposable subscription token. Dispose to unsubscribe and prevent memory leaks.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Use this overload when you only need to know that a state change occurred
	/// and your handler performs awaitable work such as persistence, browser storage,
	/// or API calls, but does not require access to the updated state instance.
	/// </para>
	/// <para>
	/// Async subscribers are only notified via
	/// <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/>.
	/// For lightweight sync work, use <see cref="Subscribe{TState}(Action)"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var subscription = stateManager.SubscribeAsync&lt;IUserState&gt;(async () =>
	/// {
	///     await sessionStorage.ClearAsync();
	/// });
	/// subscription.Dispose();
	/// </code>
	/// </example>
	IDisposable SubscribeAsync<TState>(Func<Task> handler) where TState : IAsyncApplicationState;

	/// <summary>
	/// Subscribes asynchronously to state change notifications for the specified state type,
	/// receiving the updated state instance.
	/// </summary>
	/// <typeparam name="TState">The state type to monitor for changes.</typeparam>
	/// <param name="handler">
	/// The async function to invoke when the state changes, receiving the updated state instance.
	/// </param>
	/// <returns>
	/// A disposable subscription token. Dispose to unsubscribe and prevent memory leaks.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Use this overload when your handler performs awaitable work and requires access
	/// to the updated state instance — for example, persisting state to browser storage,
	/// making API calls based on state values, or triggering navigation after app user
	/// hydration completes.
	/// </para>
	/// <para>
	/// Async subscribers are notified sequentially in registration order via
	/// <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/>.
	/// Each subscriber is awaited before the next is invoked.
	/// Async subscribers are only notified via
	/// <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/>.
	/// For lightweight sync work, use <see cref="Subscribe{TState}(Action{TState})"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var subscription = stateManager.SubscribeAsync&lt;IUserState&gt;(async state =>
	/// {
	///     await localStorage.SetItemAsync("lastUser", state.Id);
	///     navigation.NavigateTo(state.IsNewUser ? Routes.Onboard : Routes.Dashboard);
	/// });
	/// subscription.Dispose();
	/// </code>
	/// </example>
	IDisposable SubscribeAsync<TState>(Func<TState, Task> handler) where TState : IAsyncApplicationState;

	// -------------------------------------------------------------------------
	// Sync Notify
	// Fires sync subscribers only.
	// -------------------------------------------------------------------------

	/// <summary>
	/// Notifies all sync subscribers that the specified state type has changed.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <remarks>
	/// <para>
	/// Retrieves the current registered state instance and broadcasts it to all
	/// sync subscribers registered via <see cref="Subscribe{TState}(Action)"/> or
	/// <see cref="Subscribe{TState}(Action{TState})"/>.
	/// </para>
	/// <para>
	/// Async subscribers registered via <c>SubscribeAsync</c> are not notified.
	/// Use <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/> for those.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var themeState = stateManager.Get&lt;IThemeState&gt;();
	/// themeState.SetTheme(Theme.Dark);
	/// stateManager.NotifySubscribers&lt;IThemeState&gt;();
	/// </code>
	/// </example>
	void NotifySubscribers<TState>() where TState : class, IApplicationState;

	/// <summary>
	/// Notifies all sync subscribers with the provided state instance.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <param name="state">The updated state instance to broadcast to sync subscribers.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="state"/> is null.
	/// </exception>
	/// <remarks>
	/// <para>
	/// Use this overload when you already have a reference to the updated state instance.
	/// Only sync subscribers registered via <see cref="Subscribe{TState}(Action)"/> or
	/// <see cref="Subscribe{TState}(Action{TState})"/> are notified.
	/// </para>
	/// <para>
	/// Async subscribers registered via <c>SubscribeAsync</c> are not notified.
	/// Use <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/> for those.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var themeState = stateManager.Get&lt;IThemeState&gt;();
	/// themeState.SetTheme(Theme.Dark);
	/// stateManager.NotifySubscribers(themeState);
	/// </code>
	/// </example>
	void NotifySubscribers<TState>(TState state) where TState : class, IApplicationState;

	// -------------------------------------------------------------------------
	// Async Notify
	// Fires async subscribers only.
	// -------------------------------------------------------------------------

	/// <summary>
	/// Notifies all async subscribers that the specified state type has changed.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to cancel pending notifications.
	/// </param>
	/// <returns>A <see cref="Task"/> that completes when all async subscribers have been notified.</returns>
	/// <remarks>
	/// <para>
	/// Retrieves the current registered state instance from DI and broadcasts it to all
	/// async subscribers registered via <see cref="SubscribeAsync{TState}(Func{Task})"/> or
	/// <see cref="SubscribeAsync{TState}(Func{TState, Task})"/> sequentially in registration order.
	/// Each subscriber is awaited before the next is invoked.
	/// </para>
	/// <para>
	/// Prefer this overload when you have mutated state in place and do not need to supply
	/// an explicit instance. Use <see cref="NotifySubscribersAsync{TState}(TState, CancellationToken)"/>
	/// when you need to supply a specific instance to tolerate casting or derived type scenarios.
	/// </para>
	/// <para>
	/// Sync subscribers registered via <c>Subscribe</c> are not notified.
	/// Use <see cref="NotifySubscribers{TState}()"/> for those.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var userState = stateManager.Get&lt;IUserState&gt;();
	/// userState.SetUser(authenticatedUser);
	/// await stateManager.NotifySubscribersAsync&lt;IUserState&gt;();
	/// </code>
	/// </example>
	Task NotifySubscribersAsync<TState>(CancellationToken cancellationToken = default)
		where TState : class, IAsyncApplicationState;

	/// <summary>
	/// Notifies all async subscribers with the provided state instance.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <param name="state">The updated state instance to broadcast to async subscribers.</param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to cancel pending notifications.
	/// </param>
	/// <returns>A <see cref="Task"/> that completes when all async subscribers have been notified.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="state"/> is null.
	/// </exception>
	/// <remarks>
	/// <para>
	/// Use this overload when you already have a reference to the updated state instance,
	/// particularly in casting or derived type scenarios where the instance may differ
	/// from the registered DI type. Async subscribers are notified sequentially in
	/// registration order — each subscriber is awaited before the next is invoked.
	/// </para>
	/// <para>
	/// Prefer <see cref="NotifySubscribersAsync{TState}(CancellationToken)"/> when you do
	/// not need to supply an explicit instance.
	/// </para>
	/// <para>
	/// Sync subscribers registered via <c>Subscribe</c> are not notified.
	/// Use <see cref="NotifySubscribers{TState}(TState)"/> for those.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Casting scenario — concrete type passed where interface is registered
	/// var clientUser = serviceProvider.GetRequiredService&lt;ClientUser&gt;();
	/// await stateManager.NotifySubscribersAsync(clientUser);
	/// </code>
	/// </example>
	Task NotifySubscribersAsync<TState>(TState state, CancellationToken cancellationToken = default)
		where TState : class, IAsyncApplicationState;

}