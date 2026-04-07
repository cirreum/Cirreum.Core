namespace Cirreum.Caching;

/// <summary>
/// Expiration overrides that can be applied to a group of cache entries
/// (e.g., by category or by specific key pattern) via configuration.
/// </summary>
public class QueryCacheOverride {
	/// <summary>
	/// Override for distributed (L2) cache expiration.
	/// </summary>
	public TimeSpan? Expiration { get; set; }

	/// <summary>
	/// Override for local (L1) in-memory cache expiration.
	/// </summary>
	public TimeSpan? LocalExpiration { get; set; }

	/// <summary>
	/// Override for failure expiration.
	/// </summary>
	public TimeSpan? FailureExpiration { get; set; }
}
