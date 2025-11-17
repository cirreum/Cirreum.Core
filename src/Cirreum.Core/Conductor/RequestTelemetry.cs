namespace Cirreum.Conductor;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Provides telemetry capabilities for request processing including metrics and distributed tracing.
/// </summary>
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

	#region Activity Management (Distributed Tracing)

	/// <summary>
	/// Starts an activity for distributed tracing. Returns null if tracing is not enabled.
	/// </summary>
	internal static Activity? StartActivity(
		string requestName,
		bool hasResponse = false,
		string? responseType = null) {

		var activity = _activitySource.StartActivity(
			"Dispatch Request",
			DomainContext.CurrentActivityKind);

		activity?.SetTag(ConductorTelemetry.RequestTypeTag, requestName);
		activity?.SetTag(ConductorTelemetry.RequestHasResponseTag, hasResponse);

		if (hasResponse && responseType is not null) {
			activity?.SetTag(ConductorTelemetry.ResponseTypeTag, responseType);
		}

		return activity;
	}

	/// <summary>
	/// Stops and disposes an activity if it exists.
	/// </summary>
	internal static void StopActivity(Activity? activity) {
		if (activity is not null) {
			activity.Stop();
			activity.Dispose();
		}
	}

	/// <summary>
	/// Sets success information on an activity.
	/// </summary>
	internal static void SetActivitySuccess(Activity? activity) {
		activity?.SetStatus(ActivityStatusCode.Ok);
	}

	/// <summary>
	/// Sets error information on an activity.
	/// </summary>
	internal static void SetActivityError(Activity? activity, Exception ex) {
		if (activity is not null) {
			activity.SetStatus(ActivityStatusCode.Error, ex.Message);
			activity.SetTag(ConductorTelemetry.ErrorTypeTag, ex.GetType().Name);
			activity.SetTag(ConductorTelemetry.RequestFailedTag, true);
			activity.AddException(ex);
		}
	}

	/// <summary>
	/// Sets cancellation information on an activity.
	/// </summary>
	internal static void SetActivityCanceled(Activity? activity, OperationCanceledException oce) {
		if (activity is not null) {
			activity.SetStatus(ActivityStatusCode.Error, "Canceled");
			activity.SetTag(ConductorTelemetry.RequestCanceledTag, true);
			activity.AddException(oce);
		}
	}

	#endregion

	#region Metrics Recording

	/// <summary>
	/// Records success metrics for a completed request.
	/// </summary>
	internal static void RecordSuccess(
		string requestTypeName,
		string? responseType,
		double durationMs) {

		RecordMetrics(
			requestTypeName,
			responseType,
			success: true,
			canceled: false,
			durationMs);
	}

	/// <summary>
	/// Records failure metrics for a failed request.
	/// </summary>
	internal static void RecordFailure(
		string requestTypeName,
		string? responseType,
		double durationMs,
		Exception error) {

		RecordMetrics(
			requestTypeName,
			responseType,
			success: false,
			canceled: false,
			durationMs,
			error.GetType().Name);
	}

	/// <summary>
	/// Records cancellation metrics for a canceled request.
	/// </summary>
	internal static void RecordCanceled(
		string requestTypeName,
		string? responseType,
		double durationMs,
		OperationCanceledException oce) {

		RecordMetrics(
			requestTypeName,
			responseType,
			success: false,
			canceled: true,
			durationMs,
			oce.GetType().Name);
	}

	private static void RecordMetrics(
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

	#endregion

}