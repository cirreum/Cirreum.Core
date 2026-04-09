namespace Cirreum.Conductor.Intercepts;

using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

sealed class HandlerPerformance<TOperation, TResponse>(
	ILogger<HandlerPerformance<TOperation, TResponse>> logger
) : IIntercept<TOperation, TResponse>
	where TOperation : notnull {

	private const int LongRunningThresholdMs = 500;

	public async Task<Result<TResponse>> HandleAsync(
		OperationContext<TOperation> context,
		OperationHandlerDelegate<TOperation, TResponse> next,
		CancellationToken cancellationToken) {

		var startTime = Timing.Start();
		try {
			return await next(context, cancellationToken).ConfigureAwait(false);
		} finally {
			var elapsedMs = (long)Math.Round(Timing.GetElapsedMilliseconds(startTime));
			if (elapsedMs > LongRunningThresholdMs) {
				logger.LogLongRunningOperation(context.OperationType, elapsedMs);
			}
		}

	}

}