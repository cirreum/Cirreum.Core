namespace Cirreum.Conductor;

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

internal static class RequestTelemetry {

	private static readonly ActivitySource _activitySource = new(ConductorTelemetry.ActivitySourceName);
	private static readonly Meter _meter = new(ConductorTelemetry.MeterName);

	private static readonly Counter<long> _requestCounter = _meter.CreateCounter<long>(
		ConductorTelemetry.RequestsTotalTag,
		description: "Total number of requests dispatched");

	private static readonly Counter<long> _requestFailedCounter = _meter.CreateCounter<long>(
		ConductorTelemetry.RequestsFailedTotalTag,
		description: "Total number of failed requests");

	private static readonly Histogram<double> _requestDuration = _meter.CreateHistogram<double>(
		ConductorTelemetry.RequestsDurationTag,
		unit: "ms",
		description: "Request processing duration in milliseconds");


	#region Instrumentation Methods

	internal static (Activity? activity, Stopwatch stopwatch) StartActivityAndStopwatch(
		string requestName,
		bool hasResponse,
		string? responseType = null) {

		var activity = _activitySource.StartActivity("Dispatch Request", ResolveActivityKind());
		activity?.SetTag("request.type", requestName);
		activity?.SetTag("request.has_response", hasResponse);
		if (hasResponse && responseType != null) {
			activity?.SetTag("response.type", responseType);
		}

		var stopwatch = Stopwatch.StartNew();
		return (activity, stopwatch);

	}

	internal static void HandleException(Activity? activity, Exception ex) {
		activity?.SetTag(ConductorTelemetry.ErrorTypeTag, ex.GetType().Name);
		activity?.SetTag(ConductorTelemetry.RequestFailedTag, true);
		activity?.AddException(ex);
	}

	internal static void HandleFailure(ILogger logger, string requestTypeName, string? responseType, double durationMs, Activity? activity, Exception error) {
		var requestId = (activity?.SpanId ?? ActivitySpanId.CreateRandom()).ToString();
		var correlationId = (activity?.TraceId ?? ActivityTraceId.CreateRandom()).ToString();
		var errorType = error.GetType().Name;
		activity?.SetStatus(ActivityStatusCode.Error, error.Message);
		activity?.SetTag(ConductorTelemetry.RequestFailedTag, true);
		RecordMetrics(requestTypeName, responseType, false, durationMs, errorType);
		logger.LogResultFailure(
			requestTypeName,
			requestId,
			correlationId,
			errorType,
			error.Message);
	}

	internal static void HandleSuccess(string requestTypeName, string? responseType, double durationMs, Activity? activity) {
		activity?.SetStatus(ActivityStatusCode.Ok);
		RecordMetrics(requestTypeName, responseType, true, durationMs);
	}

	internal static void RecordMetrics(string requestName, string? responseType, bool success, double durationMs, string? errorType = null) {
		var tags = new TagList
		{
			{ ConductorTelemetry.RequestTypeTag, requestName },
			{ ConductorTelemetry.RequestSucceededTag, success }
		};
		if (responseType != null) {
			tags.Add(ConductorTelemetry.ResponseTypeTag, responseType);
		}

		if (errorType != null) {
			tags.Add(ConductorTelemetry.ErrorTypeTag, errorType);
		}

		_requestCounter.Add(1, tags);
		_requestDuration.Record(durationMs, tags);
		if (!success) {
			_requestFailedCounter.Add(1, tags);
		}

	}

	private static ActivityKind ResolveActivityKind() {
		return ApplicationRuntime.Current.RuntimeType switch {
			ApplicationRuntimeType.Client => ActivityKind.Client,
			ApplicationRuntimeType.Function => ActivityKind.Internal,
			ApplicationRuntimeType.WebApi => ActivityKind.Server,
			ApplicationRuntimeType.WebApp => ActivityKind.Consumer,
			_ => ActivityKind.Internal
		};
	}

	#endregion

}