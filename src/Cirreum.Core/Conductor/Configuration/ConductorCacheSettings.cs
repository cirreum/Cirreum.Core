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
	/// The cache provider to use. Defaults to None.
	/// - None: Disable caching entirely (safe default)
	/// - InMemory: Single-instance in-memory cache (Blazor WASM, development)
	/// - Distributed: Distributed cache only (Azure Functions, serverless)
	/// - Hybrid: L1 (memory) + L2 (distributed) for multi-instance apps
	/// </summary>
	public CacheProvider Provider { get; set; } = CacheProvider.None;

	/// <summary>
	/// Default expiration time for cached query results.
	/// - InMemory: Controls memory cache expiration
	/// - Distributed: Controls distributed cache (L2) expiration
	/// - Hybrid: Controls distributed cache (L2) expiration
	/// If not specified, provider defaults are used.
	/// </summary>
	public TimeSpan? DefaultExpiration { get; set; }

	/// <summary>
	/// Default expiration time for local in-memory cache (L1) when using Hybrid provider.
	/// Ignored for InMemory and Distributed providers.
	/// If not specified, uses <see cref="DefaultExpiration"/>.
	/// </summary>
	public TimeSpan? DefaultLocalExpiration { get; set; }

	/// <summary>
	/// Default expiration time for failed query results.
	/// Should be shorter than <see cref="DefaultExpiration"/> to allow faster retry.
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
