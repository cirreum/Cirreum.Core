namespace Cirreum.Conductor;

using Cirreum.Conductor.Internal;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/> that routes requests to their handlers
/// through a pipeline of intercepts.
/// </summary>
/// <remarks>
/// This dispatcher uses a wrapper-based caching strategy to avoid reflection overhead in the hot path.
/// Request type wrappers are created once and cached for the lifetime of the application.
/// </remarks>
public sealed class Dispatcher(
	IServiceProvider serviceProvider,
	ILogger<Dispatcher> logger
) : IDispatcher {

	private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> _voidHandlerCache = new();
	private static readonly ConcurrentDictionary<Type, object> _responseHandlerCache = new();

	private static readonly ActivitySource _activitySource = new(ConductorTelemetry.ActivitySourceName);
	private static readonly Meter _meter = new(ConductorTelemetry.MeterName);

	private static readonly Counter<long> _requestCounter = _meter.CreateCounter<long>(
		"conductor.requests.total",
		description: "Total number of requests dispatched");

	private static readonly Counter<long> _requestErrorCounter = _meter.CreateCounter<long>(
		"conductor.requests.errors",
		description: "Total number of failed requests");

	private static readonly Histogram<double> _requestDuration = _meter.CreateHistogram<double>(
		"conductor.requests.duration",
		unit: "ms",
		description: "Request processing duration in milliseconds");

	#region Helper Methods

	/// <summary>
	/// Starts an activity for telemetry and a stopwatch for timing.
	/// </summary>
	/// <param name="requestName">The name of the request type.</param>
	/// <param name="hasResponse">Indicates if the request expects a response.</param>
	/// <param name="responseType">Optional response type name.</param>
	/// <returns>A tuple containing the started <see cref="Activity"/> and <see cref="Stopwatch"/>.</returns>
	private static (Activity? activity, Stopwatch stopwatch) StartActivityAndStopwatch(
		string requestName, bool hasResponse, string? responseType = null) {
		var activity = _activitySource.StartActivity("Dispatch Request", ActivityKind.Internal);
		activity?.SetTag("request.type", requestName);
		activity?.SetTag("request.has_response", hasResponse);
		if (hasResponse && responseType != null) {
			activity?.SetTag("response.type", responseType);
		}

		var stopwatch = Stopwatch.StartNew();
		return (activity, stopwatch);
	}

	/// <summary>
	/// Records telemetry metrics for the request.
	/// </summary>
	/// <param name="requestName">The request type name.</param>
	/// <param name="responseType">Optional response type name.</param>
	/// <param name="success">Indicates if the request was successful.</param>
	/// <param name="durationMs">The duration in milliseconds.</param>
	/// <param name="errorType">Optional error type name.</param>
	private static void RecordMetrics(string requestName, string? responseType, bool success, double durationMs, string? errorType = null) {
		var tags = new TagList
		{
			{ "request.type", requestName },
			{ "result.success", success }
		};
		if (responseType != null) {
			tags.Add("response.type", responseType);
		}

		if (errorType != null) {
			tags.Add("error.type", errorType);
		}

		_requestCounter.Add(1, tags);
		_requestDuration.Record(durationMs, tags);
		if (!success) {
			_requestErrorCounter.Add(1, tags);
		}
	}

	/// <summary>
	/// Handles exception telemetry and adds details to the activity.
	/// </summary>
	/// <param name="activity">The current activity.</param>
	/// <param name="ex">The exception thrown.</param>
	private static void HandleException(Activity? activity, Exception ex) {
		activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
		activity?.SetTag("error.type", ex.GetType().Name);
		activity?.AddException(ex);
	}

	#endregion

	#region Dispatch Methods

	/// <inheritdoc />
	public async ValueTask<Result> DispatchAsync(
		IRequest request,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);

		var requestType = request.GetType();
		var requestName = requestType.Name;

		var (activity, stopwatch) =
			StartActivityAndStopwatch(
				requestName,
				hasResponse: false);

		try {

			var wrapper = _voidHandlerCache.GetOrAdd(requestType, static rt => {
				var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(rt);
				return (RequestHandlerWrapper)(Activator.CreateInstance(wrapperType)
					?? throw new InvalidOperationException($"Could not create wrapper for {rt.Name}"));
			});

			var result = await wrapper.Handle(request, serviceProvider, logger, cancellationToken);

			stopwatch.Stop();
			RecordMetrics(requestName, null, result.IsSuccess, stopwatch.Elapsed.TotalMilliseconds);

			activity?.SetStatus(result.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.Error?.Message);
			if (!result.IsSuccess) {
				activity?.SetTag("error.type", result.Error?.GetType().Name);
			}

			return result;
		} catch (Exception ex) {
			stopwatch.Stop();
			RecordMetrics(requestName, null, false, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
			HandleException(activity, ex);
			throw;
		}
	}

	/// <inheritdoc />
	public async ValueTask<Result<TResponse>> DispatchAsync<TResponse>(
		IRequest<TResponse> request,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);

		var requestType = request.GetType();
		var requestName = requestType.Name;
		var responseType = typeof(TResponse).Name;

		var (activity, stopwatch) =
			StartActivityAndStopwatch(
				requestName,
				hasResponse: true,
				responseType);

		try {

			var wrapper = (RequestHandlerWrapper<TResponse>)_responseHandlerCache.GetOrAdd(requestType, rt => {
				var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(rt, typeof(TResponse));
				return Activator.CreateInstance(wrapperType)
					?? throw new InvalidOperationException($"Could not create wrapper for {rt.Name}");
			});

			var result = await wrapper.Handle(request, serviceProvider, logger, cancellationToken);

			stopwatch.Stop();
			RecordMetrics(requestName, responseType, result.IsSuccess, stopwatch.Elapsed.TotalMilliseconds);

			activity?.SetStatus(result.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.Error?.Message);
			if (!result.IsSuccess) {
				activity?.SetTag("error.type", result.Error?.GetType().Name);
			}

			return result;
		} catch (Exception ex) {
			stopwatch.Stop();
			RecordMetrics(requestName, responseType, false, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
			HandleException(activity, ex);
			throw;
		}
	}

	#endregion

}