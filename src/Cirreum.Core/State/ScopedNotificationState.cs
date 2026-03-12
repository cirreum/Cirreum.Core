namespace Cirreum.State;
/// <summary>
/// Abstract base class for state that provides notification scoping and batching functionality.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to get automatic notification batching capabilities.
/// Alternatively, implement <see cref="IScopedNotificationState"/> directly if you need
/// custom notification behavior.
/// </para>
/// <para>
/// <strong>Choosing between sync and async scopes:</strong>
/// </para>
/// <list type="bullet">
///   <item>
///     Use <see cref="CreateNotificationScope"/> when your state change notification is synchronous —
///     for example, notifying JS interop or lightweight in-memory UI subscribers via
///     <c>stateManager.NotifySubscribers</c>.
///   </item>
///   <item>
///     Use <see cref="CreateNotificationScopeAsync"/> when your state change notification involves
///     awaitable work — for example, notifying async subscribers via
///     <c>stateManager.NotifySubscribersAsync</c>.
///   </item>
/// </list>
/// <para>
/// Both scope types support batching: if multiple scopes are nested, <see cref="OnStateHasChanged"/>
/// or <see cref="OnStateHasChangedAsync"/> is only invoked when the outermost scope completes,
/// ensuring a single notification per batch of state mutations.
/// </para>
/// </remarks>
public abstract class ScopedNotificationState : IScopedNotificationState {

	private int _scopeCount;

	// -------------------------------------------------------------------------
	// Scope Factory
	// -------------------------------------------------------------------------

	/// <inheritdoc/>
	public IDisposable CreateNotificationScope() {
		this.StartNewScope();
		return new NotificationScope(this.EndScopeAndTryNotify);
	}

	/// <inheritdoc/>
	public IAsyncDisposable CreateNotificationScopeAsync() {
		this.StartNewScope();
		return new AsyncNotificationScope(this.EndScopeAndTryNotifyAsync);
	}

	// -------------------------------------------------------------------------
	// Overridable Notification Hooks
	// -------------------------------------------------------------------------

	/// <summary>
	/// Called synchronously when state changes and all notification scopes have completed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Override this method to provide state-specific sync notification logic. Typically,
	/// implementations call <c>stateManager.NotifySubscribers&lt;TStateInterface&gt;(this)</c>
	/// to notify sync subscribers — for example, JS interop or lightweight in-memory UI state.
	/// </para>
	/// <para>
	/// This method is called by <see cref="NotifyStateChanged"/> and by the sync
	/// <see cref="CreateNotificationScope"/> path when the outermost scope completes.
	/// </para>
	/// <para>
	/// For async notification work, override <see cref="OnStateHasChangedAsync"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// protected override void OnStateHasChanged() {
	///     stateManager.NotifySubscribers&lt;IMyState&gt;(this);
	/// }
	/// </code>
	/// </example>
	protected abstract void OnStateHasChanged();

	/// <summary>
	/// Called asynchronously when state changes and all notification scopes have completed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Override this method to provide state-specific async notification logic. Typically,
	/// implementations call <c>await stateManager.NotifySubscribersAsync&lt;TStateInterface&gt;(this)</c>
	/// to notify async subscribers — for example, persistence, browser storage, navigation,
	/// or app user state hydration.
	/// </para>
	/// <para>
	/// This method is called by <see cref="NotifyStateChangedAsync"/> and by the async
	/// <see cref="CreateNotificationScopeAsync"/> path when the outermost scope completes.
	/// </para>
	/// <para>
	/// The default implementation is a no-op. Override only when async notification is needed.
	/// For sync notification work, override <see cref="OnStateHasChanged"/> instead.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// protected override async Task OnStateHasChangedAsync() {
	///     await stateManager.NotifySubscribersAsync&lt;IMyState&gt;(this);
	/// }
	/// </code>
	/// </example>
	protected virtual Task OnStateHasChangedAsync() => Task.CompletedTask;

	// -------------------------------------------------------------------------
	// Protected Notify Triggers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Synchronously triggers state change notification if no notification scopes are active.
	/// </summary>
	/// <remarks>
	/// Call this from state mutation methods to signal that the state has changed.
	/// When active scopes exist, notification is suppressed until the outermost scope completes,
	/// batching multiple mutations into a single notification.
	/// </remarks>
	protected virtual void NotifyStateChanged() {
		if (this._scopeCount > 0) {
			return;
		}
		this.OnStateHasChanged();
	}

	/// <summary>
	/// Asynchronously triggers state change notification if no notification scopes are active.
	/// </summary>
	/// <remarks>
	/// Call this from async state mutation methods to signal that the state has changed.
	/// When active scopes exist, notification is suppressed until the outermost scope completes,
	/// batching multiple mutations into a single notification.
	/// </remarks>
	protected virtual async Task NotifyStateChangedAsync() {
		if (this._scopeCount > 0) {
			return;
		}
		await this.OnStateHasChangedAsync();
	}

	// -------------------------------------------------------------------------
	// Internal Scope Tracking
	// -------------------------------------------------------------------------

	private void StartNewScope() => Interlocked.Increment(ref this._scopeCount);

	private void EndScopeAndTryNotify() {
		var count = Interlocked.Decrement(ref this._scopeCount);
		if (count == 0) {
			this.OnStateHasChanged();
		} else if (count < 0) {
			throw new InvalidOperationException("Notification scope ended without a matching start.");
		}
	}

	private async Task EndScopeAndTryNotifyAsync() {
		var count = Interlocked.Decrement(ref this._scopeCount);
		if (count == 0) {
			await this.OnStateHasChangedAsync();
		} else if (count < 0) {
			throw new InvalidOperationException("Notification scope ended without a matching start.");
		}
	}

	// -------------------------------------------------------------------------
	// Scope Tokens
	// -------------------------------------------------------------------------

	/// <summary>
	/// Synchronous notification scope token. Calls the end-scope action on dispose.
	/// </summary>
	private sealed class NotificationScope(Action endScope) : IDisposable {
		private bool _isDisposed;

		public void Dispose() {
			if (!this._isDisposed) {
				this._isDisposed = true;
				endScope();
			}
		}
	}

	/// <summary>
	/// Asynchronous notification scope token. Calls the async end-scope action on dispose.
	/// </summary>
	private sealed class AsyncNotificationScope(Func<Task> endScope) : IAsyncDisposable {
		private bool _isDisposed;

		public async ValueTask DisposeAsync() {
			if (!this._isDisposed) {
				this._isDisposed = true;
				await endScope();
			}
		}
	}

}