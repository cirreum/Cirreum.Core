namespace Cirreum.Caching;

/// <summary>
/// Scoped-per-request context for cache key composition. Upstream pipeline stages
/// (e.g., grant evaluation) stamp a prefix and extra tags that the
/// <c>QueryCaching</c> intercept prepends to the request's cache key and tags at cache time.
/// </summary>
/// <remarks>
/// <para>
/// When no prefix is set, <c>QueryCaching</c> uses the request's key and tags as-is.
/// This keeps non-grant queries unaffected while allowing the authorization layer to
/// inject owner/boundary isolation without coupling Conductor to Authorization.
/// </para>
/// </remarks>
public sealed class CacheKeyContext {

	/// <summary>
	/// A prefix prepended to the request's cache key
	/// to produce the final cache key. <see langword="null"/> when no prefix is needed.
	/// </summary>
	public string? KeyPrefix { get; private set; }

	/// <summary>
	/// Additional tags merged with the request's cache tags
	/// at cache time. <see langword="null"/> when no extra tags are needed.
	/// </summary>
	public string[]? ExtraTags { get; private set; }

	/// <summary>
	/// Stamps the cache key prefix. Called by upstream pipeline stages (e.g., grant evaluator).
	/// </summary>
	public void SetPrefix(string prefix) => this.KeyPrefix = prefix;

	/// <summary>
	/// Stamps additional cache tags. Called by upstream pipeline stages (e.g., grant evaluator).
	/// </summary>
	public void SetExtraTags(string[] tags) => this.ExtraTags = tags;
}
