namespace Cirreum.Authorization;

using Cirreum.Security;
using System.Collections.Immutable;

/// <summary>
/// Represents the context for a given authorization check.
/// </summary>
/// <param name="Resource">The <see cref="IAuthorizableResource"/> object being evaluated.</param>
/// <param name="UserRoles">
/// The effective roles (resolved roles) associated with the current user, 
/// including any roles inherited from others (downward inheritance only, e.g., 
/// a manager inherits from coordinator, which inherits from user).
/// </param>
/// <param name="UserState">The current <see cref="IUserState"/> representing the user's state.</param>
/// <param name="ExecutionContext">The current execution context.</param>
/// <remarks>
/// <para>
/// The user may only be assigned a single role by the Identity Provider (IdP), but based on application-defined rules,
/// this role may inherit additional roles. As a result, the <see cref="UserRoles"/> here include those additional roles, 
/// enabling the system to take advantage of role hierarchies without requiring administrators to assign multiple roles 
/// for each user or user group. This also helps prevent role assignments from exceeding the JWT's size limit, avoiding overflow.
/// </para>
/// </remarks>
public sealed record AuthorizationContext<TResource>(
	TResource Resource,
	IImmutableSet<Role> UserRoles,
	IUserState UserState,
	ExecutionContext ExecutionContext)
	where TResource : IAuthorizableResource {

	// Convenience properties for common access patterns
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
	/// Creates a new authorization context for the current runtime.
	/// </summary>
	public static AuthorizationContext<TResource> Create(
		TResource resource,
		IImmutableSet<Role> userRoles,
		IUserState userState,
		string? requestId = null,
		string? correlationId = null) =>
		new(
			resource,
			userRoles,
			userState,
			ExecutionContext.ForCurrentRuntime(requestId, correlationId));
}