namespace Cirreum.Conductor;

using Cirreum.Security;
using System.Diagnostics;

/// <summary>
/// Encapsulates contextual information about a request in the Conductor pipeline,
/// including the core operation context plus request-specific tracking information.
/// </summary>
/// <remarks>
/// Use this record to capture and propagate request metadata throughout the application, enabling
/// auditing, diagnostics, and tracing. This context composes <see cref="OperationContext"/> which
/// contains the canonical WHO/WHEN/WHERE information, and adds request-specific concerns like
/// timing and request identification.
/// </remarks>
/// <typeparam name="TRequest">The type of the request payload associated with this context.</typeparam>
/// <param name="Operation">The core operational context containing user, environment, and correlation information.</param>
/// <param name="Stopwatch">A stopwatch instance used to measure the duration of request processing.</param>
/// <param name="Request">The request payload containing the data or command to be processed.</param>
/// <param name="RequestType">The type name of the request, useful for logging and diagnostics.</param>
public sealed record RequestContext<TRequest>(
	OperationContext Operation,
	Stopwatch Stopwatch,
	TRequest Request,
	string RequestType)
	where TRequest : notnull {

	// Delegate to Operation for convenience
	public string Environment => this.Operation.DomainEnvironment.EnvironmentName;
	public IUserState UserState => this.Operation.UserState;
	public DomainRuntimeType RuntimeType => this.Operation.RuntimeType;
	public DateTimeOffset Timestamp => this.Operation.Timestamp;
	public string RequestId => this.Operation.OperationId;
	public string CorrelationId => this.Operation.CorrelationId;

	public string UserId => this.Operation.UserId;
	public string UserName => this.Operation.UserName;
	public string? TenantId => this.Operation.TenantId;
	public IdentityProviderType Provider => this.Operation.Provider;
	public bool IsAuthenticated => this.Operation.IsAuthenticated;
	public UserProfile Profile => this.Operation.Profile;
	public bool HasEnrichedProfile => this.Operation.HasEnrichedProfile;

	public bool HasActiveTenant() => this.Operation.HasActiveTenant();
	public bool IsFromProvider(IdentityProviderType provider) => this.Operation.IsFromProvider(provider);
	public bool IsInDepartment(string department) => this.Operation.IsInDepartment(department);

	public long ElapsedMilliseconds => this.Stopwatch.ElapsedMilliseconds;

	/// <summary>
	/// Creates a RequestContext for the current runtime.
	/// </summary>
	public static RequestContext<TRequest> Create(
		IDomainEnvironment domainEnvironment,
		Stopwatch stopwatch,
		IUserState userState,
		TRequest request,
		string requestType,
		string requestId,
		string correlationId) =>
		new(
			OperationContext.Create(domainEnvironment, userState, requestId, correlationId),
			stopwatch,
			request,
			requestType);
}