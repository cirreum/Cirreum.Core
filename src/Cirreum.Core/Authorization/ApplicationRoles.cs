namespace Cirreum.Authorization;

/// <summary>
/// Defines the default and publicly available application roles.
/// </summary>
public static class ApplicationRoles {

	/// <summary>
	/// Represents an authenticated User that can perform core application functions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// app:user
	/// </para>
	/// <para>
	/// Does not inherit any other roles.
	/// </para>
	/// </remarks>
	public static readonly Role AppUserRole = Role.ForApp("user");

	/// <summary>
	/// Represents an authenticated User that is a member of the internal organization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// app:internal
	/// </para>
	/// <para>
	/// Inherits from <see cref="AppUserRole"/>
	/// </para>
	/// </remarks>
	public static readonly Role AppInternalRole = Role.ForApp("internal");

	/// <summary>
	/// Represents an authenticated User that performs service and support related activities.
	/// </summary>
	/// <remarks>
	/// <para>
	/// app:agent
	/// </para>
	/// <para>
	/// Inherits from <see cref="AppInternalRole"/>
	/// </para>
	/// </remarks>
	public static readonly Role AppAgentRole = Role.ForApp("agent");

	/// <summary>
	/// Represents an authenticated User that is elevated to perform Manager related activities.
	/// </summary>
	/// <remarks>
	/// <para>
	/// app:manager
	/// </para>
	/// <para>
	/// Inherits from <see cref="AppInternalRole"/>
	/// </para>
	/// </remarks>
	public static readonly Role AppManagerRole = Role.ForApp("manager");

	/// <summary>
	/// Represents an authenticated User that is elevated to perform all restricted Application related activities.
	/// </summary>
	/// <remarks>
	/// <para>
	/// app:admin
	/// </para>
	/// <para>
	/// Inherits from <see cref="AppManagerRole"/> and <see cref="AppAgentRole"/>
	/// </para>
	/// </remarks>
	public static readonly Role AppAdminRole = Role.ForApp("admin");

	/// <summary>
	/// Represents an application system (aka daemon/function) that operates with full administrative privileges.
	/// </summary>
	/// <remarks>
	/// <para>
	/// app:system
	/// </para>
	/// <para>
	/// Inherits from <see cref="AppAdminRole"/> to have complete access to all application functions
	/// </para>
	/// </remarks>
	public static readonly Role AppSystemRole = Role.ForApp("system");

	/// <summary>
	/// Gets all the application (app:) namespace defined roles, as a readonly list.
	/// </summary>
	public static IReadOnlyList<Role> GetRoles() => _allRoles;

	private static readonly IReadOnlyList<Role> _allRoles = [
		AppUserRole,
		AppInternalRole,
		AppAgentRole,
		AppManagerRole,
		AppAdminRole,
		AppSystemRole
	];

}