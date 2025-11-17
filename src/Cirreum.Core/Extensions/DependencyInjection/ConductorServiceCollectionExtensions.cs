namespace Microsoft.Extensions.DependencyInjection;

using Cirreum.Conductor;
using Cirreum.Conductor.Caching;
using Cirreum.Conductor.Configuration;
using Cirreum.Extensions.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;

/// <summary>
/// Extension methods for registering Cirreum.Conductor services.
/// </summary>
public static class ConductorServiceCollectionExtensions {

	/// <summary>
	/// Adds Cirreum.Conductor services including the dispatcher, publisher, and allows
	/// manual configuration of request handlers, intercepts, and notification handlers.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="configure">Action to configure the conductor builder.</param>
	/// <param name="settings">The conductor settings.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddConductor(
		this IServiceCollection services,
		Action<ConductorBuilder> configure,
		ConductorSettings settings) {

		// Register concrete Publisher (internal, accessible via InternalsVisibleTo)
		services.TryAddSingleton(sp => new Publisher(
			sp,
			settings.PublisherStrategy,
			sp.GetRequiredService<ILogger<Publisher>>()));

		// Register concrete Dispatcher (internal, accessible via InternalsVisibleTo)
		services.TryAddSingleton(sp => new Dispatcher(
			sp,
			sp.GetRequiredService<Publisher>()));

		// Register public interface facades - all resolve to same singleton instances
		services.TryAddSingleton<IPublisher>(sp => sp.GetRequiredService<Publisher>());
		services.TryAddSingleton<IDispatcher>(sp => sp.GetRequiredService<Dispatcher>());
		services.TryAddSingleton<IConductor>(sp => sp.GetRequiredService<Dispatcher>());

		// Register cache service based on configuration
		services.AddCacheableQueryService(settings);

		// Create and configure the builder
		var builder = new ConductorBuilder();
		configure(builder);

		// Register handlers and notification handlers from configured assemblies
		services.AddRequestHandlers([.. builder.Assemblies]);
		services.AddNotificationHandlers([.. builder.Assemblies]);

		// Register intercepts in the order they were added to the builder
		foreach (var interceptDescriptor in builder.Intercepts) {
			services.Add(interceptDescriptor);
		}

		return services;
	}

	private static void AddCacheableQueryService(
		this IServiceCollection services,
		ConductorSettings settings) {

		// Register cache service based on configuration
		// Use Replace for None/InMemory to enforce config over code registration
		// Use TryAdd for HybridCache to allow infrastructure layer to provide implementation
		switch (settings.Cache.Provider) {
			case CacheProvider.None:
				services.Replace(ServiceDescriptor.Singleton<ICacheableQueryService, NoCacheQueryService>());
				break;

			case CacheProvider.InMemory:
				services.Replace(ServiceDescriptor.Singleton<ICacheableQueryService, InMemoryCacheableQueryService>());
				break;

			case CacheProvider.HybridCache:
				// Infrastructure layer should register HybridCacheableQueryService
				// If not registered, fall back to None
				services.TryAddSingleton<ICacheableQueryService, NoCacheQueryService>();
				break;
		}

	}

	private static IServiceCollection AddRequestHandlers(
		this IServiceCollection services,
		Assembly[] assemblies) {

		var voidHandlerType = typeof(IRequestHandler<>);
		var typedHandlerType = typeof(IRequestHandler<,>);

		var availableTypes = assemblies
			.SelectMany(a => a.GetExportedTypes())
			.Where(t => t.IsConcreteClass())  // only concrete
			.Distinct();

		var handlers = from type in availableTypes
					   let voidInterface = type.GetFirstMatchingGenericInterface(voidHandlerType)
					   let typedInterface = type.GetFirstMatchingGenericInterface(typedHandlerType)
					   where voidInterface != null || typedInterface != null
					   select (type, voidInterface, typedInterface);

		foreach (var (handlerType, voidInterface, typedInterface) in handlers) {
			if (voidInterface != null) {
				services.TryAddTransient(voidInterface, handlerType);
			}
			if (typedInterface != null) {
				services.TryAddTransient(typedInterface, handlerType);
			}
		}

		return services;
	}

	private static IServiceCollection AddNotificationHandlers(
		this IServiceCollection services,
		Assembly[] assemblies) {

		var notificationHandlerType = typeof(INotificationHandler<>);

		var availableTypes = assemblies
			.SelectMany(a => a.GetExportedTypes())
			.Where(t => t.IsClass && !t.IsAbstract)
			.Distinct();

		// 1. Register closed generic handlers (concrete implementations)
		var closedHandlers = from type in availableTypes
							 where !type.IsGenericTypeDefinition
							 let matchingInterface = type.GetFirstMatchingGenericInterface(notificationHandlerType)
							 where matchingInterface != null
							 select (matchingInterface, type);

		foreach (var (handlerInterface, handlerType) in closedHandlers) {
			services.Add(ServiceDescriptor.Transient(handlerInterface, handlerType));
		}

		// 2. Register open generic handlers (like DistributedMessageHandler<>)
		var openHandlers = from type in availableTypes
						   where type.IsGenericTypeDefinition
						   let interfaces = type.GetInterfaces()
						   where interfaces.Any(i =>
							   i.IsGenericType &&
							   i.GetGenericTypeDefinition() == notificationHandlerType)
						   // Verify arity matches (INotificationHandler<> has 1 generic parameter)
						   where type.GetGenericArguments().Length == 1
						   select type;

		foreach (var handlerType in openHandlers) {
			// Register the open generic against the open generic interface
			services.Add(ServiceDescriptor.Transient(notificationHandlerType, handlerType));
		}

		return services;

	}

}