namespace Cirreum.Conductor.Caching;

using Cirreum.Conductor;

/// <summary>
/// Provides caching services specifically for <see cref="ICacheableQuery{TResponse}"/> instances
/// within the Conductor pipeline.
/// </summary>
/// <remarks>
/// This service is designed to work with the Conductor's caching interceptor and should not be
/// confused with general application caching services. It handles cache key management and
/// expiration settings for query results.
/// </remarks>
public interface ICacheableQueryService {

	/// <summary>
	/// Retrieves a cached query result if it exists, or creates and caches it using the provided factory.
	/// </summary>
	/// <typeparam name="TResponse">The type of the query response.</typeparam>
	/// <param name="cacheKey">The unique cache key for the query.</param>
	/// <param name="factory">A delegate that produces the value if it's not cached.</param>
	/// <param name="settings">Cache expiration settings.</param>
	/// <param name="tags">Optional tags to associate with the cache entry.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached or newly created response.</returns>
	ValueTask<TResponse> GetOrCreateAsync<TResponse>(
		string cacheKey,
		Func<CancellationToken, ValueTask<TResponse>> factory,
		QueryCacheSettings settings,
		string[]? tags = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes a cached query result.
	/// </summary>
	/// <param name="cacheKey">The unique cache key for the query.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RemoveAsync(
		string cacheKey,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes all cached query results that contain the specified tag.
	/// </summary>
	/// <param name="tag">The tag to match for removal.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RemoveByTagAsync(
		string tag,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes all cached query results that contain one or more of the specified tags.
	/// </summary>
	/// <param name="tags">The collection of tags to match for removal.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RemoveByTagsAsync(
		IEnumerable<string> tags,
		CancellationToken cancellationToken = default);

}