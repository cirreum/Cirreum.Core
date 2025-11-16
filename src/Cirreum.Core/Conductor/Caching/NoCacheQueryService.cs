namespace Cirreum.Conductor.Caching;

using Cirreum.Conductor;

/// <summary>
/// No-op cache service that always executes queries without caching.
/// Useful for testing or when caching should be disabled.
/// </summary>
public class NoCacheQueryService : ICacheableQueryService {
	public async ValueTask<TResponse> GetOrCreateAsync<TResponse>(
		string cacheKey,
		Func<CancellationToken, ValueTask<TResponse>> factory,
		QueryCacheSettings settings,
		string[]? tags = null,
		CancellationToken cancellationToken = default) {
		// Always execute, never cache
		return await factory(cancellationToken);
	}

	public ValueTask RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}