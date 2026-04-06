namespace Cirreum;

using Cirreum.Authorization.Grants;
using Cirreum.Authorization.Grants.Caching;
using Cirreum.Extensions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

/// <summary>
/// Extension methods for registering Cirreum access-grants (ReBAC) services.
/// </summary>
public static class GrantServiceCollectionExtensions {

	/// <summary>
	/// Registers the access-grants (ReBAC) pipeline for a bounded context. Registers the
	/// app-provided <see cref="IGrantResolver{TDomain}"/>, the Core-supplied
	/// <c>GrantBasedAccessReachResolver&lt;TDomain&gt;</c> orchestrator, and the sealed
	/// <see cref="GrantEvaluator"/> that enforces CRL (Command/Read/List) grant semantics
	/// as Stage 1 Step 0 of the authorization pipeline.
	/// </summary>
	/// <typeparam name="TDomain">
	/// The bounded-context domain marker interface (e.g., <c>IIssueOperation</c>) that
	/// grant-aware commands, reads, and lists in this bounded context compose via
	/// <see cref="IGrantedCommand{TDomain}"/>, <see cref="IGrantedRead{TDomain, TResponse}"/>,
	/// or <see cref="IGrantedList{TDomain, TResponse}"/>.
	/// </typeparam>
	/// <typeparam name="TGrantResolver">The app's grant-resolver implementation.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
	/// <param name="lifetime">
	/// The lifetime of both the grant resolver and the orchestrator. Default is
	/// <see cref="ServiceLifetime.Scoped"/> because grant lookups usually need a scoped
	/// repository/DB context.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	/// <remarks>
	/// <para>
	/// Multiple calls with different <typeparamref name="TDomain"/> markers compose — each
	/// marker claims its own disjoint set of resource types. Core's
	/// <see cref="AccessReachResolverSelector"/> enforces 1:1 at lookup time; overlapping
	/// claims fail fast with a descriptive error.
	/// </para>
	/// <para>
	/// Also ensures a scoped <see cref="IAccessReachAccessor"/>, the selector, and the
	/// <see cref="GrantEvaluator"/> are registered. On the first call, also registers the
	/// shared <see cref="GrantCacheSettings"/> (bound from configuration + optional delegate)
	/// and the <see cref="IGrantCacheInvalidator"/> singleton.
	/// </para>
	/// </remarks>
	public static IServiceCollection AddAccessGrants<TDomain, TGrantResolver>(
		this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped)
		where TDomain : class
		where TGrantResolver : class, IGrantResolver<TDomain> {

		ArgumentNullException.ThrowIfNull(services);

		// Per-domain — skip if this domain is already registered
		if (services.Any(sd => sd.ServiceType == typeof(IGrantResolver<TDomain>))) {
			return services;
		}

		// Domain-specific resolver and orchestrator
		services.Add(ServiceDescriptor.Describe(
			serviceType: typeof(IGrantResolver<TDomain>),
			implementationType: typeof(TGrantResolver),
			lifetime: lifetime));

		services.Add(ServiceDescriptor.Describe(
			serviceType: typeof(IAccessReachResolver),
			implementationType: typeof(GrantBasedAccessReachResolver<TDomain>),
			lifetime: lifetime));

		//
		// Shared infrastructure
		//
		// Technically these only need to be called once, but it's simpler
		// to ensure they're present from every AddAccessGrants call
		//
		services.TryAddScoped<IAccessReachAccessor, DefaultAccessReachAccessor>();
		services.TryAddScoped<AccessReachResolverSelector>();
		services.TryAddScoped<GrantEvaluator>();
		services.AddGrantCacheInfrastructure();

		return services;
	}


	/// <summary>
	/// Discovers all <see cref="IGrantResolver{TDomain}"/> implementations in the provided
	/// assemblies and registers each one via <see cref="AddAccessGrants{TDomain, TGrantResolver}"/>.
	/// Intended to be called from a higher-level runtime extension (e.g., <c>AddGrantAuthorization</c>
	/// in <c>Cirreum.Runtime</c>).
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
	/// <remarks>
	/// <para>
	/// For each concrete class implementing <c>IGrantResolver&lt;TDomain&gt;</c>, this method
	/// extracts the <c>TDomain</c> type argument and calls the closed-generic
	/// <c>AddAccessGrants&lt;TDomain, TGrantResolver&gt;</c> via reflection.
	/// </para>
	/// <para>
	/// Cache settings are registered once (idempotent) across all discovered domains.
	/// If <paramref name="configuration"/> is provided, settings are first bound from the
	/// <c>Cirreum:Authorization:Grants:Cache</c> section, then the optional
	/// <paramref name="configureCaching"/> delegate is applied.
	/// </para>
	/// </remarks>
	public static IServiceCollection AddGrantAuthorization(
		this IServiceCollection services,
		IConfiguration? configuration = null,
		Assembly[]? assemblies = null,
		Action<GrantCacheSettings>? configureCaching = null) {

		ArgumentNullException.ThrowIfNull(services);

		assemblies ??= [.. AssemblyScanner.ScanAssemblies()];

		// Register cache settings from configuration + delegate (idempotent)
		services.AddGrantCacheInfrastructure(configuration, configureCaching);

		var grantResolverOpen = typeof(IGrantResolver<>);

		var resolvers = from type in assemblies
							.Where(a => a is not null)
							.SelectMany(a => a!.GetTypes())
							.Distinct()
						where type.IsConcreteClass()
						let matchingInterface = type.GetFirstMatchingGenericInterface(grantResolverOpen)
						where matchingInterface is not null
						select (resolverType: type, domainType: matchingInterface.GetGenericArguments()[0]);

		foreach (var (resolverType, domainType) in resolvers) {
			// Build: AddAccessGrants<TDomain, TGrantResolver>(services)
			var method = AddAccessGrantsMethod.MakeGenericMethod(domainType, resolverType);
			method.Invoke(null, [services, ServiceLifetime.Scoped]);
		}

		return services;
	}


	// Reflection target for AddGrantAuthorization scanner — avoids duplicating registration logic
	private static readonly MethodInfo AddAccessGrantsMethod =
		typeof(GrantServiceCollectionExtensions)
			.GetMethod(nameof(AddAccessGrants), BindingFlags.Public | BindingFlags.Static)!;


	/// <summary>
	/// Registers the shared cache infrastructure for the grant system. Idempotent — safe to
	/// call from every <see cref="AddAccessGrants{TDomain, TGrantResolver}"/> invocation.
	/// </summary>
	private static void AddGrantCacheInfrastructure(
		this IServiceCollection services,
		IConfiguration? configuration = null,
		Action<GrantCacheSettings>? configureCaching = null) {

		// Only register once across all TDomain calls
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
	}

}
