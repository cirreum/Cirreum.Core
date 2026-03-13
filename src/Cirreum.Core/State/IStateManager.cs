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
/// and consistent behavior across the application. This is enforced at compile time via
/// generic type constraints.
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
	/// Subscribers are notified via <see cref="NotifySubscribers{TState}(TState)"/>.
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
	/// Subscribers are notified via <see cref="NotifySubscribers{TState}(TState)"/>.
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
	// Notify
	// -------------------------------------------------------------------------

	/// <summary>
	/// Notifies all sync subscribers that the specified state type has changed.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <remarks>
	/// <para>
	/// Retrieves the current registered state instance and broadcasts it to all
	/// subscribers registered via <see cref="Subscribe{TState}(Action)"/> or
	/// <see cref="Subscribe{TState}(Action{TState})"/>.
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
	/// Subscribers registered via <see cref="Subscribe{TState}(Action)"/> or
	/// <see cref="Subscribe{TState}(Action{TState})"/> are notified.
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

}