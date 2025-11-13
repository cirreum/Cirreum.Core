namespace Cirreum.Conductor.Configuration;
/* 
{
  "Cirreum": {
	"Conductor": {
	  "PublisherStrategy": "Sequential", // Sequential, FailFast, Parallel and FireAndForget
	  "Cache": {
		"Provider": "InMemory", // None, InMemory, HybridCache
		"DefaultExpiration": "00:05:00",
		"DefaultLocalExpiration": "00:05:00",
		"DefaultFailureExpiration": "00:00:30",
		
		"CategoryOverrides": {
		  "users": {
			"Expiration": "00:10:00",
			"LocalExpiration": "00:02:00"
		  },
		  "reports": {
			"Expiration": "01:00:00",
			"LocalExpiration": "00:05:00"
		  },
		  "analytics": {
			"Expiration": "00:15:00"
		  },
		  "reference-data": {
			"Expiration": "24:00:00",
			"LocalExpiration": "01:00:00"
		  },
		  "real-time": {
			"Expiration": "00:01:00"
		  }
		},
		
		"QueryOverrides": {
		  "GetCriticalUserDataQuery": {
			"Expiration": "00:00:30"
		  }
		}
	  }
	}
  }
}
 
 */

/// <summary>
/// Configuration settings for Conductor caching behavior.
/// </summary>
public class ConductorCacheSettings {

	/// <summary>
	/// Specifies the configuration section name for cache settings used by the Cirreum Conductor.
	/// </summary>
	public const string SectionName = "Cirreum:Conductor:Cache";

	/// <summary>
	/// The cache provider to use. Defaults to HybridCache.
	/// Use 'None' to disable caching, 'InMemory' for development, or 'HybridCache' for production.
	/// </summary>
	public CacheProvider Provider { get; set; } = CacheProvider.HybridCache;

	/// <summary>
	/// Default expiration time for successful query results in distributed (L2) cache.
	/// If not specified, HybridCache's default is used.
	/// </summary>
	public TimeSpan? DefaultExpiration { get; set; }

	/// <summary>
	/// Default expiration time for successful query results in local (L1) in-memory cache.
	/// If not specified, uses <see cref="DefaultExpiration"/>.
	/// </summary>
	public TimeSpan? DefaultLocalExpiration { get; set; }

	/// <summary>
	/// Default expiration time for failed query results.
	/// Should be shorter than <see cref="DefaultExpiration"/>.
	/// </summary>
	public TimeSpan? DefaultFailureExpiration { get; set; }

	/// <summary>
	/// Cache setting overrides by category. Categories are defined by developers
	/// in query implementations and represent logical groupings (e.g., "reports", "analytics").
	/// </summary>
	/// <example>
	/// "reports": { "Expiration": "01:00:00" }
	/// "reference-data": { "Expiration": "24:00:00" }
	/// </example>
	public Dictionary<string, QueryCacheOverride> CategoryOverrides { get; set; } = [];

	/// <summary>
	/// Cache setting overrides by exact query type name. Takes precedence over category overrides.
	/// Use sparingly for specific queries that need different settings than their category.
	/// </summary>
	/// <example>
	/// "GetCriticalUserQuery": { "Expiration": "00:01:00" }
	/// </example>
	public Dictionary<string, QueryCacheOverride> QueryOverrides { get; set; } = [];
}

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