namespace Cirreum;

using Cirreum.Authorization.Grants;
using Cirreum.Authorization.Grants.Caching;
using Cirreum.Caching;
using Cirreum.Extensions.Internal;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
/// <summary>
/// Extension methods for registering Cirreum access-grants (ReBAC) services.
/// </summary>
public static class GrantServiceCollectionExtensions {

	/// <summary>
	/// Registers the access-grants (ReBAC) pipeline with a single universal grant resolver.
	/// Registers the app-provided <see cref="IGrantResolver"/>, the Core-supplied
	/// <see cref="GrantBasedAccessReachResolver"/> orchestrator, and the sealed
	/// <see cref="GrantEvaluator"/> that enforces CRL (Command/Read/List) grant semantics
	/// as Stage 1 Step 0 of the authorization pipeline.
	/// </summary>
	/// <typeparam name="TGrantResolver">The app's grant-resolver implementation.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
	/// <param name="lifetime">
	/// The lifetime of both the grant resolver and the orchestrator. Default is
	/// <see cref="ServiceLifetime.Scoped"/> because grant lookups usually need a scoped
	/// repository/DB context.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddAccessGrants<TGrantResolver>(
		this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped)
		where TGrantResolver : class, IGrantResolver =>
		services.AddAccessGrantsCore(typeof(TGrantResolver), lifetime);

	/// <summary>
	/// Discovers the <see cref="IGrantResolver"/> implementation in the provided assemblies,
	/// binds <see cref="GrantCacheSettings"/> from configuration, and registers the full
	/// grants pipeline. This is the convention-based entry point intended for higher-level
	/// runtime extensions (e.g., <c>AddGrantAuthorization</c> in <c>Cirreum.Runtime</c>).
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
	/// <param name="configuration">
	/// The application <see cref="IConfiguration"/> used to bind <see cref="GrantCacheSettings"/>
	/// from the <c>Cirreum:Authorization:Grants:Cache</c> section.
	/// </param>
	/// <param name="assemblies">The assemblies to scan for grant resolver implementations.</param>
	/// <param name="configureCaching">
	/// Optional delegate to override cache settings beyond what configuration provides.
	/// Applied after binding from <paramref name="configuration"/>.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddGrantAuthorization(
		this IServiceCollection services,
		IConfiguration? configuration = null,
		Assembly[]? assemblies = null,
		Action<GrantCacheSettings>? configureCaching = null) {

		ArgumentNullException.ThrowIfNull(services);

		assemblies ??= [.. AssemblyScanner.ScanAssemblies()];

		// Register cache settings from configuration + delegate (idempotent)
		services.AddGrantCacheInfrastructure(configuration, configureCaching);

		var grantResolverType = typeof(IGrantResolver);

		var resolverType = assemblies
			.Where(a => a is not null)
			.SelectMany(a => a!.GetTypes())
			.Distinct()
			.FirstOrDefault(t => t.IsConcreteClass() && grantResolverType.IsAssignableFrom(t));

		if (resolverType is not null) {
			services.AddAccessGrantsCore(resolverType, ServiceLifetime.Scoped);
		}

		return services;
	}

	private static IServiceCollection AddAccessGrantsCore(
		this IServiceCollection services,
		Type resolverType,
		ServiceLifetime lifetime) {

		ArgumentNullException.ThrowIfNull(services);

		// Skip if already registered
		if (services.Any(sd => sd.ServiceType == typeof(IGrantResolver))) {
			return services;
		}

		// Resolver and orchestrator
		services.Add(ServiceDescriptor.Describe(
			serviceType: typeof(IGrantResolver),
			implementationType: resolverType,
			lifetime: lifetime));

		services.Add(ServiceDescriptor.Describe(
			serviceType: typeof(IAccessReachResolver),
			implementationType: typeof(GrantBasedAccessReachResolver),
			lifetime: lifetime));

		// Shared infrastructure
		services.TryAddScoped<IAccessReachAccessor, DefaultAccessReachAccessor>();
		services.TryAddScoped<AccessReachResolverSelector>();
		services.TryAddScoped<GrantEvaluator>();
		services.AddGrantCacheInfrastructure();

		return services;
	}

	/// <summary>
	/// Registers the shared cache infrastructure for the grant system. Idempotent — safe to
	/// call from every <see cref="AddAccessGrants{TGrantResolver}"/> invocation.
	/// </summary>
	private static void AddGrantCacheInfrastructure(
		this IServiceCollection services,
		IConfiguration? configuration = null,
		Action<GrantCacheSettings>? configureCaching = null) {

		// Only register once
		if (services.Any(sd => sd.ServiceType == typeof(GrantCacheSettings))) {
			return;
		}

		var settings = new GrantCacheSettings();

		if (configuration is not null) {
			var section = configuration.GetSection(GrantCacheSettings.SectionPath);
			section.Bind(settings);
		}

		configureCaching?.Invoke(settings);
		services.AddSingleton(settings);

		services.TryAddSingleton<IGrantCacheInvalidator, GrantCacheInvalidator>();

		// Safety net: ensure a cache service is available even if Conductor caching
		// hasn't been configured yet. NoCacheService degrades gracefully —
		// grants resolve on every request without L2 caching.
		services.TryAddSingleton<ICacheService, NoCacheService>();
	}
}
