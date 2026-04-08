namespace Cirreum.Authorization.Resources;

/// <summary>
/// Marker for domain objects that carry an embedded Access Control List (ACL).
/// Objects implementing this interface can be evaluated by <see cref="IResourceAccessEvaluator"/>
/// for object-level permission checks.
/// </summary>
/// <remarks>
/// <para>
/// This interface is orthogonal to <see cref="IAuthorizableObject"/> — a type may implement
/// both (operation-level + object-level auth) or either independently.
/// </para>
/// <para>
/// When <see cref="InheritPermissions"/> is <see langword="true"/>, the evaluator walks up
/// the hierarchy via <see cref="IAccessEntryProvider{T}.GetParentId"/> and merges ancestor
/// ACLs into the effective access. Setting it to <see langword="false"/> breaks inheritance,
/// making this resource's <see cref="AccessList"/> the sole source of permissions.
/// </para>
/// </remarks>
public interface IProtectedResource {

	/// <summary>
	/// The unique identifier for this resource within its hierarchy.
	/// <see langword="null"/> indicates a transient or unsaved resource that
	/// inherits root defaults exclusively.
	/// </summary>
	string? ResourceId { get; }

	/// <summary>
	/// The Access Control List (ACL) embedded on this resource — the set of
	/// <see cref="AccessEntry"/> records that grant roles specific permissions.
	/// </summary>
	IReadOnlyList<AccessEntry> AccessList { get; }

	/// <summary>
	/// When <see langword="true"/>, permissions from ancestor resources are merged
	/// into this resource's effective access. When <see langword="false"/>, only
	/// this resource's own <see cref="AccessList"/> applies.
	/// </summary>
	bool InheritPermissions { get; }
}
