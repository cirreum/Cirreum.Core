namespace Cirreum.Conductor;

/// <summary>
/// Expiration policy for a cache entry. Used by both the query caching intercept
/// (for <see cref="ICacheableQuery{TResponse}"/> results) and the grant reach cache
/// (for <see cref="Cirreum.Authorization.Grants.AccessReach"/> entries).
/// </summary>
/// <param name="Expiration">
/// The absolute expiration duration for the cache entry. Applied to distributed (L2)
/// cache when present. If <see langword="null"/>, the caching service's default is used.
/// </param>
/// <param name="LocalExpiration">
/// The absolute expiration duration for the local (L1) in-memory cache. If
/// <see langword="null"/>, falls back to <paramref name="Expiration"/>. Set shorter
/// than <paramref name="Expiration"/> to reduce memory pressure or ensure fresher
/// data locally while still benefiting from L2.
/// </param>
/// <param name="FailureExpiration">
/// The expiration duration for cache entries storing failed results (<see cref="Cirreum.Result"/>
/// with <c>IsSuccess = false</c>). Should be shorter than <paramref name="Expiration"/>
/// to avoid caching transient failures for too long. If <see langword="null"/>, uses
/// the standard <paramref name="Expiration"/>.
/// </param>
public sealed record CacheExpirationSettings(
	TimeSpan? Expiration = null,
	TimeSpan? LocalExpiration = null,
	TimeSpan? FailureExpiration = null
);