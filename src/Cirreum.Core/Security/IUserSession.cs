namespace Cirreum.Security;

/// <summary>
/// Defines the contract for managing user session state and activity tracking.
/// Provides functionality to monitor session duration, track user activity, 
/// and determine session expiration based on configurable timeout periods.
/// </summary>
public interface IUserSession : IApplicationState {

	/// <summary>
	/// Gets the timestamp when the current user session was started (in UTC).
	/// </summary>
	/// <value>
	/// The session start time, or <see langword="null"/> if no session is active.
	/// </value>
	DateTimeOffset? SessionStartTime { get; }

	/// <summary>
	/// Gets the timestamp of the user's last recorded activity within the session (in UTC).
	/// </summary>
	/// <value>
	/// The last activity timestamp, or <see langword="null"/> if no session is active.
	/// </value>
	DateTimeOffset? LastActivityTime { get; }

	/// <summary>
	/// Records the current time as the user's last activity timestamp.
	/// </summary>
	/// <remarks>
	/// This method should be called whenever user activity is detected (e.g., API calls, 
	/// navigation, UI interactions) to prevent premature session timeout.
	/// </remarks>
	void UpdateActivity();

	/// <summary>
	/// Determines whether the current session has expired based on inactivity duration.
	/// </summary>
	/// <param name="timeout">
	/// The maximum duration of inactivity before the session is considered expired.
	/// Must be a positive <see cref="TimeSpan"/> value.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the session has expired or no session is active; 
	/// otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="timeout"/> is zero or negative.
	/// </exception>
	bool IsSessionExpired(TimeSpan timeout);

}