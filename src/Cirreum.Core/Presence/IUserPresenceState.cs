namespace Cirreum.Presence;

using Cirreum;

public interface IUserPresenceState : IScopedNotificationState {
	/// <summary>
	/// Gets the current <see cref="UserPresence"/>.
	/// </summary>
	UserPresence Presence { get; }
	/// <summary>
	/// Sets the current users presence.
	/// </summary>
	/// <param name="presence">The <see cref="UserPresence"/> to set.</param>
	void SetPresence(UserPresence presence);
}