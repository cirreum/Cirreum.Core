namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
		CancellationToken cancellationToken) {

		// ----- 0. START ACTIVITY & TIMING -----
		using var activity = RequestTelemetry.StartActivity(
			requestTypeName,
			hasResponse: true,
			responseTypeName);

		var startTimestamp = activity is not null ? Timing.Start() : 0L;

		// Local function for recording telemetry
		void RecordTelemetry(bool success, Exception? error = null, bool canceled = false) {

			if (activity is null) {
				return;
			}

			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			if (canceled) {
				RequestTelemetry.SetActivityCanceled(activity, (OperationCanceledException)error!);
				RequestTelemetry.RecordCanceled(requestTypeName, responseTypeName, elapsed, (OperationCanceledException)error!);
			} else if (success) {
				RequestTelemetry.SetActivitySuccess(activity);
				RequestTelemetry.RecordSuccess(requestTypeName, responseTypeName, elapsed);
			} else {
				RequestTelemetry.SetActivityError(activity, error!);
				RequestTelemetry.RecordFailure(requestTypeName, responseTypeName, elapsed, error!);
			}
		}

		try {

			// ----- 1. RESOLVE HANDLER -----
			var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
			if (handler is null) {
				return Result<TResponse>.Fail(new InvalidOperationException(
					$"No handler registered for request type '{requestTypeName}'"));
			}

			// ----- 2. RESOLVE INTERCEPTS (deferred array materialization) -----
			var intercepts = serviceProvider.GetServices<IIntercept<TRequest, TResponse>>();

			// ----- 3. EXECUTE HANDLER OR PIPELINE -----
			Result<TResponse> finalResult;
			if (intercepts is ICollection<IIntercept<TRequest, TResponse>> { Count: 0 }) {
				// BYPASS PATH (rare): Zero intercepts registered — skip pipeline/context entirely
				// and invoke the handler directly. Unsafe.As avoids the isinst check — safe because
				// the dispatcher retrieves this wrapper from a cache keyed by typeof(TRequest).
				finalResult = await handler.HandleAsync(Unsafe.As<TRequest>(request), cancellationToken);
			} else {
				// TYPICAL PATH: Intercepts present (Cirreum ships 4 by default: Validation,
				// Authorization, HandlerPerformance, QueryCaching). Materialize array (cast if
				// DI already returned one) and walk the pipeline via a single-alloc cursor.
				var interceptArray = intercepts as IIntercept<TRequest, TResponse>[]
					?? [.. intercepts];

				var requestContext = await CreateRequestContext(
					serviceProvider,
					activity,
					startTimestamp,
					Unsafe.As<TRequest>(request),
					requestTypeName);

				var cursor = new PipelineCursor<TRequest, TResponse>(interceptArray, handler);
				finalResult = await cursor.NextDelegate(requestContext, cancellationToken);
			}

			// ----- 4. POST-PROCESSING (TELEMETRY) -----
			RecordTelemetry(finalResult.IsSuccess, finalResult.Error);
			return finalResult;

		} catch (OperationCanceledException oce) {
			RecordTelemetry(success: false, error: oce, canceled: true);
			throw;

		} catch (Exception fex) when (fex.IsFatal()) {
			throw;

		} catch (Exception ex) {
			var finalResult = Result<TResponse>.Fail(ex);
			RecordTelemetry(finalResult.IsSuccess, finalResult.Error);
			return finalResult;
		}
	}

	private static async Task<RequestContext<TRequest>> CreateRequestContext(
		IServiceProvider serviceProvider,
		Activity? activity,
		long startTimestamp,
		TRequest typedRequest,
		string requestTypeName) {

		// GetService<T>()! — IUserStateAccessor is registered by Cirreum bootstrap; skip
		// GetRequiredService's null-guard + throw-helper overhead on the hot path.
		var userState = await serviceProvider
			.GetService<IUserStateAccessor>()!
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

}
