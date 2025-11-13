namespace Cirreum.Presence;

public interface IUserPresenceService {
	/// <summary>
	/// Updates the user's presence.
	/// </summary>
	Task UpdateUserPresence();
	/// <summary>
	/// Gets if the service is enabled
	/// </summary>
	bool IsEnabled { get; }
}