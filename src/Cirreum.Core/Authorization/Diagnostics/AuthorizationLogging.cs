namespace Cirreum.Authorization.Diagnostics;

using Microsoft.Extensions.Logging;

internal static partial class AuthorizationLogging {

	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingResourceDeniedId,
		Level = LogLevel.Warning,
		Message = "User '{UserName}' was DENIED access to Resource '{Resource}' with RequestId '{RequestId}'.\r\n{FailureReasons}")]
	public static partial void LogAuthorizingResourceDenied(
		this ILogger logger,
		string userName,
		string resource,
		string requestId,
		string failureReasons);


	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingResourceUnknownErrorId,
		Level = LogLevel.Error,
		Message = "User '{UserName}' was DENIED access to Resource '{Resource}' with RequestId '{RequestId}', due to an unknown/unhandled exception.")]
	public static partial void LogAuthorizingResourceUnknownError(
		this ILogger logger,
		Exception ex,
		string userName,
		string resource,
		string requestId);


	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingResourceAllowedId,
		Level = LogLevel.Information,
		Message = "User '{UserName}' was ALLOWED access to Resource '{Resource}' with RequestId '{RequestId}'")]
	public static partial void LogAuthorizingResourceAllowed(
		this ILogger logger,
		string userName,
		string resource,
		string requestId);


}