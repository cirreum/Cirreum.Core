namespace Cirreum.Conductor;

/// <summary>
/// Constants for OpenTelemetry instrumentation.
/// </summary>
public static class ConductorTelemetry {

	/// <summary>
	/// The meter name for Conductor dispatcher metrics.
	/// Use this when configuring OpenTelemetry: <c>metrics.AddMeter(ConductorTelemetry.MeterName)</c>
	/// </summary>
	public const string MeterName = "cirreum.conductor.dispatcher";

	/// <summary>
	/// The activity source name for Conductor tracing.
	/// Use this when configuring OpenTelemetry: <c>tracing.AddSource(ConductorTelemetry.ActivitySourceName)</c>
	/// </summary>
	public const string ActivitySourceName = "cirreum.conductor.dispatcher";

	/// <summary>
	/// The meter name for Conductor cache metrics.
	/// Use this when configuring OpenTelemetry: <c>metrics.AddMeter(ConductorTelemetry.CacheMeterName)</c>
	/// </summary>
	public const string CacheMeterName = "cirreum.conductor.cache";

	/// <summary>
	/// Metric: Cache operation duration in milliseconds.
	/// </summary>
	public const string CacheDurationMetric = "conductor.cache.duration";

	/// <summary>
	/// Tag: Query type name.
	/// </summary>
	public const string QueryNameTag = "query.name";

	/// <summary>
	/// Tag: Query cache category.
	/// </summary>
	public const string QueryCategoryTag = "query.category";

	/// <summary>
	/// Tag: Query status (success/failure).
	/// </summary>
	public const string QueryStatusTag = "query.status";

	/// <summary>
	/// Tag: Error type tag.
	/// </summary>
	public const string ErrorTypeTag = "error.type";

	/// <summary>
	/// Tag: Request type tag.
	/// </summary>
	public const string RequestTypeTag = "request.type";

	/// <summary>
	/// Tag: Response type tag.
	/// </summary>
	public const string ResponseTypeTag = "response.type";

	/// <summary>
	/// Tag: Request success (true/false).
	/// </summary>
	public const string RequestSucceededTag = "request.success";

	/// <summary>
	/// Tag: Request failed (true/false).
	/// </summary>
	public const string RequestFailedTag = "request.failed";

	/// <summary>
	/// Tag: Requests total.
	/// </summary>
	public const string RequestsTotalTag = "requests.total";

	/// <summary>
	/// Tag: Requests failed total.
	/// </summary>
	public const string RequestsFailedTotalTag = "requests.failed";

	/// <summary>
	/// Tag: Histogram of requests duration.
	/// </summary>
	public const string RequestsDurationTag = "requests.duration";

}