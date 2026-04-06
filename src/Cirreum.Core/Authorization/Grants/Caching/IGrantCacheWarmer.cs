namespace Cirreum.Authorization.Grants.Caching;

/// <summary>
/// Hook for proactive grant cache population. Core defines the contract; apps or
/// higher layers (e.g., <c>Cirreum.Services.Server</c>) provide implementations
/// that know how to enumerate callers and domains to pre-resolve grants.
/// </summary>
/// <remarks>
/// <para>
/// Pre-caching is optional. Without it, the first request from a user for a given
/// permission set triggers a cold-path resolution (DB hit); subsequent requests
/// within the cache TTL are served from L2.
/// </para>
/// <para>
/// Typical warm-up strategies: populate on user login, run a background job on
/// startup, or warm after bulk grant mutations.
/// </para>
/// </remarks>
public interface IGrantCacheWarmer {

	/// <summary>
	/// Pre-populates cached reach entries for the specified caller across
	/// applicable domains and permission sets.
	/// </summary>
	/// <param name="callerId">The user ID to warm the cache for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WarmCallerAsync(string callerId, CancellationToken cancellationToken = default);
}
