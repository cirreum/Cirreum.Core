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
/// The scoping mechanism supports nested operations, ensuring that notifications are only
/// triggered when the outermost scope completes, similar to transaction boundaries in databases.
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

	// -------------------------------------------------------------------------
	// Overridable Notification Hook
	// -------------------------------------------------------------------------

	/// <summary>
	/// Called when state changes and all notification scopes have completed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Override this method to provide state-specific notification logic. Typically,
	/// implementations call <c>stateManager.NotifySubscribers&lt;TStateInterface&gt;(this)</c>
	/// to notify subscribers — for example, JS interop or lightweight in-memory UI state.
	/// </para>
	/// <para>
	/// This method is called by <see cref="NotifyStateChanged"/> and by
	/// <see cref="CreateNotificationScope"/> when the outermost scope completes.
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

	// -------------------------------------------------------------------------
	// Protected Notify Trigger
	// -------------------------------------------------------------------------

	/// <summary>
	/// Triggers state change notification if no notification scopes are active.
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

	// -------------------------------------------------------------------------
	// Scope Token
	// -------------------------------------------------------------------------

	/// <summary>
	/// Notification scope token. Calls the end-scope action on dispose.
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

}
