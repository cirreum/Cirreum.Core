namespace Cirreum;

using Cirreum.Security;

/// <summary>
/// Represents the canonical context for an operation, containing the fundamental
/// information about WHO, WHEN, WHERE, and correlation identifiers.
/// </summary>
/// <remarks>
/// This is the single source of truth for operational context that flows through
/// authorization, auditing, telemetry, and other cross-cutting concerns.
/// </remarks>
/// <param name="Environment">The environment in which the operation is being processed (e.g., Development, Staging, Production).</param>
/// <param name="Runtime">The runtime environment in which the operation is being executed.</param>
/// <param name="Timestamp">The timestamp indicating when the operation was created.</param>
/// <param name="UserState">The current user's state, including identity and authentication information.</param>
/// <param name="OperationId">A unique identifier for this operation. Can be used for tracking or logging purposes.</param>
/// <param name="CorrelationId">An identifier used to correlate this operation with related operations or events.</param>
public sealed record OperationContext(
	string Environment,
	ApplicationRuntimeType Runtime,
	DateTimeOffset Timestamp,
	IUserState UserState,
	string OperationId,
	string CorrelationId) {

	// User convenience properties
	public string UserId => this.UserState.Id;
	public string UserName => this.UserState.Name;
	public string? TenantId => this.UserState.Profile.Organization.OrganizationId;
	public IdentityProviderType Provider => this.UserState.Provider;
	public bool IsAuthenticated => this.UserState.IsAuthenticated;
	public UserProfile Profile => this.UserState.Profile;
	public bool HasEnrichedProfile => this.UserState.Profile.IsEnriched;

	// Helper methods
	public bool HasActiveTenant() => !string.IsNullOrWhiteSpace(this.TenantId);
	public bool IsFromProvider(IdentityProviderType provider) => this.Provider == provider;
	public bool IsInDepartment(string department) =>
		string.Equals(this.Profile.Department, department, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Creates an OperationContext for the current runtime.
	/// </summary>
	public static OperationContext Create(
		string environment,
		IUserState userState,
		string operationId,
		string correlationId) =>
		new(
			environment,
			ApplicationRuntime.Current.RuntimeType,
			DateTimeOffset.UtcNow,
			userState,
			operationId,
			correlationId);
}