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

	private static readonly Counter<long> _requestCanceledCounter = _meter.CreateCounter<long>(
		ConductorTelemetry.RequestsCanceledTotalTag,
		description: "Total number of canceled requests");

	private static readonly Histogram<double> _requestDuration = _meter.CreateHistogram<double>(
		ConductorTelemetry.RequestsDurationTag,
		unit: "ms",
		description: "Request processing duration in milliseconds");

	#region Instrumentation Methods

	internal static (Activity? activity, Stopwatch stopwatch) StartActivityAndStopwatch(
		IDomainEnvironment applicationEnvironment,
		string requestName,
		bool hasResponse,
		string? responseType = null) {

		var activity = _activitySource.StartActivity("Dispatch Request", ResolveActivityKind(applicationEnvironment.RuntimeType));
		activity?.SetTag(ConductorTelemetry.RequestTypeTag, requestName);
		activity?.SetTag("request.has_response", hasResponse); // optional constant if you want
		if (hasResponse && responseType is not null) {
			activity?.SetTag(ConductorTelemetry.ResponseTypeTag, responseType);
		}

		var stopwatch = Stopwatch.StartNew();
		return (activity, stopwatch);
	}

	/// <summary>
	/// Basic exception tagging (used if you just need to annotate an Activity).
	/// </summary>
	internal static void HandleException(Activity? activity, Exception ex) {
		activity?.SetTag(ConductorTelemetry.ErrorTypeTag, ex.GetType().Name);
		activity?.SetTag(ConductorTelemetry.RequestFailedTag, true);
		activity?.AddException(ex);
	}

	internal static void HandleFailure(
		ILogger logger,
		string requestTypeName,
		string? responseType,
		double durationMs,
		Activity? activity,
		Exception error) {

		var requestId = (activity?.SpanId ?? ActivitySpanId.CreateRandom()).ToString();
		var correlationId = (activity?.TraceId ?? ActivityTraceId.CreateRandom()).ToString();
		var errorType = error.GetType().Name;

		activity?.SetStatus(ActivityStatusCode.Error, error.Message);
		activity?.SetTag(ConductorTelemetry.RequestFailedTag, true);

		RecordMetrics(
			requestTypeName,
			responseType,
			success: false,
			canceled: false,
			durationMs,
			errorType);

		logger.LogResultFailure(
			requestTypeName,
			requestId,
			correlationId,
			errorType,
			error.Message);
	}

	internal static void HandleSuccess(
		string requestTypeName,
		string? responseType,
		double durationMs,
		Activity? activity) {

		activity?.SetStatus(ActivityStatusCode.Ok);

		RecordMetrics(
			requestTypeName,
			responseType,
			success: true,
			canceled: false,
			durationMs);
	}

	internal static void HandleCanceled(
		string requestTypeName,
		string? responseType,
		double durationMs,
		Activity? activity,
		OperationCanceledException oce) {

		activity?.SetStatus(ActivityStatusCode.Error, "Canceled");
		activity?.SetTag(ConductorTelemetry.RequestCanceledTag, true);
		activity?.AddException(oce);

		RecordMetrics(
			requestTypeName,
			responseType,
			success: false,
			canceled: true,
			durationMs,
			oce.GetType().Name);
	}

	internal static void RecordMetrics(
		string requestName,
		string? responseType,
		bool success,
		bool canceled,
		double durationMs,
		string? errorType = null) {

		var tags = new TagList {
			{ ConductorTelemetry.RequestTypeTag, requestName },
			{ ConductorTelemetry.RequestSucceededTag, success },
			{ ConductorTelemetry.RequestFailedTag, !success && !canceled },
			{ ConductorTelemetry.RequestCanceledTag, canceled },
			{ ConductorTelemetry.RequestStatusTag, canceled ? "canceled" : success ? "success" : "failure" }
		};

		if (responseType is not null) {
			tags.Add(ConductorTelemetry.ResponseTypeTag, responseType);
		}

		if (errorType is not null) {
			tags.Add(ConductorTelemetry.ErrorTypeTag, errorType);
		}

		_requestCounter.Add(1, tags);
		_requestDuration.Record(durationMs, tags);

		// NOTE: by design, "canceled" is NOT counted as a failed request
		if (!success && !canceled) {
			_requestFailedCounter.Add(1, tags);
		}

		if (canceled) {
			_requestCanceledCounter.Add(1, tags);
		}

	}

	private static ActivityKind ResolveActivityKind(DomainRuntimeType runtimeType) {
		return runtimeType switch {
			DomainRuntimeType.BlazorWasm => ActivityKind.Client,
			DomainRuntimeType.MauiHybrid => ActivityKind.Client,
			DomainRuntimeType.Function => ActivityKind.Internal,
			DomainRuntimeType.WebApi => ActivityKind.Server,
			DomainRuntimeType.WebApp => ActivityKind.Consumer,
			_ => ActivityKind.Internal
		};
	}

	#endregion
}
