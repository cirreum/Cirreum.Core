namespace Cirreum.Authorization.Resources;

/// <summary>
/// A single Access Control Entry (ACE): binds a <see cref="Authorization.Role"/> to one or more
/// <see cref="Permission"/>s on a protected resource. The set of <see cref="AccessEntry"/>
/// records on a resource forms its Access Control List (ACL).
/// </summary>
/// <remarks>
/// <para>
/// Each entry answers: <em>"holders of this role may perform these operations on this resource."</em>
/// A resource's full ACL is the union of all its entries — if any entry grants a permission
/// for a caller's effective role, the caller has that permission.
/// </para>
/// </remarks>
public sealed record AccessEntry {

	/// <summary>
	/// The role this entry applies to. Callers whose effective roles include this role
	/// are granted the listed <see cref="Permissions"/>.
	/// </summary>
	public required Role Role { get; init; }

	/// <summary>
	/// The permissions granted to holders of <see cref="Role"/> on the owning resource.
	/// </summary>
	public required IReadOnlyList<Permission> Permissions { get; init; }

	/// <summary>
	/// Returns <see langword="true"/> if this entry grants the specified <paramref name="permission"/>.
	/// </summary>
	public bool HasPermission(Permission permission) {
		for (var i = 0; i < this.Permissions.Count; i++) {
			if (this.Permissions[i] == permission) {
				return true;
			}
		}
		return false;
	}
}
