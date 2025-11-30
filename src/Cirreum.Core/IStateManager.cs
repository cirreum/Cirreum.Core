namespace Cirreum;

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
/// and consistent behavior across the application.
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

	/// <summary>
	/// Subscribes to state change notifications for the specified state type.
	/// </summary>
	/// <typeparam name="TState">The state type to monitor for changes.</typeparam>
	/// <param name="handler">The action to invoke when the state changes.</param>
	/// <returns>
	/// A disposable subscription token. Dispose this to unsubscribe and prevent memory leaks.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Use this overload when you only need to know that a state change occurred,
	/// without requiring access to the updated state instance. This is common in UI
	/// scenarios where a change notification triggers a re-render.
	/// </para>
	/// <para>
	/// The subscription remains active until the returned <see cref="IDisposable"/> is disposed.
	/// Failing to dispose subscriptions will result in memory leaks and continued notifications
	/// to stale references.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Subscribe to state changes
	/// var subscription = stateManager.Subscribe&lt;IUserState&gt;(() => RefreshUI());
	/// 
	/// // Unsubscribe when done
	/// subscription.Dispose();
	/// </code>
	/// </example>
	IDisposable Subscribe<TState>(Action handler) where TState : IApplicationState;

	/// <summary>
	/// Subscribes to state change notifications for the specified state type,
	/// receiving the updated state instance.
	/// </summary>
	/// <typeparam name="TState">The state type to monitor for changes.</typeparam>
	/// <param name="handler">
	/// The action to invoke when the state changes, receiving the updated state instance.
	/// </param>
	/// <returns>
	/// A disposable subscription token. Dispose this to unsubscribe and prevent memory leaks.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Use this overload when you need access to the updated state instance in your handler.
	/// This is useful for conditional logic based on state values, logging, or updating
	/// derived state.
	/// </para>
	/// <para>
	/// The subscription remains active until the returned <see cref="IDisposable"/> is disposed.
	/// Failing to dispose subscriptions will result in memory leaks and continued notifications
	/// to stale references.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Subscribe with state access
	/// var subscription = stateManager.Subscribe&lt;IUserState&gt;(userState => 
	/// {
	///     Console.WriteLine($"User changed: {userState.Name}");
	///     if (userState.IsActive)
	///     {
	///         StartSession();
	///     }
	/// });
	/// 
	/// // Unsubscribe when done
	/// subscription.Dispose();
	/// </code>
	/// </example>
	IDisposable Subscribe<TState>(Action<TState> handler) where TState : IApplicationState;

	/// <summary>
	/// Notifies all subscribers that the specified state type has changed.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <remarks>
	/// <para>
	/// This method retrieves the current state instance and broadcasts the change to all
	/// subscribers. Use this when you have modified state in-place and want to notify
	/// all subscribers of the changes.
	/// </para>
	/// <para>
	/// For batched updates, consider using <see cref="IScopedNotificationState.CreateNotificationScope"/>
	/// to defer notifications until all changes are complete.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Modify state and notify
	/// var userState = stateManager.Get&lt;IUserState&gt;();
	/// userState.Name = "Jane Doe";
	/// stateManager.NotifySubscribers&lt;IUserState&gt;();
	/// </code>
	/// </example>
	void NotifySubscribers<TState>() where TState : class, IApplicationState;

	/// <summary>
	/// Notifies all subscribers that the specified state instance has changed.
	/// </summary>
	/// <typeparam name="TState">The state type that has changed.</typeparam>
	/// <param name="state">The updated state instance to broadcast to subscribers.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="state"/> is null.
	/// </exception>
	/// <remarks>
	/// <para>
	/// Use this overload when you already have a reference to the state instance.
	/// Implementations may validate that the provided instance matches the registered
	/// state to ensure consistency.
	/// </para>
	/// <para>
	/// For batched updates, consider using <see cref="IScopedNotificationState.CreateNotificationScope"/>
	/// to defer notifications until all changes are complete.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Modify and notify with explicit instance
	/// var userState = stateManager.Get&lt;IUserState&gt;();
	/// userState.Name = "Alice Johnson";
	/// stateManager.NotifySubscribers(userState);
	/// </code>
	/// </example>
	void NotifySubscribers<TState>(TState state) where TState : class, IApplicationState;

}