namespace Cirreum.Authorization;

/// <summary>
/// Associates a <see cref="Role"/> with the <see cref="Permission"/>s it grants on a
/// specific resource. Building block for resource-level ACLs.
/// </summary>
/// <remarks>
/// <para>
/// Used on resources to express fine-grained access control. Each grant says:
/// "this role can perform these actions on this resource."
/// </para>
/// <para>
/// Because Cirreum resolves effective roles via hierarchy, you only need to declare
/// the minimum role — inheriting roles automatically receive the same grant.
/// </para>
/// </remarks>
public sealed record PermissionGrant {

	/// <summary>The role this grant applies to.</summary>
	public required Role Role { get; init; }

	/// <summary>The permissions granted to this role on the resource.</summary>
	public required IReadOnlyList<Permission> Permissions { get; init; }

	/// <summary>
	/// Checks whether this grant includes the specified permission.
	/// </summary>
	public bool HasPermission(Permission permission) =>
		this.Permissions.Contains(permission);
}
