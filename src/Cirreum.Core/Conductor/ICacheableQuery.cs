namespace Cirreum.Conductor;

// ===== Cacheable Query Requests =====

/// <summary>
/// Represents a query that can be cached.
/// </summary>
/// <typeparam name="TResponse">
/// The response type. Must be immutable for safe caching with instance reuse.
/// Use sealed records with init-only properties and mark with [ImmutableObject(true)].
/// </typeparam>
public interface ICacheableQuery<out TResponse> : IRequest<TResponse> {

	/// <summary>
	/// The unique cache key for this query instance.
	/// </summary>
	string CacheKey { get; }

	/// <summary>
	/// Cache expiration settings for this query. Values specified here can be overridden by
	/// configuration at runtime. If not specified, uses global defaults.
	/// </summary>
	QueryCacheSettings Cache => new();

	/// <summary>
	/// Tags for cache invalidation. Cannot be overridden by configuration.
	/// Tags enable bulk invalidation of related cache entries and define the
	/// domain relationships of the cached data.
	/// </summary>
	/// <example>
	/// ["users", $"user:{UserId}"] - Can invalidate all users or specific user
	/// ["orders", "tenant:123"] - Can invalidate by entity type or tenant
	/// </example>
	string[]? CacheTags => null;

	/// <summary>
	/// Cache category for configuration grouping. This allows DevOps to configure
	/// cache settings for groups of related queries without knowing specific query names.
	/// </summary>
	/// <remarks>
	/// Common categories: "users", "orders", "reports", "analytics", "reference-data", "real-time"
	/// If null, only uses default settings or exact query name overrides.
	/// </remarks>
	string? CacheCategory => null;
}