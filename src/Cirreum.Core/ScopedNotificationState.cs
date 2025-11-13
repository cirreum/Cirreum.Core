namespace Cirreum;

/// <summary>
/// Abstract base class for state that provides notification scoping functionality.
/// </summary>
/// <remarks>
/// Inherit from this class to get automatic notification batching capabilities.
/// Alternatively, implement <see cref="IScopedNotificationState"/> directly if you need custom notification behavior.
/// </remarks>
public abstract class ScopedNotificationState : IScopedNotificationState {

	private int _scopeCount;

	/// <inheritdoc/>
	public IDisposable CreateNotificationScope() {
		this.StartNewScope();
		return new NotificationScope(this.EndScopeAndTryNotify);
	}

	/// <inheritdoc/>
	public IAsyncDisposable CreateNotificationScopeAsync() {
		this.StartNewScope();
		return new NotificationScope(this.EndScopeAndTryNotify);
	}

	/// <summary>
	/// Called when state changes and all notification scopes have completed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Override this method in concrete implementations to provide state-specific notification logic.
	/// Typically, implementations should call <c>stateManager.NotifySubscribers&lt;TStateInterface&gt;(this)</c>
	/// to notify all subscribers of the state change.
	/// </para>
	/// <para>
	/// This method is called by <see cref="NotifyStateChanged"/> when no notification scopes are active,
	/// ensuring that notifications are properly batched and only sent when appropriate.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // from a concrete state implementation
	/// protected override void OnStateHasChanged() {
	///     stateManager.NotifySubscribers&lt;IMyState&gt;(this);
	/// }
	/// </code>
	/// </example>
	protected abstract void OnStateHasChanged();

	/// <summary>
	/// Triggers state change notifications if no notification scopes are currently active.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is typically called internally by state mutation operations to signal that
	/// the state has changed. It respects notification scoping by suppressing notifications
	/// when <see cref="CreateNotificationScope"/> has created active scopes.
	/// </para>
	/// <para>
	/// When no scopes are active (scope count is zero), this method calls <see cref="OnStateHasChanged"/>
	/// to allow the concrete implementation to perform the appropriate notification logic.
	/// This pattern enables batching of multiple state changes into a single notification.
	/// </para>
	/// </remarks>
	protected virtual void NotifyStateChanged() {
		if (this._scopeCount > 0) {
			return;
		}

		this.OnStateHasChanged();
	}

	private void StartNewScope() => this._scopeCount++;
	private void EndScopeAndTryNotify() {
		this._scopeCount--;
		if (this._scopeCount == 0) {
			this.OnStateHasChanged();
		} else if (this._scopeCount < 0) {
			throw new InvalidOperationException("Notification scope ended without a matching start.");
		}
	}
	private sealed class NotificationScope(Action endBatch) : IDisposable, IAsyncDisposable {
		private bool _isDisposed;

		public void Dispose() {
			if (!this._isDisposed) {
				this._isDisposed = true;
				endBatch();
			}
		}

		public ValueTask DisposeAsync() {
			this.Dispose();
			return ValueTask.CompletedTask;
		}
	}

}