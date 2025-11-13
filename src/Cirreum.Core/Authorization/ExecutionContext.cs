namespace Cirreum.Authorization;

/// <summary>
/// Provides execution context information for authorization decisions.
/// </summary>
/// <param name="RuntimeType"></param>
/// <param name="Timestamp"></param>
/// <param name="RequestId">Unique identifier for the current request/operation.</param>
/// <param name="CorrelationId">Unique identifier for correlating related operations across service boundaries.</param>
public sealed record ExecutionContext(
	ApplicationRuntimeType RuntimeType,
	DateTimeOffset Timestamp,
	string? RequestId = null,
	string? CorrelationId = null) {
	/// <summary>
	/// Creates an ExecutionContext for the current runtime.
	/// </summary>
	public static ExecutionContext ForCurrentRuntime(
		string? requestId = null,
		string? correlationId = null) =>
		new(
			ApplicationRuntime.Current.RuntimeType,
			DateTimeOffset.UtcNow,
			requestId,
			correlationId);
}