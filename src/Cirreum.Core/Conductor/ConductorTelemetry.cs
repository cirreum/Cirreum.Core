namespace Cirreum.Conductor;

/// <summary>
/// Constants for OpenTelemetry instrumentation.
/// </summary>
public static class ConductorTelemetry {


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
	/// Tag: Does this request have a response.
	/// </summary>
	public const string RequestHasResponseTag = "request.has_response";

	/// <summary>
	/// Tag: Response type tag.
	/// </summary>
	public const string ResponseTypeTag = "response.type";

	/// <summary>
	/// Tag: Request status (success/failure/canceled).
	/// </summary>
	public const string RequestStatusTag = "request.status";

	/// <summary>
	/// Tag: Request success (true/false).
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

	/// <summary>
	/// Tag: Requests total.
	/// </summary>
	public const string RequestsTotalTag = "requests.total";

	/// <summary>
	/// Tag: Requests failed total.
	/// </summary>
	public const string RequestsFailedTotalTag = "requests.failed";

	/// <summary>
	/// Tag: Requests canceled total.
	/// </summary>
	public const string RequestsCanceledTotalTag = "requests.canceled";

	/// <summary>
	/// Tag: Histogram of requests duration.
	/// </summary>
	public const string RequestsDurationTag = "requests.duration";
}