namespace Cirreum.Conductor.Caching;

using Cirreum.Conductor;
using System.Collections.Concurrent;

/// <summary>
/// In-memory implementation of <see cref="ICacheableQueryService"/> for development and testing.
/// Does not support distributed caching or expiration - entries remain until manually removed or app restart.
/// </summary>
public class InMemoryCacheableQueryService : ICacheableQueryService {
	private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

	public async ValueTask<TResponse> GetOrCreateAsync<TResponse>(
		string cacheKey,
		Func<CancellationToken, ValueTask<TResponse>> factory,
		QueryCacheSettings settings,
		string[]? tags = null,
		CancellationToken cancellationToken = default) {

		// Check if exists and not expired
		if (this._cache.TryGetValue(cacheKey, out var existing) && !existing.IsExpired) {
			return (TResponse)existing.Value;
		}

		// Create new entry
		var value = await factory(cancellationToken);
		var expiration = CalculateExpiration(value, settings);

		var entry = new CacheEntry(value!, expiration, tags);
		this._cache[cacheKey] = entry;

		return value;
	}

	public ValueTask RemoveAsync(string cacheKey, CancellationToken cancellationToken = default) {
		this._cache.TryRemove(cacheKey, out _);
		return ValueTask.CompletedTask;
	}

	public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) {
		var keysToRemove = this._cache
			.Where(kvp => kvp.Value.Tags?.Contains(tag) == true)
			.Select(kvp => kvp.Key)
			.ToList();

		foreach (var key in keysToRemove) {
			this._cache.TryRemove(key, out _);
		}

		return ValueTask.CompletedTask;
	}

	public async ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default) {
		foreach (var tag in tags) {
			await this.RemoveByTagAsync(tag, cancellationToken);
		}
	}

	private static DateTime? CalculateExpiration<TResponse>(TResponse value, QueryCacheSettings settings) {
		// Check if it's a failed result
		if (value is IResult { IsSuccess: false } && settings.FailureExpiration.HasValue) {
			return DateTime.UtcNow.Add(settings.FailureExpiration.Value);
		}

		// Use standard expiration (ignoring LocalExpiration for in-memory)
		if (settings.Expiration.HasValue) {
			return DateTime.UtcNow.Add(settings.Expiration.Value);
		}

		return null; // No expiration
	}

	private sealed class CacheEntry(object value, DateTime? expiresAt, string[]? tags) {

		public object Value { get; } = value;

		public DateTime? ExpiresAt { get; } = expiresAt;

		public string[]? Tags { get; } = tags;

		public bool IsExpired => this.ExpiresAt.HasValue && DateTime.UtcNow >= this.ExpiresAt.Value;

	}

}