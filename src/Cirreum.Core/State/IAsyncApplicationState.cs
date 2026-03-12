namespace Cirreum.State;
/// <summary>
/// Marker interface for state types that notify subscribers asynchronously.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on state interfaces that require async notification — for example,
/// state that drives persistence, browser storage, API calls, navigation, or app user hydration.
/// </para>
/// <para>
/// State types implementing <see cref="IAsyncApplicationState"/> must use
/// <see cref="IStateManager.SubscribeAsync{TState}(Func{TState, Task})"/> to register subscribers
/// and <see cref="IStateManager.NotifySubscribersAsync{TState}(System.Threading.CancellationToken)"/>
/// to notify them. These constraints are enforced at compile time via generic type constraints
/// on <see cref="IStateManager"/>.
/// </para>
/// <para>
/// Since <see cref="IAsyncApplicationState"/> extends <see cref="IApplicationState"/>, async state
/// types automatically satisfy all constraints that require <see cref="IApplicationState"/> — for
/// example, <see cref="IStateManager.Get{TState}"/> and the sync <c>Subscribe</c> overloads remain
/// accessible, though sync subscriptions will not receive async notifications.
/// </para>
/// <para>
/// <strong>When to use:</strong>
/// </para>
/// <list type="bullet">
///   <item>State that hydrates an application user after authentication</item>
///   <item>State that triggers navigation on change</item>
///   <item>State that persists to browser storage or an API on change</item>
///   <item>Any state where <see cref="ScopedNotificationState.OnStateHasChangedAsync"/> in <see cref="ScopedNotificationState"/> is overridden</item>
/// </list>
/// <para>
/// <strong>When NOT to use:</strong>
/// </para>
/// <list type="bullet">
///   <item>State that drives synchronous JS interop — use <see cref="IApplicationState"/> instead</item>
///   <item>Lightweight in-memory UI state such as theme or page state</item>
///   <item>State used in Server or Serverless hosting environments where <see cref="IStateManager"/> is not registered</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Declare an async state interface
/// public interface IUserState : IAsyncApplicationState { ... }
///
/// // Subscribe to async notifications
/// stateManager.SubscribeAsync&lt;IUserState&gt;(async state =>
/// {
///     await localStorage.SetItemAsync("userId", state.Id);
///     navigation.NavigateTo(state.IsNewUser ? Routes.Onboard : Routes.Dashboard);
/// });
///
/// // Notify async subscribers
/// await stateManager.NotifySubscribersAsync&lt;IUserState&gt;();
/// </code>
/// </example>
public interface IAsyncApplicationState : IApplicationState;