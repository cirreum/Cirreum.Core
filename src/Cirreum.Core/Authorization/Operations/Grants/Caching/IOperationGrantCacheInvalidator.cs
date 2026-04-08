namespace Cirreum.Authorization.Operations.Grants.Caching;

/// <summary>
/// Invalidates cached reach entries. Apps call this after grant mutations
/// (grant/revoke operations) to ensure subsequent requests resolve fresh
/// from the database.
/// </summary>
/// <remarks>
/// <para>
/// Invalidation is tag-based: each cached reach entry is tagged with the caller ID
/// and domain feature. Invalidating a caller removes all entries for that user
/// across all domains; invalidating a domain removes all entries for that domain
/// across all users.
/// </para>
/// </remarks>
public interface IOperationGrantCacheInvalidator {

	/// <summary>
	/// Invalidates all cached reach entries for a specific caller. Call this after
	/// granting or revoking permissions for a user.
	/// </summary>
	/// <param name="callerId">The user ID whose cached reach entries should be removed.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask InvalidateCallerAsync(string callerId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Invalidates all cached reach entries for a specific domain feature. Call this after
	/// domain-wide permission structure changes.
	/// </summary>
	/// <param name="domainFeature">The domain feature name (e.g., <c>"issues"</c>).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask InvalidateDomainAsync(string domainFeature, CancellationToken cancellationToken = default);
}
