// RequestHandlerWrapperLogger.cs
namespace Cirreum.Conductor.Internal;

using Microsoft.Extensions.Logging;

internal static partial class RequestHandlerWrapperLogger {

	[LoggerMessage(EventId = 1000, Level = LogLevel.Debug,
		Message = "Dispatching request {RequestType}")]
	public static partial void DispatchingRequest(this ILogger logger, string requestType);

	[LoggerMessage(EventId = 1001, Level = LogLevel.Error,
		Message = "No handler registered for request type '{RequestType}'")]
	public static partial void NoHandlerRegistered(this ILogger logger, string requestType);

	[LoggerMessage(EventId = 1002, Level = LogLevel.Debug,
		Message = "Executing pipeline with {InterceptCount} intercepts for {RequestType}")]
	public static partial void ExecutingPipeline(this ILogger logger, int interceptCount, string requestType);

}