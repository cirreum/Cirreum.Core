namespace Cirreum.Auditing;

using Cirreum.Conductor;

public static class AuditLogEntryFactory {

	public static AuditLogEntry FromRequestContext<TRequest>(
		RequestContext<TRequest> context,
		string result,
		string? failureReason,
		string? errorType)
		where TRequest : notnull =>
		new() {
			// From OperationContext
			Environment = context.Operation.Environment,
			Runtime = context.Operation.Runtime,
			Timestamp = context.Operation.Timestamp,
			UserId = context.Operation.UserId,
			UserName = context.Operation.UserName,
			TenantId = context.Operation.TenantId,
			Provider = context.Operation.Provider,
			IsAuthenticated = context.Operation.IsAuthenticated,

			// From RequestContext
			RequestType = context.RequestType,
			RequestId = context.RequestId,
			CorrelationId = context.CorrelationId,
			DurationMs = context.Stopwatch.ElapsedMilliseconds,

			// Outcome
			Result = result,
			FailureReason = failureReason,
			ErrorType = errorType
		};
}