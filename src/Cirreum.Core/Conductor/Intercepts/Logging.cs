namespace Cirreum.Conductor.Intercepts;

using Microsoft.Extensions.Logging;

internal static partial class Logging {

	[LoggerMessage(
		EventId = LoggingEventId.SkippingAuthorizingRequestId,
		Level = LogLevel.Debug,
		Message = "Skipped Authorizing Request '{RequestName}' as it does not require authorization.")]
	public static partial void LogSkippedAuthorizingRequest(
		this ILogger logger,
		string requestName);

	[LoggerMessage(
		SkipEnabledCheck = true,
		EventId = LoggingEventId.LongRunningRequestId,
		Level = LogLevel.Warning,
		Message = "Long running request {RequestName} took {ElapsedMilliseconds}ms")]
	public static partial void LogLongRunningRequest(
		this ILogger logger,
		string requestName,
		long elapsedMilliseconds);
}