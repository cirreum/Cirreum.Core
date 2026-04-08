namespace Cirreum.Authorization.Resources;

/// <summary>
/// App-implemented infrastructure contract that loads protected resources and navigates
/// the permission hierarchy. The framework calls these methods during hierarchy walking
/// in <see cref="ResourceAccessEvaluator"/>.
/// </summary>
/// <typeparam name="T">The protected resource type (e.g., <c>DocumentFolder</c>).</typeparam>
/// <remarks>
/// <para>
/// Implement this interface in the persistence/infrastructure layer — it needs database
/// access to load resources by ID and resolve parent relationships.
/// </para>
/// <para>
/// <see cref="RootDefaults"/> provides the fallback ACL applied when the hierarchy walk
/// reaches the root (no parent). This is typically the organization-wide default permissions.
/// </para>
/// </remarks>
public interface IAccessEntryProvider<T> where T : IProtectedResource {

	/// <summary>
	/// Loads a resource by its <see cref="IProtectedResource.ResourceId"/>.
	/// Returns <see langword="null"/> when the resource does not exist (orphan detection).
	/// </summary>
	/// <param name="resourceId">The resource identifier to look up.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The resource if found; otherwise <see langword="null"/>.</returns>
	ValueTask<T?> GetByIdAsync(string resourceId, CancellationToken cancellationToken);

	/// <summary>
	/// Returns the <see cref="IProtectedResource.ResourceId"/> of the parent resource,
	/// or <see langword="null"/> when <paramref name="resource"/> is at the root of the hierarchy.
	/// </summary>
	/// <param name="resource">The resource whose parent ID should be resolved.</param>
	/// <returns>The parent's resource ID, or <see langword="null"/> for root resources.</returns>
	string? GetParentId(T resource);

	/// <summary>
	/// The default ACL applied at the root of the hierarchy. When the hierarchy walk reaches
	/// a root resource (no parent), these entries are merged into the effective access.
	/// </summary>
	IReadOnlyList<AccessEntry> RootDefaults { get; }
}
