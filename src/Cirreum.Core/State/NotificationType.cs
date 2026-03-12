namespace Cirreum.State;

/// <summary>
/// Defines the severity and visual category of a <see cref="Notification"/>.
/// </summary>
public enum NotificationType {
	/// <summary>General informational notification with no implied urgency.</summary>
	Info,

	/// <summary>Indicates a successfully completed operation or positive outcome.</summary>
	Success,

	/// <summary>Indicates a condition that may require attention but is not critical.</summary>
	Warning,

	/// <summary>Indicates a failure or critical condition requiring immediate attention.</summary>
	Error
}