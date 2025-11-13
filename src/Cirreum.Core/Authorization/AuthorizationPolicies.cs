namespace Cirreum.Authorization;
/// <summary>
/// Defines a set of authorization policy names used throughout the application to enforce role-based access control.
/// </summary>
/// <remarks>
/// The <see cref="AuthorizationPolicies"/> class encapsulates various authorization policies that govern access to different parts of the application.
/// 
/// <list type="bullet">
///     <item>
///         <term><see cref="Standard"/></term>
///         <description>
///             Grants access to any authenticated entity with a valid role, including:
///             <see cref="ApplicationRoles.AppSystemRole"/> (app:system),
///             <see cref="ApplicationRoles.AppAdminRole"/> (app:admin),
///             <see cref="ApplicationRoles.AppManagerRole"/> (app:manager),
///             <see cref="ApplicationRoles.AppAgentRole"/> (app:agent),
///             <see cref="ApplicationRoles.AppInternalRole"/> (app:internal), or
///             <see cref="ApplicationRoles.AppUserRole"/> (app:user).
///         </description>
///     </item>
///     <item>
///         <term><see cref="StandardInternal"/></term>
///         <description>
///             Grants access to users with organizational roles:
///             <see cref="ApplicationRoles.AppSystemRole"/> (app:system),
///             <see cref="ApplicationRoles.AppAdminRole"/> (app:admin),
///             <see cref="ApplicationRoles.AppManagerRole"/> (app:manager), or
///             <see cref="ApplicationRoles.AppInternalRole"/> (app:internal).
///             Used for operations requiring organization-wide access.
///         </description>
///     </item>
///     <item>
///         <term><see cref="StandardAgent"/></term>
///         <description>
///             Grants access to support and service roles:
///             <see cref="ApplicationRoles.AppSystemRole"/> (app:system),
///             <see cref="ApplicationRoles.AppAdminRole"/> (app:admin),
///             <see cref="ApplicationRoles.AppManagerRole"/> (app:manager), or
///             <see cref="ApplicationRoles.AppAgentRole"/> (app:agent).
///             Used for customer service and support operations.
///         </description>
///     </item>
///     <item>
///         <term><see cref="StandardManager"/></term>
///         <description>
///             Grants access to management roles:
///             <see cref="ApplicationRoles.AppSystemRole"/> (app:system),
///             <see cref="ApplicationRoles.AppAdminRole"/> (app:admin), or
///             <see cref="ApplicationRoles.AppManagerRole"/> (app:manager).
///             Suitable for tasks requiring elevated organizational oversight.
///         </description>
///     </item>
///     <item>
///         <term><see cref="StandardAdmin"/></term>
///         <description>
///             Grants access to administrative roles:
///             <see cref="ApplicationRoles.AppSystemRole"/> (app:system) or
///             <see cref="ApplicationRoles.AppAdminRole"/> (app:admin).
///             Intended for system administration and configuration tasks.
///         </description>
///     </item>
///     <item>
///         <term><see cref="System"/></term>
///         <description>
///             Restricts access exclusively to non-interactive clients with the 
///             <see cref="ApplicationRoles.AppSystemRole"/> (app:system) role.
///             Used for daemon/serverless processes that operate without user context.
///         </description>
///     </item>
/// </list>
/// <para>
/// These policies are utilized by the authorization framework to determine whether an entity has 
/// the necessary permissions to perform specific actions. Each policy represents a different level 
/// of access control, tailored to various organizational responsibilities and system requirements.
/// </para>
/// </remarks>
public static class AuthorizationPolicies {

	/// <summary>
	/// Represents the default policy allowing access to any authenticated entity with a valid role.
	/// </summary>
	/// <remarks>
	/// Grants access to any authenticated entity, including system processes and all user roles
	/// from basic users to administrators.
	/// </remarks>
	public const string Standard = "Auth.Standard";

	/// <summary>
	/// Represents the policy for organization-wide internal access.
	/// </summary>
	/// <remarks>
	/// Grants access to internal organization members and higher roles, including system processes.
	/// Used for operations requiring broader organizational access.
	/// </remarks>
	public const string StandardInternal = "Auth.Standard.Internal";

	/// <summary>
	/// Represents the policy for customer service and support operations.
	/// </summary>
	/// <remarks>
	/// Grants access to service agents and higher roles, including system processes.
	/// Used for customer support and service-related activities.
	/// </remarks>
	public const string StandardAgent = "Auth.Standard.Agent";

	/// <summary>
	/// Represents the policy for management-level access.
	/// </summary>
	/// <remarks>
	/// Grants access to managers and higher roles, including system processes.
	/// Suitable for organizational oversight and approval tasks.
	/// </remarks>
	public const string StandardManager = "Auth.Standard.Manager";

	/// <summary>
	/// Represents the policy for administrative access.
	/// </summary>
	/// <remarks>
	/// Grants access to administrators and system processes only.
	/// Required for system configuration and critical operations.
	/// </remarks>
	public const string StandardAdmin = "Auth.Standard.Admin";

	/// <summary>
	/// Represents the policy exclusively for non-interactive system access.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Restricts access exclusively to system processes with the 
	/// <see cref="ApplicationRoles.AppSystemRole"/> (app:system) role.
	/// </para>
	/// <para>
	/// This policy should only be used for endpoints specifically designed for
	/// daemon/serverless function access, where no user context is expected or required.
	/// </para>
	/// </remarks>
	public const string System = "Auth.System";

	/// <summary>
	/// The list of all default application Authorization Policies.
	/// </summary>
	public static List<string> All { get; } = [
		Standard,
		StandardAdmin,
		StandardAgent,
		StandardInternal,
		StandardManager,
		System
	];

}