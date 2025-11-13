namespace Cirreum.Conductor.Tests {
	using Cirreum.Conductor.Configuration;

	public static class Shared {

		public static ConductorSettings SequentialSettings { get; } = new ConductorSettings {
			PublisherStrategy = PublisherStrategy.Sequential,
			Cache = new ConductorCacheSettings {
				Provider = CacheProvider.InMemory,
				DefaultExpiration = TimeSpan.FromMinutes(5)
			}
		};

		public static ConductorSettings FireAndForgetSettings { get; } = new ConductorSettings {
			PublisherStrategy = PublisherStrategy.FireAndForget,
			Cache = new ConductorCacheSettings {
				Provider = CacheProvider.InMemory,
				DefaultExpiration = TimeSpan.FromMinutes(5)
			}
		};


		public static ConductorSettings ParallelSettings { get; } = new ConductorSettings {
			PublisherStrategy = PublisherStrategy.Parallel,
			Cache = new ConductorCacheSettings {
				Provider = CacheProvider.InMemory,
				DefaultExpiration = TimeSpan.FromMinutes(5)
			}
		};

	}
}
