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
	/// The <see cref="ResourceId"/> of the parent resource in the permission hierarchy,
	/// or <see langword="null"/> when this resource is at the root.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The evaluator uses this to walk up the hierarchy when <see cref="InheritPermissions"/>
	/// is <see langword="true"/>, merging ancestor ACLs into the effective access.
	/// </para>
	/// <para>
	/// Defaults to <see langword="null"/> (root resource). Override to point to the parent —
	/// for example, a document might return its folder ID.
	/// </para>
	/// </remarks>
	string? ParentResourceId => null;

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

	/// <summary>
	/// Materialized list of ancestor <see cref="ResourceId"/> values, ordered nearest-to-farthest
	/// (e.g., <c>["parent-id", "grandparent-id", "root-id"]</c>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// When non-empty, the evaluator batch-loads all ancestors in a single call instead of
	/// walking one-by-one, reducing hierarchy resolution from O(depth) sequential reads to
	/// a single batch read.
	/// </para>
	/// <para>
	/// Defaults to an empty list (opt-in). Entities that populate this property gain O(1)
	/// batch permission evaluation. The <c>Cirreum.Persistence.Azure</c> persistence layer
	/// auto-maintains this property on create and move operations.
	/// </para>
	/// </remarks>
	IReadOnlyList<string> AncestorResourceIds => [];

	/// <summary>
	/// The default ACL applied at the root of the hierarchy. When the hierarchy walk reaches
	/// a root resource (no parent), these entries are merged into the effective access.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is typically the organization-wide default permissions — for example, granting
	/// admin roles full access and regular users read-only access.
	/// </para>
	/// <para>
	/// Defaults to an empty list (no root-level grants). Override on the implementing type
	/// to declare root defaults using strongly-typed <see cref="Role"/> and
	/// <see cref="Permission"/> constants:
	/// </para>
	/// <code>
	/// public static IReadOnlyList&lt;AccessEntry&gt; RootDefaults => [
	///     new AccessEntry(MyRoles.Admin, [MyPermissions.Documents.Read, MyPermissions.Documents.Write]),
	///     new AccessEntry(MyRoles.User, [MyPermissions.Documents.Read]),
	/// ];
	/// </code>
	/// </remarks>
	static virtual IReadOnlyList<AccessEntry> RootDefaults => [];

}