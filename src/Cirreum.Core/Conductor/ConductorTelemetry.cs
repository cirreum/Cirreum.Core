namespace Cirreum.Conductor;

/// <summary>
/// Constants for OpenTelemetry instrumentation.
/// </summary>
public static class ConductorTelemetry {
	// Metrics
	/// <summary>
	/// Metric: Cache operation duration in milliseconds.
	/// </summary>
	public const string CacheDurationMetric = "conductor.cache.duration";

	/// <summary>
	/// Metric: Total number of requests.
	/// </summary>
	public const string RequestsTotalMetric = "conductor.requests.total";

	/// <summary>
	/// Metric: Total number of failed requests.
	/// </summary>
	public const string RequestsFailedTotalMetric = "conductor.requests.failed";

	/// <summary>
	/// Metric: Total number of canceled requests.
	/// </summary>
	public const string RequestsCanceledTotalMetric = "conductor.requests.canceled";

	/// <summary>
	/// Metric: Histogram of request duration.
	/// </summary>
	public const string RequestsDurationHistogram = "conductor.requests.duration";

	// Tags/Attributes
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
	/// Tag: Error type.
	/// </summary>
	public const string ErrorTypeTag = "error.type";

	/// <summary>
	/// Tag: Request type.
	/// </summary>
	public const string RequestTypeTag = "request.type";

	/// <summary>
	/// Tag: Does this request have a response.
	/// </summary>
	public const string RequestHasResponseTag = "request.has_response";

	/// <summary>
	/// Tag: Response type.
	/// </summary>
	public const string ResponseTypeTag = "response.type";

	/// <summary>
	/// Tag: Request status (success/failure/canceled).
	/// </summary>
	public const string RequestStatusTag = "request.status";

	/// <summary>
	/// Tag: Request succeeded (true/false).
	/// </summary>
	public const string RequestSucceededTag = "request.succeeded";

	/// <summary>
	/// Tag: Request failed (true/false).
	/// </summary>
	public const string RequestFailedTag = "request.failed";

	/// <summary>
	/// Tag: Request canceled (true/false).
	/// </summary>
	public const string RequestCanceledTag = "request.canceled";
}