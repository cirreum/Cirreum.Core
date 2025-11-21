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
			"FailureExpiration": "00:00:10"
		  }
		}

		// explicit overrides
		"QueryOverrides": {
		  "GetCriticalUserDataQuery": {
			"Expiration": "00:00:30"
			"FailureExpiration": "00:00:10"
		  }
		}
	  }
	}
  }
}
*/

/// <summary>
/// Configuration settings for Conductor behavior.
/// </summary>
public class ConductorSettings {
	/// <summary>
	/// Specifies the configuration section name for Conductor settings.
	/// </summary>
	public const string SectionName = "Cirreum:Conductor";

	/// <summary>
	/// The strategy used for publishing domain events. Defaults to Sequential.
	/// </summary>
	public PublisherStrategy PublisherStrategy { get; set; } = PublisherStrategy.Sequential;

	/// <summary>
	/// Cache configuration for cacheable queries.
	/// </summary>
	public ConductorCacheSettings Cache { get; set; } = new();
}