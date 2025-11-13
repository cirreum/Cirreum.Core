namespace Cirreum.Conductor.Intercepts;

/// <summary>
/// Represents the settings for a cache entry, including its expiration policy and optional tags.
/// </summary>
/// <param name="Expiration">
/// The absolute expiration time for distributed (L2) cache. If specified,
/// the cache entry will expire after the given duration in the distributed cache.
/// If null, uses the default expiration configured in HybridCache.
/// </param>
/// <param name="LocalExpiration">
/// The absolute expiration time for local (L1) in-memory cache. If null, uses <paramref name="Expiration"/>.
/// Set this shorter than <paramref name="Expiration"/> to reduce memory usage or ensure fresher data locally.
/// </param>
/// <param name="FailureExpiration">
/// The absolute expiration time for failed query results. Should be shorter than 
/// <paramref name="Expiration"/>. If null, uses the standard expiration.
/// This prevents failed queries from being cached too long during transient failures.
/// </param>
public sealed record QueryCacheSettings(
	TimeSpan? Expiration = null,
	TimeSpan? LocalExpiration = null,
	TimeSpan? FailureExpiration = null
);