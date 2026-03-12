namespace Cirreum.State;

/// <summary>
/// Represents an in-app notification with optional action support.
/// </summary>
/// <param name="Id">The unique identifier for the notification.</param>
/// <param name="Title">The notification title displayed as a heading.</param>
/// <param name="Message">The body content of the notification.</param>
/// <param name="Type">The severity/category type of the notification.</param>
/// <param name="Timestamp">The UTC date and time the notification was created.</param>
/// <param name="IsRead">Whether the notification has been read by the user. Defaults to <c>false</c>.</param>
/// <param name="IsDismissed">Whether the notification has been dismissed by the user. Defaults to <c>false</c>.</param>
/// <param name="ActionUrl">An optional URL the user can navigate to from the notification.</param>
/// <param name="ActionText">The display text for the optional action link.</param>
public record Notification(
	string Id,
	string Title,
	string Message,
	NotificationType Type,
	DateTime Timestamp,
	bool IsRead = false,
	bool IsDismissed = false,
	string? ActionUrl = null,
	string? ActionText = null
) {
	/// <summary>
	/// Creates a new <see cref="Notification"/> with a generated ID and current UTC timestamp.
	/// </summary>
	/// <param name="title">The notification title displayed as a heading.</param>
	/// <param name="message">The body content of the notification.</param>
	/// <param name="type">The severity/category type of the notification. Defaults to <see cref="NotificationType.Info"/>.</param>
	/// <param name="actionUrl">An optional URL the user can navigate to from the notification.</param>
	/// <param name="actionText">The display text for the optional action link.</param>
	/// <returns>A new <see cref="Notification"/> instance that is unread and not dismissed.</returns>
	public static Notification Create(
		string title,
		string message,
		NotificationType type = NotificationType.Info,
		string? actionUrl = null,
		string? actionText = null) {
		return new Notification(
			Guid.NewGuid().ToString(),
			title,
			message,
			type,
			DateTime.UtcNow,
			false,
			false,
			actionUrl,
			actionText
		);
	}
}