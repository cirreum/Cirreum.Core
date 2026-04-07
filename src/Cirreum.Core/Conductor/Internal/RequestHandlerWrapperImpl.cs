namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Concrete wrapper implementation for requests without typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest>
	: RequestHandlerWrapper
	where TRequest : class, IRequest {

	private static readonly string requestTypeName = typeof(TRequest).Name;

	public override Task<Result> HandleAsync(
		IRequest request,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken) {

		// ----- 1. RESOLVE HANDLER -----
		var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
		if (handler is null) {
			return Task.FromResult(Result.Fail(new InvalidOperationException(
				$"No handler registered for request type '{requestTypeName}'")));
		}

		// ----- 2. RESOLVE INTERCEPTS -----
		var intercepts = serviceProvider.GetServices<IIntercept<TRequest, Unit>>();

		// ----- 3. FAST PATH: zero intercepts — no telemetry, no context, no cursor, no async -----
		if (intercepts is ICollection<IIntercept<TRequest, Unit>> { Count: 0 }) {
			try {
				return handler.HandleAsync(Unsafe.As<TRequest>(request), cancellationToken);
			} catch (Exception ex) when (!ex.IsFatal()) {
				return Task.FromResult(Result.Fail(ex));
			}
		}

		// ----- 4. PIPELINE PATH: intercepts present — full telemetry + context -----
		return RequestHandlerWrapperImpl<TRequest>.HandleWithPipelineAsync(request, serviceProvider, handler, intercepts, cancellationToken);
	}

	/// <summary>
	/// Pipeline path: intercepts are present. This method carries the full async state machine,
	/// telemetry, context creation, and exception handling — none of which is paid on the fast
	/// (zero-intercept) path above.
	/// </summary>
	private static async Task<Result> HandleWithPipelineAsync(
		IRequest request,
		IServiceProvider serviceProvider,
		IRequestHandler<TRequest> handler,
		IEnumerable<IIntercept<TRequest, Unit>> intercepts,
		CancellationToken cancellationToken) {

		// ----- 0. START ACTIVITY & TIMING -----
		using var activity = RequestTelemetry.StartActivity(
			requestTypeName,
			hasResponse: false);

		var startTimestamp = activity is not null ? Timing.Start() : 0L;

		try {

			// Materialize array (cast if DI already returned one) and walk
			// the pipeline via a single-alloc cursor.
			var interceptArray = intercepts as IIntercept<TRequest, Unit>[]
				?? [.. intercepts];

			var requestContext = await CreateRequestContext(
				serviceProvider,
				activity,
				startTimestamp,
				Unsafe.As<TRequest>(request),
				requestTypeName);

			var cursor = new PipelineCursor<TRequest>(interceptArray, handler);
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
			var finalResult = Result.Fail(ex);
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
			RequestTelemetry.RecordCanceled(requestTypeName, null, elapsed, (OperationCanceledException)error!);
		} else if (success) {
			RequestTelemetry.SetActivitySuccess(activity);
			RequestTelemetry.RecordSuccess(requestTypeName, null, elapsed);
		} else {
			RequestTelemetry.SetActivityError(activity, error!);
			RequestTelemetry.RecordFailure(requestTypeName, null, elapsed, error!);
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
