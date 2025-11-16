namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

/// <summary>
/// Concrete wrapper implementation for requests without typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
	where TRequest : class, IRequest {

	public override async Task<Result> HandleAsync(
		IDomainEnvironment domainEnvironment,
		IRequest request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		ILogger logger,
		CancellationToken cancellationToken) {

		RequestContext<TRequest>? requestContext = null;
		Result finalResult;
		var typedRequest = (TRequest)request;
		var requestTypeName = typeof(TRequest).Name;
		var responseTypeName = (string)null!;
		logger.DispatchingRequest(requestTypeName);

		// ----- 0. START ACTIVITY -----
		var (activity, stopwatch) =
			RequestTelemetry.StartActivityAndStopwatch(
				domainEnvironment,
				requestTypeName,
				hasResponse: true,
				responseTypeName);

		try {

			// ----- 1. RESOLVE HANDLER -----
			var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
			if (handler is null) {
				logger.NoHandlerRegistered(requestTypeName);
				return Result.Fail(new InvalidOperationException(
					$"No handler registered for request type '{requestTypeName}'"));
			}

			// ----- 2. BUILD PIPELINE -----
			var intercepts = serviceProvider.GetServices<IIntercept<TRequest, Unit>>();

			// ----- 3a. EXECUTE HANDLER OR PIPELINE -----
			if (!intercepts.Any()) {
				finalResult = await handler.HandleAsync(typedRequest, cancellationToken);
			} else {
				requestContext = await CreateRequestContext(
					domainEnvironment,
					serviceProvider,
					activity,
					stopwatch,
					typedRequest,
					requestTypeName);
				if (logger.IsEnabled(LogLevel.Debug)) {
					logger.ExecutingPipeline(intercepts.Count(), requestTypeName);
				}
				finalResult = await ExecutePipelineAsync(
					requestContext,
					handler,
					[.. intercepts],
					0,
					cancellationToken);
			}

			// ----- 3b. EXECUTION FINISHED -----
			stopwatch.Stop();
			if (activity is not null) {
				if (activity.Duration == TimeSpan.Zero) {
					activity.SetEndTime(DateTime.UtcNow);
				}
				activity.Stop();
			}

			// ----- 4. POST-PROCESSING (TELEMETRY + AUDIT) -----
			// At this point, `finalResult` is the truth. 
			// This block must NOT overwrite it.
			finalResult.Switch(
				onSuccess: () => {
					RequestTelemetry.HandleSuccess(
							requestTypeName,
							responseTypeName,
							stopwatch.Elapsed.TotalMilliseconds,
							activity);
				},
				onFailure: err => {
					RequestTelemetry.HandleFailure(
						 logger,
						 requestTypeName,
						 responseTypeName,
						 stopwatch.Elapsed.TotalMilliseconds,
						 activity,
						 err);
				},
				onCallbackError: unhandledException => {
					logger.LogRecordTelemetryFailed(unhandledException);
				});

			if (request is IAuditableRequestBase) {

				requestContext ??= await CreateRequestContext(
						domainEnvironment,
						serviceProvider,
						activity,
						stopwatch,
						typedRequest,
						requestTypeName);

				var notification = RequestCompletedNotification
						.FromResult(finalResult, requestContext);

				// Publish notification - fire-and-forget
				try {
					await publisher.PublishAsync(
						notification,
						PublisherStrategy.FireAndForget,
						CancellationToken.None);
				} catch (Exception ex) {
					logger.LogAuditLoggingFailed(ex);
				}
			}

		} catch (OperationCanceledException oce) {
			stopwatch.Stop();

			RequestTelemetry.HandleCanceled(
				requestTypeName,
				responseTypeName,
				stopwatch.Elapsed.TotalMilliseconds,
				activity,
				oce);

			// cancellation -> rethrow to preserve stack trace
			throw;

		} catch (Exception ex) {
			stopwatch.Stop();

			RequestTelemetry.HandleFailure(
				logger,
				requestTypeName,
				responseTypeName,
				stopwatch.Elapsed.TotalMilliseconds,
				activity,
				ex);

			// handler/pipeline failure -> THIS is a real request failure
			finalResult = Result.Fail(ex);

		} finally {

			if (activity is not null) {
				if (activity.Duration == TimeSpan.Zero) {
					activity.SetEndTime(DateTime.UtcNow);
				}
				activity.Stop();
			}

		}

		// ----- 5. RETURN FINAL RESULT -----
		return finalResult;

	}

	private static async Task<RequestContext<TRequest>> CreateRequestContext(
		IDomainEnvironment domainEnvironment,
		IServiceProvider serviceProvider,
		Activity? activity,
		Stopwatch stopwatch,
		TRequest typedRequest,
		string requestTypeName) {

		var userState = await serviceProvider.GetRequiredService<IUserStateAccessor>().GetUser();
		var requestId = activity?.SpanId.ToString() ?? Guid.NewGuid().ToString("N")[..16];
		var correlationId = activity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
		return RequestContext<TRequest>.Create(
			domainEnvironment,
			stopwatch,
			userState,
			typedRequest,
			requestTypeName,
			requestId,
			correlationId
		);
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