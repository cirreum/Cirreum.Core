namespace Cirreum.Conductor;

using Cirreum.Security;
using System;

/// <summary>
/// Encapsulates contextual information about a request in the Conductor pipeline:
/// the caller identity, the request payload, timing, and correlation identifiers.
/// </summary>
/// <remarks>
/// Use this record to capture and propagate request metadata throughout the application, enabling
/// auditing, diagnostics, and tracing. Created once per pipeline invocation by
/// <see cref="Internal.OperationContextFactory"/>.
/// </remarks>
/// <typeparam name="TOperation">The type of the request payload associated with this context.</typeparam>
/// <param name="UserState">The current user's state, including identity and authentication information.</param>
/// <param name="Operation">The payload containing the data or command to be processed.</param>
/// <param name="OperationType">The type name of the operation, useful for logging and diagnostics.</param>
/// <param name="OperationId">A unique identifier for this operation (typically the <c>Activity.SpanId</c>).</param>
/// <param name="CorrelationId">An identifier used to correlate this operation with related operations (typically the <c>Activity.TraceId</c>).</param>
/// <param name="StartTimestamp">The high-precision timestamp for accurate duration calculation.</param>
public sealed record OperationContext<TOperation>(
	IUserState UserState,
	TOperation Operation,
	string OperationType,
	string OperationId,
	string CorrelationId,
	long StartTimestamp)
	where TOperation : notnull {

	/// <summary>
	/// The domain feature derived from <typeparamref name="TOperation"/>'s namespace convention.
	/// Cached per-type via <see cref="DomainFeatureResolver"/> — zero per-request cost.
	/// </summary>
	public string? DomainFeature => DomainFeatureResolver.Resolve<TOperation>();

	// Static environment — set once at startup via DomainContext
	public string Environment => DomainContext.Environment;
	public DomainRuntimeType RuntimeType => DomainContext.RuntimeType;

	// Timestamp captured at creation
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

	// User convenience properties
	public string UserId => this.UserState.Id;
	public string UserName => this.UserState.Name;
	/// <summary>
	/// The caller's tenant (organization) identifier, or <see langword="null"/> when
	/// no organization is associated with the user profile. Sourced from the OIDC
	/// <c>tid</c> claim (or provider-equivalent) via <c>UserState.Profile.Organization.OrganizationId</c>.
	/// </summary>
	public string? TenantId => this.UserState.Profile.Organization.OrganizationId;
	public IdentityProviderType Provider => this.UserState.Provider;
	public AuthenticationBoundary AuthenticationBoundary => this.UserState.AuthenticationBoundary;
	public bool IsAuthenticated => this.UserState.IsAuthenticated;
	public UserProfile Profile => this.UserState.Profile;
	public bool HasEnrichedProfile => this.UserState.Profile.IsEnriched;

	// Timing
	public TimeSpan ElapsedDuration => Timing.GetElapsedTime(this.StartTimestamp);

	// Helper methods
	public bool HasActiveTenant() => !string.IsNullOrWhiteSpace(this.TenantId);
	public bool IsFromProvider(IdentityProviderType provider) => this.Provider == provider;
	public bool IsInDepartment(string department) =>
		!string.IsNullOrWhiteSpace(this.Profile.Department) &&
		string.Equals(this.Profile.Department, department, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Creates a <see cref="OperationContext{TOperation}"/> for the current runtime.
	/// </summary>
	public static OperationContext<TOperation> Create(
		IUserState userState,
		TOperation operation,
		string operationType,
		string operationId,
		string correlationId,
		long startTimestamp) =>
		new(
			userState,
			operation,
			operationType,
			operationId,
			correlationId,
			startTimestamp);
}
