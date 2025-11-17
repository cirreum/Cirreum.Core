namespace Cirreum.Conductor.Intercepts;

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public sealed class HandlerPerformance<TRequest, TResponse>(
	ILogger<HandlerPerformance<TRequest, TResponse>> logger
) : IIntercept<TRequest, TResponse>
	where TRequest : notnull {

	private const int LongRunningThresholdMs = 500;

	public async Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken) {

		var startTime = Stopwatch.GetTimestamp();
		try {
			return await next(context, cancellationToken).ConfigureAwait(false);
		} finally {
			var elapsedMs = (long)Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
			if (elapsedMs > LongRunningThresholdMs) {
				logger.LogLongRunningRequest(context.RequestType, elapsedMs);
			}
		}

	}

}