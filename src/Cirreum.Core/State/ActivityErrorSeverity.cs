namespace Cirreum.State;

/// <summary>
/// Defines the severity of an <see cref="ActivityError"/>.
/// </summary>
/// <remarks>
/// Severity allows consumers to determine how an error should be
/// presented to the user or handled by the application.
/// </remarks>
public enum ActivityErrorSeverity {

	/// <summary>
	/// Indicates informational activity feedback that does not represent a failure.
	/// </summary>
	Info = 0,

	/// <summary>
	/// Indicates a recoverable issue that may impact functionality but does not
	/// prevent the activity from continuing.
	/// </summary>
	Warning = 1,

	/// <summary>
	/// Indicates a failure that prevented the activity from completing successfully.
	/// </summary>
	Error = 2
}