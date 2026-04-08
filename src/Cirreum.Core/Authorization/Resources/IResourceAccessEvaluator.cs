namespace Cirreum.Authorization.Resources;

/// <summary>
/// Framework service for evaluating object-level permissions on protected resources.
/// Handlers inject this to perform in-handler authorization checks against the resource's
/// embedded ACL (and its ancestor chain when inheritance is enabled).
/// </summary>
/// <remarks>
/// <para>
/// This is the <em>data-time</em> counterpart to the <em>request-time</em> pipeline
/// (<see cref="IAuthorizationEvaluator"/>). The pipeline gates operation access before the
/// handler runs; this service checks permissions on specific data objects after they are loaded.
/// </para>
/// <para>
/// Two usage patterns:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Check</b> — single resource, returns <see cref="Result"/> (commands, single lookups).
///   </description></item>
///   <item><description>
///     <b>Filter</b> — batch filtering, returns only authorized resources (queries, listings).
///   </description></item>
/// </list>
/// </remarks>
public interface IResourceAccessEvaluator {

	/// <summary>
	/// Checks whether the current caller has <paramref name="permission"/> on the given
	/// <paramref name="resource"/>.
	/// </summary>
	/// <typeparam name="T">The protected resource type.</typeparam>
	/// <param name="resource">The resource to check (its ACL is evaluated directly).</param>
	/// <param name="permission">The required permission.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// <see cref="Result.Success"/> when authorized;
	/// a failed <see cref="Result"/> with <see cref="DenyCodes.ResourceAccessDenied"/> when denied.
	/// </returns>
	ValueTask<Result> CheckAsync<T>(
		T resource,
		Permission permission,
		CancellationToken cancellationToken = default)
		where T : IProtectedResource;

	/// <summary>
	/// Checks whether the current caller has <paramref name="permission"/> on the resource
	/// identified by <paramref name="resourceId"/>. When <paramref name="resourceId"/> is
	/// <see langword="null"/>, root defaults from
	/// <see cref="IAccessEntryProvider{T}.RootDefaults"/> are used.
	/// </summary>
	/// <typeparam name="T">The protected resource type.</typeparam>
	/// <param name="resourceId">
	/// The resource identifier, or <see langword="null"/> for root-level checks.
	/// </param>
	/// <param name="permission">The required permission.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// <see cref="Result.Success"/> when authorized;
	/// a failed <see cref="Result"/> with <see cref="DenyCodes.ResourceNotFound"/> when the
	/// resource does not exist;
	/// a failed <see cref="Result"/> with <see cref="DenyCodes.ResourceAccessDenied"/> when denied.
	/// </returns>
	ValueTask<Result> CheckAsync<T>(
		string? resourceId,
		Permission permission,
		CancellationToken cancellationToken = default)
		where T : IProtectedResource;

	/// <summary>
	/// Filters a collection of resources, returning only those for which the current caller
	/// has <paramref name="permission"/>.
	/// </summary>
	/// <typeparam name="T">The protected resource type.</typeparam>
	/// <param name="resources">The resources to evaluate.</param>
	/// <param name="permission">The required permission.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A list containing only the authorized resources (preserves input order).</returns>
	ValueTask<IReadOnlyList<T>> FilterAsync<T>(
		IEnumerable<T> resources,
		Permission permission,
		CancellationToken cancellationToken = default)
		where T : IProtectedResource;
}
