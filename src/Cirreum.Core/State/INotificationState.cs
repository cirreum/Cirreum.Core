namespace Cirreum.State;

/// <summary>
/// Defines the full notification state contract, extending scoped access with
/// management operations for adding, reading, and removing notifications.
/// </summary>
public interface INotificationState : IScopedNotificationState {

	/// <summary>
	/// Gets the complete list of current notifications.
	/// </summary>
	/// <remarks>
	/// Typically, in descending chronological order.
	/// </remarks>
	IReadOnlyList<Notification> Notifications { get; }

	/// <summary>
	/// Gets the count of notifications that have not yet been read.
	/// </summary>
	int UnreadCount { get; }

	/// <summary>
	/// Adds a new notification to the state.
	/// </summary>
	/// <param name="notification">The <see cref="Notification"/> to add.</param>
	void AddNotification(Notification notification);

	/// <summary>
	/// Marks a specific notification as read.
	/// </summary>
	/// <param name="notificationId">The unique identifier of the notification to mark as read.</param>
	void MarkAsRead(string notificationId);

	/// <summary>
	/// Marks all current notifications as read.
	/// </summary>
	void MarkAllAsRead();

	/// <summary>
	/// Removes a specific notification from the state.
	/// </summary>
	/// <param name="notificationId">The unique identifier of the notification to remove.</param>
	void RemoveNotification(string notificationId);

	/// <summary>
	/// Removes all notifications from the state.
	/// </summary>
	void ClearAll();

	/// <summary>
	/// Triggers a state change notification to force subscribers to re-evaluate bound properties.
	/// </summary>
	void Refresh();

}