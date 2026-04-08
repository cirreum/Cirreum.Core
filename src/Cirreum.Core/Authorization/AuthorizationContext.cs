namespace Cirreum.Authorization;

using Cirreum.Security;
using System.Collections.Immutable;

/// <summary>
/// Represents the context for evaluating authorization against an <see cref="IAuthorizableObject"/>.
/// </summary>
/// <param name="UserState">The current user's state, including identity and authentication information.</param>
/// <param name="EffectiveRoles">
/// The effective roles (resolved roles) associated with the current user,
/// including any roles inherited from others (downward inheritance only, e.g.,
/// a manager inherits from coordinator, which inherits from user).
/// </param>
/// <param name="AuthorizableObject">The <see cref="IAuthorizableObject"/> being evaluated for authorization.</param>
/// <remarks>
/// <para>
/// The user may only be assigned a single role by the Identity Provider (IdP), but based on application-defined
/// rules, this role may inherit additional roles. As a result, the <see cref="EffectiveRoles"/> here include
/// those additional roles, enabling the system to take advantage of role hierarchies without requiring
/// administrators to assign multiple roles for each user or user group. This also helps prevent role assignments
/// from exceeding the JWT's size limit, avoiding overflow.
/// </para>
/// </remarks>
public sealed record AuthorizationContext<TAuthorizableObject>(
	IUserState UserState,
	IImmutableSet<Role> EffectiveRoles,
	TAuthorizableObject AuthorizableObject)
	where TAuthorizableObject : IAuthorizableObject {

	/// <summary>
	/// The domain feature derived from <typeparamref name="TAuthorizableObject"/>'s namespace convention.
	/// Cached per-type via <see cref="DomainFeatureResolver"/> — zero per-request cost.
	/// </summary>
	public string? DomainFeature => DomainFeatureResolver.Resolve<TAuthorizableObject>();

	// User convenience properties
	public string UserId => this.UserState.Id;
	public string UserName => this.UserState.Name;
	public string? TenantId => this.UserState.Profile.Organization.OrganizationId;
	public IdentityProviderType Provider => this.UserState.Provider;
	public AccessScope AccessScope => this.UserState.AccessScope;
	public bool IsAuthenticated => this.UserState.IsAuthenticated;
	public UserProfile Profile => this.UserState.Profile;
	public bool HasEnrichedProfile => this.UserState.Profile.IsEnriched;

	/// <summary>
	/// The application-layer user loaded from the app's user store, or <see langword="null"/>
	/// when no app-db record backs this caller (e.g., workforce identities that exist only in
	/// an operator IdP). Shortcut for <c>UserState.ApplicationUser</c>.
	/// </summary>
	public IApplicationUser? ApplicationUser => this.UserState.ApplicationUser;

	/// <summary>
	/// The distinct set of permissions declared on <typeparamref name="TAuthorizableObject"/> via
	/// <see cref="RequiresPermissionAttribute"/>. Hoisted once per type from
	/// <see cref="RequiredPermissionCache"/>; available to every authorization stage without
	/// per-request reflection. AND semantics — every listed permission is required.
	/// </summary>
	public PermissionSet Permissions => RequiredPermissionCache.GetFor<TAuthorizableObject>();

	// Static environment — set once at startup via DomainContext
	public DomainRuntimeType RuntimeType => DomainContext.RuntimeType;
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

	// Helper methods
	public bool HasActiveTenant() => !string.IsNullOrWhiteSpace(this.TenantId);
	public bool IsFromProvider(IdentityProviderType provider) => this.Provider == provider;
	public bool IsInDepartment(string department) =>
		!string.IsNullOrWhiteSpace(this.Profile.Department) &&
		string.Equals(this.Profile.Department, department, StringComparison.OrdinalIgnoreCase);
}
