namespace Cirreum;

using Cirreum.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

/// <summary>
/// Extension methods for registering Cirreum's centralized cache infrastructure.
/// </summary>
public static class CacheServiceCollectionExtensions {

	/// <summary>
	/// Registers the centralized <see cref="CacheSettings"/> and the
	/// <see cref="ICacheService"/> implementation selected by
	/// <see cref="CacheSettings.Provider"/>. Idempotent — safe to call
	/// from multiple subsystem registrations.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
	/// <param name="configuration">
	/// Optional <see cref="IConfiguration"/> used to bind settings from the
	/// <c>Cirreum:Cache</c> section.
	/// </param>
	/// <param name="configureCaching">
	/// Optional delegate to override cache settings beyond what configuration provides.
	/// Applied after binding from <paramref name="configuration"/>.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddCirreumCaching(
		this IServiceCollection services,
		IConfiguration? configuration = null,
		Action<CacheSettings>? configureCaching = null) {

		ArgumentNullException.ThrowIfNull(services);

		// Idempotent — only register once
		if (services.Any(sd => sd.ServiceType == typeof(CacheSettings))) {
			return services;
		}

		var settings = new CacheSettings();

		if (configuration is not null) {
			var section = configuration.GetSection(CacheSettings.SectionPath);
			section.Bind(settings);
		}

		configureCaching?.Invoke(settings);
		services.AddSingleton(settings);

		// Scoped cache key context for upstream pipeline stages (e.g., grant evaluation)
		// to stamp key prefixes and extra tags consumed by QueryCaching.
		services.TryAddScoped<CacheKeyContext>();

		// Register the cache service based on the provider
		AddCacheableQueryService(services, settings);

		// Wrap the concrete ICacheService with the telemetry decorator.
		// By this point the app has already had a chance to register its
		// infra cache implementation (e.g., Hybrid, Distributed), and
		// AddCacheableQueryService above has applied the fallback.
		DecorateWithInstrumentation(services);

		return services;
	}

	private static void AddCacheableQueryService(
		IServiceCollection services,
		CacheSettings settings) {

		// Use Replace for None/InMemory to enforce config over code registration.
		// Use TryAdd for Distributed/Hybrid to allow infrastructure packages to
		// provide their own implementation.
		switch (settings.Provider) {
			case CacheProvider.None:
				services.Replace(ServiceDescriptor.Singleton<ICacheService, NoCacheService>());
				break;

			case CacheProvider.InMemory:
				services.Replace(ServiceDescriptor.Singleton<ICacheService, InMemoryCacheService>());
				break;

			case CacheProvider.Distributed:
			case CacheProvider.Hybrid:
				// Infrastructure packages (Cirreum.QueryCache.Distributed, Cirreum.QueryCache.Hybrid)
				// register the real implementation. Fall back to NoCacheService if not registered.
				services.TryAddSingleton<ICacheService, NoCacheService>();
				break;
		}
	}

	/// <summary>
	/// Replaces the current <see cref="ICacheService"/> descriptor with a factory
	/// that wraps the concrete implementation in <see cref="InstrumentedCacheService"/>.
	/// Skips wrapping <see cref="NoCacheService"/> — there's no cache to observe.
	/// </summary>
	private static void DecorateWithInstrumentation(IServiceCollection services) {
		var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICacheService));
		if (descriptor is null) {
			return;
		}

		services.Remove(descriptor);

		services.Add(new ServiceDescriptor(
			typeof(ICacheService),
			sp => {
				var inner = ResolveFromDescriptor(sp, descriptor);
				return inner is NoCacheService
					? inner
					: new InstrumentedCacheService(inner);
			},
			descriptor.Lifetime));
	}

	private static ICacheService ResolveFromDescriptor(
		IServiceProvider sp,
		ServiceDescriptor descriptor) {

		if (descriptor.ImplementationInstance is ICacheService instance) {
			return instance;
		}
		if (descriptor.ImplementationFactory is not null) {
			return (ICacheService)descriptor.ImplementationFactory(sp);
		}
		return (ICacheService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
	}
}
