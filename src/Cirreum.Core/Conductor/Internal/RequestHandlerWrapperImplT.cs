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

	// Cache the auditable check at type level - happens once per request type
	private static readonly bool _isAuditable = typeof(IAuditableRequestBase).IsAssignableFrom(typeof(TRequest));

	public override Task<Result<TResponse>> HandleAsync(
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		CancellationToken cancellationToken) {

		// Fast branch - determined at compile time per request type
		if (!_isAuditable) {
			return RequestHandlerWrapperImpl<TRequest, TResponse>.HandleNonAuditableAsync(
				(TRequest)request,
				serviceProvider,
				cancellationToken);
		}

		return RequestHandlerWrapperImpl<TRequest, TResponse>.HandleAuditableAsync(
			(TRequest)request,
			serviceProvider,
			publisher,
			cancellationToken);
	}

	private static async Task<Result<TResponse>> HandleNonAuditableAsync(
		TRequest request,
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

			// ----- 2. BUILD PIPELINE -----
			var interceptArray = serviceProvider
				.GetServices<IIntercept<TRequest, TResponse>>()
				.ToArray();

			// ----- 3. EXECUTE HANDLER OR PIPELINE -----
			Result<TResponse> finalResult;
			if (interceptArray.Length == 0) {
				// HOT PATH: No context needed - direct handler execution
				finalResult = await handler.HandleAsync((TRequest)request, cancellationToken);
			} else {
				// COLD PATH: Create context for pipeline
				var requestContext = await CreateRequestContext(
					serviceProvider,
					activity,
					startTimestamp,
					request,
					requestTypeName);

				finalResult = await ExecutePipelineAsync(
					requestContext,
					handler,
					interceptArray,
					0,
					cancellationToken);
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

	private static async Task<Result<TResponse>> HandleAuditableAsync(
		TRequest request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		CancellationToken cancellationToken) {

		RequestContext<TRequest>? requestContext = null;
		Result<TResponse> finalResult = default!;

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
					request,
					requestTypeName);

				finalResult = await ExecutePipelineAsync(
					requestContext,
					handler,
					interceptArray,
					0,
					cancellationToken);
			}

			// ----- 4. POST-PROCESSING (TELEMETRY) -----
			RecordTelemetry(finalResult.IsSuccess, finalResult.Error);

		} catch (OperationCanceledException oce) {
			finalResult = Result<TResponse>.Fail(oce);
			RecordTelemetry(success: false, error: oce, canceled: true);
			throw;

		} catch (Exception fex) when (fex.IsFatal()) {
			// Fatal exceptions: stop activity but let them bubble
			finalResult = Result<TResponse>.Fail(fex);
			throw;

		} catch (Exception ex) {
			finalResult = Result<TResponse>.Fail(ex);
			RecordTelemetry(finalResult.IsSuccess, finalResult.Error);
		} finally {

			// ----- 6. AUDIT NOTIFICATION (IF NEEDED) -----
			try {
				// ONLY create context if auditing is needed
				requestContext ??= await CreateRequestContext(
					serviceProvider,
					activity,
					startTimestamp,
					request,
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