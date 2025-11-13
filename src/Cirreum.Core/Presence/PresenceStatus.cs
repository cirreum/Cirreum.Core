namespace Cirreum.Presence;

/// <summary>
/// Represents the presence status of a user.
/// </summary>
public enum PresenceStatus {
	/// <summary>
	/// Indicates that the user is busy and may not be immediately available.
	/// </summary>
	Busy,

	/// <summary>
	/// Indicates that the user is out of the office and may have limited availability.
	/// </summary>
	OutOfOffice,

	/// <summary>
	/// Indicates that the user is temporarily away from their desk or device.
	/// </summary>
	Away,

	/// <summary>
	/// Indicates that the user is available and ready for communication.
	/// </summary>
	Available,

	/// <summary>
	/// Indicates that the user is offline or not connected to the system.
	/// </summary>
	Offline,

	/// <summary>
	/// Indicates that the user does not want to be disturbed and should only be contacted for urgent matters.
	/// </summary>
	DoNotDisturb,

	/// <summary>
	/// Indicates that the user's status is unknown or cannot be determined.
	/// </summary>
	Unknown
}