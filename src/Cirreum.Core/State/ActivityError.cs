namespace Cirreum.State;

/// <summary>
/// Represents an error that occurred during tracked application activity.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ActivityError"/> captures both diagnostic information and a
/// user-friendly message describing the failure. The original
/// <see cref="Exception"/> is preserved for diagnostics, while
/// <see cref="DisplayMessage"/> can be safely shown in the UI.
/// </para>
/// <para>
/// Errors may occur during application initialization via
/// <see cref="IInitializationOrchestrator"/> or during other activity cycles
/// throughout the lifetime of the user session.
/// </para>
/// </remarks>
/// <param name="SourceName">
/// The display name of the component, service, or store that produced the error.
/// </param>
/// <param name="Severity">
/// The severity level of the error.
/// </param>
/// <param name="Exception">
/// The exception that was thrown during the activity operation.
/// </param>
/// <param name="DisplayMessage">
/// A user-friendly message suitable for display in the UI.
/// </param>
/// <param name="ErrorMessage">
/// The diagnostic message extracted from the exception.
/// </param>
/// <param name="StackTrace">
/// The stack trace from the exception, if available.
/// </param>
/// <param name="Timestamp">
/// The UTC time when the error occurred.
/// </param>
public sealed record ActivityError(
	string SourceName,
	ActivityErrorSeverity Severity,
	Exception Exception,
	string DisplayMessage,
	string ErrorMessage,
	string? StackTrace,
	DateTime Timestamp
) {

	/// <summary>
	/// Creates a new <see cref="ActivityError"/> from an exception.
	/// </summary>
	/// <param name="sourceName">
	/// The display name of the component, service, or store that failed.
	/// </param>
	/// <param name="exception">
	/// The exception that occurred.
	/// </param>
	/// <param name="displayMessage">
	/// Optional user-friendly message. If not provided, a generic message is used.
	/// </param>
	/// <param name="severity">
	/// The severity of the error. Defaults to <see cref="ActivityErrorSeverity.Error"/>.
	/// </param>
	public static ActivityError FromException(
		string sourceName,
		Exception exception,
		string? displayMessage = null,
		ActivityErrorSeverity severity = ActivityErrorSeverity.Error) =>
		new(
			SourceName: sourceName,
			Severity: severity,
			Exception: exception,
			DisplayMessage: displayMessage ?? "An unexpected error occurred.",
			ErrorMessage: exception.Message,
			StackTrace: exception.StackTrace,
			Timestamp: DateTime.UtcNow
		);
}