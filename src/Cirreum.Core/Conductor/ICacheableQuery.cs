namespace Cirreum.Conductor;

using Cirreum.Caching;

// ===== Cacheable Query Requests =====

/// <summary>
/// Non-generic marker for cacheable queries. Allows runtime detection of cacheability
/// without knowing the response type (e.g., in the grant evaluator).
/// </summary>
public interface ICacheableQuery;

/// <summary>
/// Represents a query that can be cached.
/// </summary>
/// <typeparam name="TResultValue">
/// The response type. Must be immutable for safe caching with instance reuse.
/// Use sealed records with init-only properties and mark with [ImmutableObject(true)].
/// </typeparam>
public interface ICacheableQuery<out TResultValue> : ICacheableQuery, IOperation<TResultValue> {

	/// <summary>
	/// The unique cache key for this query instance.
	/// </summary>
	string CacheKey { get; }

	/// <summary>
	/// Expiration policy for this query's cache entries. Values specified here serve as
	/// code-level defaults and can be overridden by configuration at runtime (category
	/// overrides, query-specific overrides). If not specified, inherits from
	/// <see cref="CacheSettings.DefaultExpiration"/>.
	/// </summary>
	CacheExpirationSettings CacheExpiration => new();

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

}