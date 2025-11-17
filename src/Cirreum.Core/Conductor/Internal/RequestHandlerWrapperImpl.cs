namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

/// <summary>
/// Concrete wrapper implementation for requests without typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest>
	: RequestHandlerWrapper
	where TRequest : class, IRequest {

	private static readonly string requestTypeName = typeof(TRequest).Name;

	public override async Task<Result> HandleAsync(
		IRequest request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		CancellationToken cancellationToken) {

		RequestContext<TRequest>? requestContext = null;
		Result finalResult = default!;

		// ----- 0. START ACTIVITY & TIMING -----
		var activity = RequestTelemetry.StartActivity(requestTypeName, hasResponse: false);
		var startTimestamp = Timing.Start();

		try {

			// ----- 1. RESOLVE HANDLER -----
			var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
			if (handler is null) {
				finalResult = Result.Fail(new InvalidOperationException(
					$"No handler registered for request type '{requestTypeName}'"));
				return finalResult;
			}

			// ----- 2. BUILD PIPELINE -----
			var interceptArray = serviceProvider
				.GetServices<IIntercept<TRequest, Unit>>()
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
					RequestTelemetry.RecordSuccess(requestTypeName, null, elapsed);
				} else {
					RequestTelemetry.SetActivityError(activity, finalResult.Error);
					RequestTelemetry.RecordFailure(requestTypeName, null, elapsed, finalResult.Error);
				}
			} catch {
				// Telemetry failure shouldn't break the request
			} finally {
				RequestTelemetry.StopActivity(activity);
			}

		} catch (OperationCanceledException oce) {
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			RequestTelemetry.SetActivityCanceled(activity, oce);
			RequestTelemetry.RecordCanceled(requestTypeName, null, elapsed, oce);
			RequestTelemetry.StopActivity(activity);

			finalResult = Result.Fail(oce);
			throw;

		} catch (Exception fex) when (fex is OutOfMemoryException || fex is ThreadAbortException) {
			// Fatal exceptions: stop activity but let them bubble
			RequestTelemetry.StopActivity(activity);

			finalResult = Result.Fail(fex);
			throw;

		} catch (Exception ex) {
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			RequestTelemetry.SetActivityError(activity, ex);
			RequestTelemetry.RecordFailure(requestTypeName, null, elapsed, ex);
			RequestTelemetry.StopActivity(activity);

			finalResult = Result.Fail(ex);

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

	private static async Task<Result<Unit>> ExecutePipelineAsync(
		RequestContext<TRequest> context,
		IRequestHandler<TRequest> handler,
		IIntercept<TRequest, Unit>[] intercepts,
		int index,
		CancellationToken cancellationToken) {

		if (index >= intercepts.Length) {
			var result = await handler.HandleAsync(context.Request, cancellationToken);
			return result; // Implicit conversion from Result to Result<Unit>
		}

		var current = intercepts[index];

		return await current.HandleAsync(
			context,
			(ctx, ct) => ExecutePipelineAsync(ctx, handler, intercepts, index + 1, ct),
			cancellationToken);
	}

}