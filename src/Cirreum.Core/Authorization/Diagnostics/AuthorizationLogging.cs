namespace Cirreum.Authorization.Diagnostics;

using Microsoft.Extensions.Logging;

internal static partial class AuthorizationLogging {

	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingResourceDeniedId,
		Level = LogLevel.Warning,
		Message = "User '{UserName}' was DENIED access to Resource '{ResourceName}'.\r\n{DeniedReason}")]
	public static partial void LogAuthorizingResourceDenied(
		this ILogger logger,
		string userName,
		string resourceName,
		string deniedReason);


	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingResourceUnknownErrorId,
		Level = LogLevel.Error,
		Message = "Exception encountered while authorizing User '{UserName}' for Resource '{ResourceName}'.\r\n{FailureReasons}")]
	public static partial void LogAuthorizingResourceUnknownError(
		this ILogger logger,
		Exception ex,
		string userName,
		string resourceName,
		string failureReasons);


	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingResourceAllowedId,
		Level = LogLevel.Information,
		Message = "User '{UserName}' was ALLOWED access to Resource '{ResourceName}'")]
	public static partial void LogAuthorizingResourceAllowed(
		this ILogger logger,
		string userName,
		string resourceName);
}
