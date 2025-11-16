namespace Cirreum.Authorization;

using Cirreum.Security;
using System.Collections.Immutable;

/// <summary>
/// Represents the context for a given authorization check.
/// </summary>
/// <param name="Operation">The core operational context containing user, environment, and timing information.</param>
/// <param name="EffectiveRoles">
/// The effective roles (resolved roles) associated with the current user, 
/// including any roles inherited from others (downward inheritance only, e.g., 
/// a manager inherits from coordinator, which inherits from user).
/// </param>
/// <param name="Resource">The <see cref="IAuthorizableResource"/> object being evaluated.</param>
/// <remarks>
/// <para>
/// The user may only be assigned a single role by the Identity Provider (IdP), but based on application-defined
/// rules, this role may inherit additional roles. As a result, the <see cref="EffectiveRoles"/> here include
/// those additional roles, enabling the system to take advantage of role hierarchies without requiring
/// administrators to assign multiple roles for each user or user group. This also helps prevent role assignments
/// from exceeding the JWT's size limit, avoiding overflow.
/// </para>
/// </remarks>
public sealed record AuthorizationContext<TResource>(
	OperationContext Operation,
	IImmutableSet<Role> EffectiveRoles,
	TResource Resource)
	where TResource : IAuthorizableResource {

	// Convenience properties delegated to Operation
	public string UserId => this.Operation.UserId;
	public string UserName => this.Operation.UserName;
	public string? TenantId => this.Operation.TenantId;
	public IdentityProviderType Provider => this.Operation.Provider;
	public bool IsAuthenticated => this.Operation.IsAuthenticated;
	public UserProfile Profile => this.Operation.Profile;
	public bool HasEnrichedProfile => this.Operation.HasEnrichedProfile;
	public IUserState UserState => this.Operation.UserState;

	// ExecutionContext properties (for policy validators)
	public DomainRuntimeType RuntimeType => this.Operation.RuntimeType;
	public DateTimeOffset Timestamp => this.Operation.Timestamp;

	// Helper methods
	public bool HasActiveTenant() => this.Operation.HasActiveTenant();
	public bool IsFromProvider(IdentityProviderType provider) => this.Operation.IsFromProvider(provider);
	public bool IsInDepartment(string department) => this.Operation.IsInDepartment(department);
}