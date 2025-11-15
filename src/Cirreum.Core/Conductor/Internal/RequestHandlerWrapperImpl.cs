namespace Cirreum.Conductor.Internal;

using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

/// <summary>
/// Concrete wrapper implementation for requests without typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
	where TRequest : class, IRequest {

	public override async ValueTask<Result> Handle(
		string environment,
		IRequest request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		ILogger logger,
		CancellationToken cancellationToken) {

		var typedRequest = (TRequest)request;
		var requestTypeName = typeof(TRequest).Name;

		logger.DispatchingRequest(requestTypeName);

		var (activity, stopwatch) =
			RequestTelemetry.StartActivityAndStopwatch(
				requestTypeName,
				hasResponse: false);

		try {

			// Create the request context
			var userState = await serviceProvider.GetRequiredService<IUserStateAccessor>().GetUser();
			var requestId = activity?.SpanId.ToString() ?? Guid.NewGuid().ToString("N")[..16];
			var correlationId = activity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
			var requestContext = RequestContext<TRequest>.Create(
				environment,
				stopwatch,
				userState,
				typedRequest,
				requestTypeName,
				requestId,
				correlationId
			);

			var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
			if (handler is null) {
				logger.NoHandlerRegistered(requestTypeName);
				return Result.Fail(new InvalidOperationException(
					$"No handler registered for request type '{requestTypeName}'"));
			}
			var handlerTypeName = handler.GetType().Name;

			// Get all intercepts for this request type (using Unit as TResponse)
			var intercepts = serviceProvider
				.GetServices<IIntercept<TRequest, Unit>>()
				.ToList();

			Result<Unit> finalResult;

			if (intercepts.Count == 0) {
				finalResult = await handler.HandleAsync(typedRequest, cancellationToken);
			} else {
				logger.ExecutingPipeline(intercepts.Count, requestTypeName);
				finalResult = await ExecutePipelineAsync(
					requestContext,
					handler,
					intercepts,
					0,
					cancellationToken);
			}

			stopwatch.Stop();

			activity?.SetEndTime(DateTime.UtcNow);

			finalResult.Switch(
				onSuccess: _ => {
					RequestTelemetry.HandleSuccess(requestTypeName, null, stopwatch.Elapsed.TotalMilliseconds, activity);
				},
				onFailure: error => {
					RequestTelemetry.HandleFailure(logger, requestTypeName, null, stopwatch.Elapsed.TotalMilliseconds, activity, error);
				}
			);

			await RequestAuditor.AuditRequestIfRequired(publisher, finalResult, requestContext, logger);

			return finalResult;

		} catch (OperationCanceledException oce) {
			stopwatch.Stop();
			RequestTelemetry.RecordMetrics(requestTypeName, null, false, stopwatch.Elapsed.TotalMilliseconds, oce.GetType().Name);
			RequestTelemetry.HandleException(activity, oce);
			throw;
		} catch (Exception ex) {
			stopwatch.Stop();
			RequestTelemetry.RecordMetrics(requestTypeName, null, false, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
			RequestTelemetry.HandleException(activity, ex);
			return Result.Fail(ex);
		} finally {
			if (activity is not null) {
				if (activity.Duration == TimeSpan.Zero) {
					activity.SetEndTime(DateTime.UtcNow);
				}
				activity.Stop();
			}
		}

	}

	private static async ValueTask<Result<Unit>> ExecutePipelineAsync(
		RequestContext<TRequest> context,
		IRequestHandler<TRequest> handler,
		List<IIntercept<TRequest, Unit>> intercepts,
		int index,
		CancellationToken cancellationToken) {

		if (index >= intercepts.Count) {
			// Base case: execute the actual handler and convert to Result<Unit>
			var result = await handler.HandleAsync(context.Request, cancellationToken);
			return result; // Implicit conversion from Result to Result<Unit>
		}

		// Recursive case: execute current intercept with continuation
		var currentIntercept = intercepts[index];
		return await currentIntercept.HandleAsync(
			context,
			ct => ExecutePipelineAsync(context, handler, intercepts, index + 1, ct),
			cancellationToken);
	}

}