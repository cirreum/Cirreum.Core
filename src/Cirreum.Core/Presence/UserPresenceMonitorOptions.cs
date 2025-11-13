namespace Cirreum.Presence;

/// <summary>
/// Configuration options for the user presence monitoring system.
/// </summary>
public class UserPresenceMonitorOptions {

	/// <summary>
	/// The default refresh interval value (1 minute).
	/// </summary>
	public const int DefaultRefreshInterval = 60_000;

	/// <summary>
	/// Gets or sets the interval in milliseconds between presence updates.
	/// </summary>
	/// <remarks>
	/// This value determines how frequently the presence monitor will check and update the user's presence status.
	/// A smaller interval provides more real-time presence information but increases server load.
	/// A larger interval reduces server load but may result in less accurate presence information.
	/// </remarks>
	/// <value>
	/// The refresh interval in milliseconds. Default value is <see cref="DefaultRefreshInterval"/>. A
	/// value of 5 seconds or less is considered invalid, and will fall back to <see cref="DefaultRefreshInterval"/>.
	/// A value of 0 (zero) will disable the monitor.
	/// </value>
	public int RefreshInterval { get; set; } = DefaultRefreshInterval;

}