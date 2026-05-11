namespace Cirreum.Authorization.Diagnostics;

using Microsoft.Extensions.Logging;

internal static partial class AuthorizationLogging {

	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingDeniedId,
		Level = LogLevel.Warning,
		Message = "User '{UserName}' was DENIED access to '{ObjectName}'.{DelegationSuffix}\r\n{DeniedReason}")]
	public static partial void LogAuthorizingDenied(
		this ILogger logger,
		string userName,
		string objectName,
		string deniedReason,
		string? delegationSuffix = null);


	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingUnknownErrorId,
		Level = LogLevel.Error,
		Message = "Exception encountered while authorizing User '{UserName}' for '{ObjectName}'.{DelegationSuffix}\r\n{FailureReasons}")]
	public static partial void LogAuthorizingUnknownError(
		this ILogger logger,
		Exception ex,
		string userName,
		string objectName,
		string failureReasons,
		string? delegationSuffix = null);


	[LoggerMessage(
		EventId = AuthorizationLogEventId.AuthorizingAllowedId,
		Level = LogLevel.Information,
		Message = "User '{UserName}' was ALLOWED access to '{ObjectName}'.{DelegationSuffix}")]
	public static partial void LogAuthorizingAllowed(
		this ILogger logger,
		string userName,
		string objectName,
		string? delegationSuffix = null);

}
