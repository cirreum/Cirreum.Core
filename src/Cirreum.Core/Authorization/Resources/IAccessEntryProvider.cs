namespace Cirreum.Authorization.Resources;

/// <summary>
/// Infrastructure contract that loads protected resources and navigates
/// the permission hierarchy. The framework calls these methods during hierarchy walking
/// in <see cref="ResourceAccessEvaluator"/>.
/// </summary>
/// <typeparam name="T">The protected resource type (e.g., <c>DocumentFolder</c>).</typeparam>
/// <remarks>
/// <para>
/// Only <see cref="GetByIdAsync"/> is required. <see cref="GetParentId"/> and
/// <see cref="RootDefaults"/> default to the entity's own <see cref="IProtectedResource"/>
/// declarations (<see cref="IProtectedResource.ParentResourceId"/> and
/// <see cref="IProtectedResource.RootDefaults"/>). Override them only when the hierarchy
/// is not expressed directly on the entity.
/// </para>
/// <para>
/// When using <c>Cirreum.Persistence.Azure</c>, a default implementation is auto-registered
/// via <c>DefaultAccessEntryProvider&lt;T&gt;</c> — no manual implementation is needed.
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
	/// Batch-loads multiple resources by their <see cref="IProtectedResource.ResourceId"/>.
	/// Used by the evaluator when <see cref="IProtectedResource.AncestorResourceIds"/> is
	/// populated, enabling O(1) batch hierarchy resolution instead of O(depth) sequential reads.
	/// </summary>
	/// <remarks>
	/// The default implementation falls back to sequential <see cref="GetByIdAsync"/> calls.
	/// Persistence-backed providers should override with a batch read (e.g., Cosmos ReadMany).
	/// Missing resources are silently excluded from the result (orphan tolerance).
	/// </remarks>
	async ValueTask<IReadOnlyList<T>> GetManyByIdAsync(
		IReadOnlyList<string> resourceIds,
		CancellationToken cancellationToken) {

		var results = new List<T>(resourceIds.Count);
		foreach (var id in resourceIds) {
			var resource = await this.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
			if (resource is not null) {
				results.Add(resource);
			}
		}
		return results;
	}

	/// <summary>
	/// Returns the <see cref="IProtectedResource.ResourceId"/> of the parent resource,
	/// or <see langword="null"/> when <paramref name="resource"/> is at the root of the hierarchy.
	/// Defaults to <see cref="IProtectedResource.ParentResourceId"/>.
	/// </summary>
	/// <param name="resource">The resource whose parent ID should be resolved.</param>
	/// <returns>The parent's resource ID, or <see langword="null"/> for root resources.</returns>
	string? GetParentId(T resource) => resource.ParentResourceId;

	/// <summary>
	/// The default ACL applied at the root of the hierarchy. When the hierarchy walk reaches
	/// a root resource (no parent), these entries are merged into the effective access.
	/// Defaults to <see cref="IProtectedResource.RootDefaults"/> on <typeparamref name="T"/>.
	/// </summary>
	IReadOnlyList<AccessEntry> RootDefaults => T.RootDefaults;

}