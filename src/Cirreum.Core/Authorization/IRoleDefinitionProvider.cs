namespace Cirreum.Authorization;

/// <summary>
/// Defines roles and their inheritance relationships.
/// </summary>
///
/// <remarks>
/// Role definitions follow a two-step process:
/// <list type="number">
///   <item><description><b>Registration:</b> Declare all roles that should exist via <see cref="Roles"/>.</description></item>
///   <item><description><b>Relationships:</b> Define inheritance between registered roles via <see cref="RoleHierarchy"/>.</description></item>
/// </list>
///
/// <para>Roles not referenced in <see cref="RoleHierarchy"/> are treated as standalone roles with no inheritance.</para>
///
/// <para><b>Application Role Guidelines:</b></para>
/// <list type="bullet">
///   <item><description>Do not recreate or override existing 'app' roles.</description></item>
///   <item><description>You can have a predefined 'app' role inherit from a custom role.</description></item>
///   <item><description>You can define a custom role that inherits from a predefined 'app' role.</description></item>
/// </list>
///
/// <para><b>Role Hierarchy Example:</b></para>
/// <code>
/// Roles: [custom:manager, custom:coordinator, feature:beta]
/// 
/// RoleHierarchy:
/// app:manager (system registered)
///   └── custom:manager
/// app:internal (system registered)
///   └── custom:coordinator  
/// custom:manager
///   └── custom:coordinator
/// custom:coordinator
///   └── app:user (system registered)
/// 
/// Standalone: feature:beta (no inheritance)
/// </code>
///
/// <para><b>Implementation Notes:</b></para>
/// <list type="bullet">
///   <item><description>The runtime will automatically discover, validate, and register your role definitions in <see cref="IAuthorizationRoleRegistry"/>.</description></item>
///   <item><description>All custom roles in <see cref="RoleHierarchy"/> must exist in <see cref="Roles"/>.</description></item>
///   <item><description>Only one implementation of this interface should exist per assembly.</description></item>
///   <item><description>If multiple implementations exist, the last one found will be used.</description></item>
/// </list>
/// </remarks>
public interface IRoleDefinitionProvider {

	/// <summary>
	/// Defines all custom roles that should be registered in the system.
	/// </summary>
	/// <remarks>
	/// This array contains all custom roles that your module/assembly contributes to the system,
	/// including both standalone roles and roles that participate in inheritance hierarchies.
	/// 
	/// <para><b>Include here:</b></para>
	/// <list type="bullet">
	///   <item><description>Standalone roles (e.g., feature flags, API access, temporary permissions)</description></item>
	///   <item><description>Custom roles that will participate in inheritance relationships</description></item>
	/// </list>
	/// 
	/// <para><b>Do NOT include:</b></para>
	/// <list type="bullet">
	///   <item><description>Application roles ('app:*') - these are already registered by the system</description></item>
	/// </list>
	/// 
	/// <para><b>Validation:</b></para>
	/// <list type="bullet">
	///   <item><description>All custom roles referenced in <see cref="RoleHierarchy"/> must be declared here</description></item>
	///   <item><description>Application roles ('app:*') are not allowed in this array and will be filtered out with warnings</description></item>
	///   <item><description>Duplicate role registration across assemblies will result in warnings</description></item>
	/// </list>
	/// </remarks>
	static abstract Role[] Roles { get; }

	/// <summary>
	/// Defines the role inheritance configuration used at runtime.
	/// </summary>
	///
	/// <remarks>
	/// This dictionary maps each role to the roles it inherits from. Roles not included
	/// in this dictionary are treated as standalone roles with no inheritance relationships.
	///
	/// <para><b>Structure:</b></para>
	/// <list type="bullet">
	///   <item><description><b>Key:</b> A <see cref="Role"/> that inherits from other roles.</description></item>
	///   <item><description><b>Value:</b> An array of <see cref="Role"/>(s) that the Key role inherits from.</description></item>
	/// </list>
	///
	/// <para><b>Inheritance Rules:</b></para>
	/// <list type="bullet">
	///   <item><description>Role inheritance is transitive - if A inherits from B, and B inherits from C, then A inherits from C.</description></item>
	///   <item><description>Circular inheritance is not allowed and will be detected during validation.</description></item>
	///   <item><description>All custom roles referenced here (keys and values) must exist in <see cref="Roles"/>.</description></item>
	///   <item><description>Application roles ('app:*') can be referenced but must not be in <see cref="Roles"/>.</description></item>
	/// </list>
	///
	/// <para><b>Empty Dictionary:</b> If no inheritance relationships are needed, return an empty dictionary.
	/// All roles in <see cref="Roles"/> will be treated as standalone.</para>
	/// </remarks>
	static abstract Dictionary<Role, Role[]> RoleHierarchy { get; }

}