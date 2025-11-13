namespace Cirreum.Conductor;

/// <summary>
/// Defines a handler for a notification.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled.</typeparam>
public interface INotificationHandler<in TNotification>
	where TNotification : INotification {

	/// <summary>
	/// Handles the notification asynchronously.
	/// </summary>
	/// <param name="notification">The notification instance to handle.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task HandleAsync(
		TNotification notification,
		CancellationToken cancellationToken = default);
}