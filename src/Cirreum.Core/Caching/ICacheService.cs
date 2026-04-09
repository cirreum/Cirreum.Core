namespace Cirreum.Caching;

/// <summary>
/// Platform-level cache-aside abstraction. Provides get-or-create, remove, and
/// tag-based invalidation semantics that any Cirreum subsystem can consume.
/// </summary>
/// <remarks>
/// The active implementation is selected by <see cref="CacheSettings.Provider"/>.
/// Built-in: <see cref="NoCacheService"/> (no-op) and
/// <see cref="InMemoryCacheService"/> (single-instance). Distributed and Hybrid
/// providers are supplied by infrastructure packages.
/// </remarks>
public interface ICacheService {

	/// <summary>
	/// Returns a cached value for <paramref name="cacheKey"/> if it exists;
	/// otherwise invokes <paramref name="factory"/>, caches the result, and returns it.
	/// </summary>
	/// <typeparam name="TResultValue">The type of the cached value.</typeparam>
	/// <param name="cacheKey">Unique key identifying the cache entry.</param>
	/// <param name="factory">Delegate that produces the value on a cache miss.</param>
	/// <param name="settings">Expiration policy for the new entry.</param>
	/// <param name="tags">Optional tags for bulk invalidation via <see cref="RemoveByTagAsync"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached or freshly computed value.</returns>
	ValueTask<TResultValue> GetOrCreateAsync<TResultValue>(
		string cacheKey,
		Func<CancellationToken, ValueTask<TResultValue>> factory,
		CacheExpirationSettings settings,
		string[]? tags = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes a single cache entry by its key.
	/// </summary>
	/// <param name="cacheKey">Unique key identifying the cache entry.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RemoveAsync(
		string cacheKey,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes all cache entries associated with the specified tag.
	/// </summary>
	/// <param name="tag">The tag to match for removal.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RemoveByTagAsync(
		string tag,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes all cache entries associated with one or more of the specified tags.
	/// </summary>
	/// <param name="tags">The collection of tags to match for removal.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RemoveByTagsAsync(
		IEnumerable<string> tags,
		CancellationToken cancellationToken = default);

}