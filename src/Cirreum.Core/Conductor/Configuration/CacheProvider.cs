namespace Cirreum.Conductor.Configuration;

/// <summary>
/// Cache provider types for Conductor queries.
/// </summary>
public enum CacheProvider {
	/// <summary>
	/// No caching - queries always execute.
	/// </summary>
	None,

	/// <summary>
	/// Simple in-memory cache for development/testing. Not distributed.
	/// </summary>
	InMemory,

	/// <summary>
	/// HybridCache with L1 (in-memory) and L2 (distributed) tiers. Recommended for production.
	/// </summary>
	HybridCache
}