namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;
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

	public override Task<Result<TResponse>> HandleAsync(
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken) {

		// ----- 1. RESOLVE HANDLER -----
		var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
		if (handler is null) {
			return Task.FromResult(Result<TResponse>.Fail(new InvalidOperationException(
				$"No handler registered for request type '{requestTypeName}'")));
		}

		// ----- 2. RESOLVE INTERCEPTS -----
		var intercepts = serviceProvider.GetServices<IIntercept<TRequest, TResponse>>();

		// ----- 3. FAST PATH: zero intercepts — no telemetry, no context, no cursor, no async -----
		// Returns the handler's Task directly when possible: zero async state machine, zero
		// closure allocation, zero telemetry overhead. Handler exceptions are still caught
		// and converted to Result.Fail to preserve the dispatcher's "no-throw" contract.
		if (intercepts is ICollection<IIntercept<TRequest, TResponse>> { Count: 0 }) {
			try {
				return handler.HandleAsync(Unsafe.As<TRequest>(request), cancellationToken);
			} catch (Exception ex) when (!ex.IsFatal()) {
				return Task.FromResult(Result<TResponse>.Fail(ex));
			}
		}

		// ----- 4. PIPELINE PATH: intercepts present — full telemetry + context -----
		return RequestHandlerWrapperImpl<TRequest, TResponse>.HandleWithPipelineAsync(request, serviceProvider, handler, intercepts, cancellationToken);
	}

	/// <summary>
	/// Pipeline path: intercepts are present (Cirreum ships 4 by default: Validation,
	/// Authorization, HandlerPerformance, QueryCaching). This method carries the full
	/// async state machine, telemetry, context creation, and exception handling — none
	/// of which is paid on the fast (zero-intercept) path above.
	/// </summary>
	private static async Task<Result<TResponse>> HandleWithPipelineAsync(
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		IRequestHandler<TRequest, TResponse> handler,
		IEnumerable<IIntercept<TRequest, TResponse>> intercepts,
		CancellationToken cancellationToken) {

		// ----- 0. START ACTIVITY & TIMING -----
		using var activity = RequestTelemetry.StartActivity(
			requestTypeName,
			hasResponse: true,
			responseTypeName);

		var startTimestamp = Timing.Start();

		try {

			// Materialize array (cast if DI already returned one) and walk
			// the pipeline via a single-alloc cursor.
			var interceptArray = intercepts as IIntercept<TRequest, TResponse>[]
				?? [.. intercepts];

			var requestContext = await serviceProvider.CreateRequestContext(
				activity,
				startTimestamp,
				Unsafe.As<TRequest>(request),
				requestTypeName);

			var cursor = new PipelineCursor<TRequest, TResponse>(interceptArray, handler);
			var finalResult = await cursor.NextDelegate(requestContext, cancellationToken);

			// ----- POST-PROCESSING (TELEMETRY) -----
			RecordTelemetry(activity, startTimestamp, finalResult.IsSuccess, finalResult.Error);
			return finalResult;

		} catch (OperationCanceledException oce) {
			RecordTelemetry(activity, startTimestamp, success: false, error: oce, canceled: true);
			throw;

		} catch (Exception fex) when (fex.IsFatal()) {
			throw;

		} catch (Exception ex) {
			var finalResult = Result<TResponse>.Fail(ex);
			RecordTelemetry(activity, startTimestamp, finalResult.IsSuccess, finalResult.Error);
			return finalResult;
		}
	}

	private static void RecordTelemetry(
		Activity? activity,
		long startTimestamp,
		bool success,
		Exception? error = null,
		bool canceled = false) {

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

}
