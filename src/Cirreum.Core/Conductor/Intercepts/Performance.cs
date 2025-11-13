namespace Cirreum.Conductor.Intercepts;

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public sealed class Performance<TRequest, TResponse>(
	ILogger<Performance<TRequest, TResponse>> logger
) : IIntercept<TRequest, TResponse>
	where TRequest : notnull {

	private const int LongRunningThresholdMs = 500;

	public async ValueTask<Result<TResponse>> HandleAsync(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken) {

		var sw = Stopwatch.StartNew();
		try {
			return await next(cancellationToken);
		} finally {
			sw.Stop();
			var elapsedMs = sw.ElapsedMilliseconds;

			if (elapsedMs > LongRunningThresholdMs) {
				var requestName = request.GetType().Name;
				logger.LogLongRunningRequest(requestName, elapsedMs);
			}
		}

	}

}