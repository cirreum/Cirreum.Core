namespace Cirreum.Conductor.Configuration;

/// <summary>
/// Cache setting overrides for a specific query type or category.
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
