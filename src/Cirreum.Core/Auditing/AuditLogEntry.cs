namespace Cirreum.Auditing;

public sealed class AuditLogEntry {

	// ENVIRONMENT / RUNTIME
	public string Environment { get; init; } = string.Empty;
	public ApplicationRuntimeType Runtime { get; init; }

	// WHEN
	public DateTimeOffset Timestamp { get; init; }
	public long DurationMs { get; init; }

	// WHO (User Context)
	public string UserId { get; init; } = string.Empty;
	public string UserName { get; init; } = string.Empty;
	public string? TenantId { get; init; }
	public IdentityProviderType Provider { get; init; }
	public bool IsAuthenticated { get; init; }

	// REQUEST (WHAT)
	public string RequestType { get; init; } = string.Empty;
	public string RequestId { get; init; } = string.Empty;
	public string CorrelationId { get; init; } = string.Empty;

	// RESULT (Outcome)
	public string Result { get; init; } = string.Empty;
	public string? FailureReason { get; init; }
	public string? ErrorType { get; init; }

}