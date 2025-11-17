namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

/// <summary>
/// Concrete wrapper implementation for requests with typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse>
	: RequestHandlerWrapper<TResponse>
	where TRequest : class, IRequest<TResponse> {

	private static readonly string requestTypeName = typeof(TRequest).Name;
	private static readonly string responseTypeName = typeof(TResponse).Name;

	public override async Task<Result<TResponse>> HandleAsync(
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		CancellationToken cancellationToken) {

		RequestContext<TRequest>? requestContext = null;
		Result<TResponse> finalResult = default!;

		// ----- 0. START ACTIVITY & TIMING -----
		var activity = RequestTelemetry.StartActivity(
			requestTypeName,
			hasResponse: true,
			responseTypeName);
		var startTimestamp = Timing.Start();

		try {

			// ----- 1. RESOLVE HANDLER -----
			var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
			if (handler is null) {
				finalResult = Result<TResponse>.Fail(new InvalidOperationException(
					$"No handler registered for request type '{requestTypeName}'"));
				return finalResult;
			}

			// ----- 2. BUILD PIPELINE -----
			var interceptArray = serviceProvider
				.GetServices<IIntercept<TRequest, TResponse>>()
				.ToArray();

			// ----- 3. EXECUTE HANDLER OR PIPELINE -----
			if (interceptArray.Length == 0) {
				// HOT PATH: No context needed - direct handler execution
				finalResult = await handler.HandleAsync((TRequest)request, cancellationToken);
			} else {
				// COLD PATH: Create context for pipeline
				requestContext = await CreateRequestContext(
					serviceProvider,
					activity,
					startTimestamp,
					(TRequest)request,
					requestTypeName);

				finalResult = await ExecutePipelineAsync(
					requestContext,
					handler,
					interceptArray,
					0,
					cancellationToken);
			}

			// ----- 4. GET ELAPSED TIME -----
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			// ----- 5. POST-PROCESSING (TELEMETRY) -----
			try {
				if (finalResult.IsSuccess) {
					RequestTelemetry.SetActivitySuccess(activity);
					RequestTelemetry.RecordSuccess(requestTypeName, responseTypeName, elapsed);
				} else {
					RequestTelemetry.SetActivityError(activity, finalResult.Error);
					RequestTelemetry.RecordFailure(requestTypeName, responseTypeName, elapsed, finalResult.Error);
				}
			} catch {
				// Telemetry failure shouldn't break the request
			} finally {
				RequestTelemetry.StopActivity(activity);
			}

		} catch (OperationCanceledException oce) {
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			RequestTelemetry.SetActivityCanceled(activity, oce);
			RequestTelemetry.RecordCanceled(requestTypeName, responseTypeName, elapsed, oce);
			RequestTelemetry.StopActivity(activity);

			finalResult = Result<TResponse>.Fail(oce);
			throw;

		} catch (Exception fex) when (fex is OutOfMemoryException || fex is ThreadAbortException) {
			// Fatal exceptions: stop activity but let them bubble
			RequestTelemetry.StopActivity(activity);

			finalResult = Result<TResponse>.Fail(fex);
			throw;

		} catch (Exception ex) {
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			RequestTelemetry.SetActivityError(activity, ex);
			RequestTelemetry.RecordFailure(requestTypeName, responseTypeName, elapsed, ex);
			RequestTelemetry.StopActivity(activity);

			finalResult = Result<TResponse>.Fail(ex);

		} finally {

			// ----- 6. AUDIT NOTIFICATION (IF NEEDED) -----
			if (request is IAuditableRequestBase) {
				try {
					// ONLY create context if auditing is needed
					requestContext ??= await CreateRequestContext(
						serviceProvider,
						activity,
						startTimestamp,
						(TRequest)request,
						requestTypeName);

					var notification = RequestCompletedNotification
						.FromResult(finalResult, requestContext);

					// Publish notification - fire-and-forget
					await publisher
						.PublishAsync(
							notification,
							PublisherStrategy.FireAndForget,
							CancellationToken.None)
						.ConfigureAwait(false);

				} catch {
					// Audit failure shouldn't break the request
				}
			}

		}

		// ----- 7. RETURN FINAL RESULT -----
		return finalResult;

	}

	private static async Task<RequestContext<TRequest>> CreateRequestContext(
		IServiceProvider serviceProvider,
		Activity? activity,
		long startTimestamp,
		TRequest typedRequest,
		string requestTypeName) {

		var userState = await serviceProvider
			.GetRequiredService<IUserStateAccessor>()
			.GetUser();

		var requestId = activity?.SpanId.ToString()
			?? Guid.NewGuid().ToString("N")[..16];
		var correlationId = activity?.TraceId.ToString()
			?? Guid.NewGuid().ToString("N");

		return RequestContext<TRequest>.Create(
			userState,
			typedRequest,
			requestTypeName,
			requestId,
			correlationId,
			startTimestamp);
	}

	private static Task<Result<TResponse>> ExecutePipelineAsync(
		RequestContext<TRequest> context,
		IRequestHandler<TRequest, TResponse> handler,
		IIntercept<TRequest, TResponse>[] intercepts,
		int index,
		CancellationToken cancellationToken) {

		if (index >= intercepts.Length) {
			return handler.HandleAsync(context.Request, cancellationToken);
		}

		var current = intercepts[index];

		return current.HandleAsync(
			context,
			(ctx, ct) => ExecutePipelineAsync(ctx, handler, intercepts, index + 1, ct),
			cancellationToken);

	}

}