namespace Cirreum.Conductor.Internal;

using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Concrete wrapper implementation for requests with typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse>
  : RequestHandlerWrapper<TResponse>
	where TRequest : class, IRequest<TResponse> {

	public override async ValueTask<Result<TResponse>> Handle(
		string environment,
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		ILogger logger,
		CancellationToken cancellationToken) {

		var typedRequest = (TRequest)request;
		var requestTypeName = typeof(TRequest).Name;
		var responseTypeName = typeof(TResponse).Name;

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

			// Resolve the handler
			var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
			if (handler is null) {
				logger.NoHandlerRegistered(requestTypeName);
				return Result<TResponse>.Fail(new InvalidOperationException(
					$"No handler registered for request type '{requestTypeName}'"));
			}
			var handlerTypeName = handler.GetType().Name;

			// Get all intercepts for this request type (using Unit as TResponse)	
			var intercepts = serviceProvider
				.GetServices<IIntercept<TRequest, TResponse>>()
				.ToList();

			Result<TResponse> finalResult;

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
					RequestTelemetry.HandleSuccess(requestTypeName, responseTypeName, stopwatch.Elapsed.TotalMilliseconds, activity);
				},
				onFailure: error => {
					RequestTelemetry.HandleFailure(logger, requestTypeName, responseTypeName, stopwatch.Elapsed.TotalMilliseconds, activity, error);
				}
			);

			await RequestAuditor.AuditRequestIfRequired(publisher, finalResult, requestContext, logger);

			return finalResult;

		} catch (OperationCanceledException oce) {
			stopwatch.Stop();
			RequestTelemetry.RecordMetrics(requestTypeName, responseTypeName, false, stopwatch.Elapsed.TotalMilliseconds, oce.GetType().Name);
			RequestTelemetry.HandleException(activity, oce);
			throw;
		} catch (Exception ex) {
			stopwatch.Stop();
			RequestTelemetry.RecordMetrics(requestTypeName, responseTypeName, false, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
			RequestTelemetry.HandleException(activity, ex);
			return Result<TResponse>.Fail(ex);
		} finally {
			if (activity is not null) {
				if (activity.Duration == TimeSpan.Zero) {
					activity.SetEndTime(DateTime.UtcNow);
				}
				activity.Stop();
			}
		}

	}

	private static async ValueTask<Result<TResponse>> ExecutePipelineAsync(
		RequestContext<TRequest> context,
		IRequestHandler<TRequest, TResponse> handler,
		List<IIntercept<TRequest, TResponse>> intercepts,
		int index,
		CancellationToken cancellationToken) {

		if (index >= intercepts.Count) {
			// Base case: execute the actual handler
			return await handler.HandleAsync(context.Request, cancellationToken);
		}

		// Recursive case: execute current intercept with continuation
		var currentIntercept = intercepts[index];
		return await currentIntercept.HandleAsync(
			context,
			ct => ExecutePipelineAsync(context, handler, intercepts, index + 1, ct),
			cancellationToken);
	}

}