// MessageScannerLogger.cs
namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System;

internal static partial class MessageScannerLogger {

	[LoggerMessage(EventId = 2000, Level = LogLevel.Debug,
		Message = "Discovered distributed message type: {MessageType}")]
	public static partial void DiscoveredMessage(this ILogger logger, string messageType);

	[LoggerMessage(EventId = 2001, Level = LogLevel.Warning,
		Message = "Duplicate message definition detected: {Type} uses identifier '{Identifier}' with version {Version} which is already in use")]
	public static partial void DuplicateDetected(this ILogger logger, string type, string identifier, string version);

	[LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
		Message = "Found {DuplicateCount} duplicate message definitions. This may cause issues with message routing and versioning.")]
	public static partial void DuplicateSummary(this ILogger logger, int duplicateCount);

	[LoggerMessage(EventId = 2003, Level = LogLevel.Debug,
		Message = "Completed scanning. Discovered '{MessageCount}' distributed messages")]
	public static partial void CompletedScanning(this ILogger logger, int messageCount);

	[LoggerMessage(EventId = 2004, Level = LogLevel.Error,
		Message = "Missing Member Exception: {Message}")]
	public static partial void MissingMember(this ILogger logger, string message, Exception ex);

	[LoggerMessage(EventId = 2005, Level = LogLevel.Error,
		Message = "Error accessing DistributedMessage property in {Type}: {InnerMessage}")]
	public static partial void TargetInvocation(this ILogger logger, string type, string? innerMessage, Exception ex);

	[LoggerMessage(EventId = 2006, Level = LogLevel.Error,
		Message = "Unexpected error scanning type {Type}")]
	public static partial void UnexpectedError(this ILogger logger, string type, Exception ex);

}