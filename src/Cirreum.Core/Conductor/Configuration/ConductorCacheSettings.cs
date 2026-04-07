namespace Cirreum.Conductor.Configuration;

using Cirreum.Caching;

/*
{
  "Cirreum": {
	"Cache": {
	  "Provider": "InMemory",
	  "DefaultExpiration": {
		"Expiration": "00:05:00",
		"LocalExpiration": "00:02:00",
		"FailureExpiration": "00:00:30"
	  }
	},
	"Conductor": {
	  "Cache": {
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
/// Conductor-specific cache overrides. Provider selection and default expirations live
/// in <see cref="CacheSettings"/> (<c>Cirreum:Cache</c>). This section only
/// contains overrides by query category or exact query type name.
/// </summary>
public class ConductorCacheSettings {

	/// <summary>
	/// Specifies the configuration section name.
	/// </summary>
	public const string SectionName = "Cirreum:Conductor:Cache";

	/// <summary>
	/// Cache setting overrides by exact query type name. Use sparingly for specific
	/// queries that need different settings than the global defaults.
	/// </summary>
	/// <example>
	/// "GetCriticalUserQuery": { "Expiration": "00:01:00" }
	/// </example>
	public Dictionary<string, QueryCacheOverride> QueryOverrides { get; set; } = [];
}
