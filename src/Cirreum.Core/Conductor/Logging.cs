namespace Cirreum.Conductor;

using Microsoft.Extensions.Logging;

internal static partial class Logging {

	/// <summary>
	/// Logs a failed Result with correlation tracking.
	/// </summary>
	/// <remarks>
	/// The error reference ID (requestId) can be provided to support teams for troubleshooting.
	/// </remarks>
	[LoggerMessage(
		EventId = LoggingEventId.ResultFailureId,
		Level = LogLevel.Warning,
		Message = "Request '{RequestType}' failed with RequestId '{RequestId}' and CorrelationId: {CorrelationId}]. Exception: {ExceptionType} - {ExceptionMessage}")]
	public static partial void LogResultFailure(
		this ILogger logger,
		string requestType,
		string requestId,
		string correlationId,
		string exceptionType,
		string exceptionMessage);

	[LoggerMessage(
		EventId = LoggingEventId.SkippingAuthorizingId,
		Level = LogLevel.Debug,
		Message = "Skipped Authorizing Request '{RequestName}' as it does not require authorization.")]
	public static partial void LogSkippedAuthorizingRequest(
		this ILogger logger,
		string requestName);

	[LoggerMessage(
		SkipEnabledCheck = true,
		EventId = LoggingEventId.LongRunningId,
		Level = LogLevel.Warning,
		Message = "Long running request {RequestName} took {ElapsedMilliseconds}ms")]
	public static partial void LogLongRunningRequest(
		this ILogger logger,
		string requestName,
		long elapsedMilliseconds);

	[LoggerMessage(
		EventId = LoggingEventId.RecordTelemetryFailedId,
		Level = LogLevel.Error,
		Message = "Exception encountered recording telemetry.")]
	public static partial void LogRecordTelemetryFailed(
		this ILogger logger,
		Exception ex);

	[LoggerMessage(
		EventId = LoggingEventId.AuditLoggingFailedId,
		Level = LogLevel.Error,
		Message = "Exception enountered logging audit entry")]
	public static partial void LogAuditLoggingFailed(
		this ILogger logger,
		Exception ex);

}