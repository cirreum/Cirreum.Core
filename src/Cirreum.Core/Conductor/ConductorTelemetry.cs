namespace Cirreum.Conductor;

/// <summary>
/// Constants for OpenTelemetry instrumentation.
/// </summary>
public static class ConductorTelemetry {
	/// <summary>
	/// The meter name for Conductor dispatcher metrics.
	/// Use this when configuring OpenTelemetry: <c>metrics.AddMeter(ConductorTelemetry.MeterName)</c>
	/// </summary>
	public const string MeterName = "Cirreum.Conductor.Dispatcher";

	/// <summary>
	/// The meter name for Conductor cache metrics.
	/// Use this when configuring OpenTelemetry: <c>metrics.AddMeter(ConductorTelemetry.CacheMeterName)</c>
	/// </summary>
	public const string CacheMeterName = "Cirreum.Conductor.Cache";

	/// <summary>
	/// The activity source name for Conductor tracing.
	/// Use this when configuring OpenTelemetry: <c>tracing.AddSource(ConductorTelemetry.ActivitySourceName)</c>
	/// </summary>
	public const string ActivitySourceName = "Cirreum.Conductor.Dispatcher";

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
	/// Tag: Operation result (success/failure).
	/// </summary>
	public const string ResultStatusTag = "result.status";
}