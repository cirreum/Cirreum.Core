namespace Cirreum.Authorization;

using Cirreum.Security;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Non-generic base representing the resolved caller identity for an authorization evaluation.
/// Contains the caller's <see cref="UserState"/> and <see cref="EffectiveRoles"/> — the two
/// pieces that are expensive to resolve and reusable across authorization tiers.
/// </summary>
/// <param name="UserState">The current user's state, including identity and authentication information.</param>
/// <param name="EffectiveRoles">
/// The effective roles (resolved roles) associated with the current user,
/// including any roles inherited from others (downward inheritance only, e.g.,
/// a manager inherits from coordinator, which inherits from user).
/// </param>
/// <remarks>
/// <para>
/// The user may only be assigned a single role by the Identity Provider (IdP), but based on application-defined
/// rules, this role may inherit additional roles. As a result, the <see cref="EffectiveRoles"/> here include
/// those additional roles, enabling the system to take advantage of role hierarchies without requiring
/// administrators to assign multiple roles for each user or user group. This also helps prevent role assignments
/// from exceeding the JWT's size limit, avoiding overflow.
/// </para>
/// <para>
/// This base record is stored on <see cref="IAuthorizationContextAccessor"/> after the authorization
/// pipeline resolves it, making the resolved caller identity available to downstream consumers
/// (e.g., <c>ResourceAccessEvaluator</c>) without redundant role resolution.
/// </para>
/// </remarks>
public record AuthorizationContext(
	IUserState UserState,
	IImmutableSet<Role> EffectiveRoles) {

	/// <summary>
	/// The application-layer user loaded from the app's user store, or <see langword="null"/>
	/// when no app-db record backs this caller (e.g., workforce identities that exist only in
	/// an operator IdP). Shortcut for <c>UserState.ApplicationUser</c>.
	/// </summary>
	public IApplicationUser? ApplicationUser => this.UserState.ApplicationUser;

	/// <summary>
	/// Gets the date and time, in Coordinated Universal Time (UTC), when the event occurred or the object was created.
	/// </summary>
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

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
	/// Gets the runtime type information for the current domain context.
	/// </summary>
	/// <remarks>This property provides access to the runtime type as determined at application startup. The value
	/// is set once and does not change during the application's lifetime. Use this property to query environment-specific
	/// type information relevant to the domain context.</remarks>
	[SuppressMessage("Performance", "CA1822:Mark members as static",
		Justification = "Instance convenience — delegates to DomainContext for consumer ergonomics.")]
	public DomainRuntimeType RuntimeType => DomainContext.RuntimeType;

	// Helper methods
	public bool HasActiveTenant() => !string.IsNullOrWhiteSpace(this.TenantId);
	public bool IsFromProvider(IdentityProviderType provider) => this.Provider == provider;
	public bool IsInDepartment(string department) =>
		!string.IsNullOrWhiteSpace(this.Profile.Department) &&
		string.Equals(this.Profile.Department, department, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Generic extension of <see cref="AuthorizationContext"/> that adds the specific
/// <see cref="IAuthorizableObject"/> being evaluated for authorization.
/// </summary>
/// <typeparam name="TAuthorizableObject">The type of the <see cref="IAuthorizableObject"/> being authorized.</typeparam>
/// <param name="UserState">The current user's state, including identity and authentication information.</param>
/// <param name="EffectiveRoles">The effective roles associated with the current user.</param>
/// <param name="AuthorizableObject">The <see cref="IAuthorizableObject"/> being evaluated for authorization.</param>
public sealed record AuthorizationContext<TAuthorizableObject>(
	IUserState UserState,
	IImmutableSet<Role> EffectiveRoles,
	TAuthorizableObject AuthorizableObject)
	: AuthorizationContext(UserState, EffectiveRoles)
	where TAuthorizableObject : IAuthorizableObject {

	/// <summary>
	/// The domain feature derived from <typeparamref name="TAuthorizableObject"/>'s namespace convention.
	/// Cached per-type via <see cref="DomainFeatureResolver"/> — zero per-request cost.
	/// </summary>
	[SuppressMessage("Performance", "CA1822:Mark members as static",
		Justification = "Instance convenience — delegates to DomainFeatureResolver for consumer ergonomics.")]
	public string? DomainFeature => DomainFeatureResolver.Resolve<TAuthorizableObject>();

	/// <summary>
	/// The distinct set of permissions declared on <typeparamref name="TAuthorizableObject"/> via
	/// <see cref="RequiresPermissionAttribute"/>. Hoisted once per type from
	/// <see cref="RequiredPermissionCache"/>; available to every authorization stage without
	/// per-request reflection. AND semantics — every listed permission is required.
	/// </summary>
	[SuppressMessage("Performance", "CA1822:Mark members as static",
		Justification = "Instance convenience — delegates to RequiredPermissionCache for consumer ergonomics.")]
	public PermissionSet Permissions => RequiredPermissionCache.GetFor<TAuthorizableObject>();
}
