namespace Cirreum;

using Cirreum.Authorization;
using Cirreum.Authorization.Visualization;
using Cirreum.Conductor.Configuration;
using Cirreum.Extensions.Internal;
using Cirreum.Presence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Reflection;

/// <summary>
/// Extension methods to register the core services of the application.
/// </summary>
public static class ServiceCollectionExtensions {

	private const string DomainServicesRegisteredKey = "__DomainServicesRegistered";


	/// <summary>
	/// Tries to add the default built-in implementation of the 
	/// <see cref="IAuthorizationEvaluator"/> service if one is not already registered.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	public static void AddDefaultAuthorizationEvaluator(
		this IServiceCollection services) {
		services.TryAddScoped<IAuthorizationEvaluator, DefaultAuthorizationEvaluator>();
	}

	/// <summary>
	/// Tries to add the default built-in implementation of the 
	/// <see cref="IAuthorizationDocumenter"/> service if one is not already registered.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="serviceLifetime">The desired lifetime. Default: <see cref="ServiceLifetime.Singleton"/></param>
	public static void AddDefaultAuthorizationDocumenter(
		this IServiceCollection services,
		ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) {
		if (serviceLifetime == ServiceLifetime.Singleton) {
			services.TryAddSingleton<IAuthorizationDocumenter, EnhancedAuthorizationDocumenter>();
			return;
		}
		services.TryAddScoped<IAuthorizationDocumenter, EnhancedAuthorizationDocumenter>();
	}

	/// <summary>
	/// Registers the default domain context initializer.
	/// </summary>
	/// <param name="services">The current <see cref="IServiceCollection"/> to register with.</param>
	public static void AddDomainContextInitilizer(
		this IServiceCollection services) {
		services.TryAddScoped<IDomainContextInitializer, DomainContextInitializer>();
	}

	/// <summary>
	/// Registers a custom user presence service implementation with the specified refresh interval.
	/// </summary>
	/// <typeparam name="TUserPresenceService">The type of the user presence service implementation that must implement <see cref="IUserPresenceService"/>.</typeparam>
	/// <param name="builder">The <see cref="IUserPresenceBuilder"/> instance to configure.</param>
	/// <param name="refreshInterval">The interval in milliseconds at which the presence service should refresh user presence status.</param>
	/// <returns>The <see cref="IUserPresenceBuilder"/> instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when called outside of a browser environment (not Blazor WebAssembly).</exception>
	/// <remarks>
	/// This method registers the specified presence service as a scoped service and configures the monitoring options
	/// to use the provided refresh interval. The service will be used to periodically update user presence information.
	/// <para>
	/// This method can only be called in browser environments (Blazor WebAssembly). Attempting to use it in server-side
	/// environments will result in an <see cref="InvalidOperationException"/>.
	/// </para>
	/// </remarks>
	public static IUserPresenceBuilder AddPresenceService<TUserPresenceService>(
	   this IUserPresenceBuilder builder,
	   int refreshInterval)
	   where TUserPresenceService : class, IUserPresenceService {

		if (!OperatingSystem.IsBrowser()) {
			throw new InvalidOperationException("User presence monitor is only allowed on the client.");
		}

		builder.Services.AddScoped<IUserPresenceService, TUserPresenceService>();
		builder.Services.PostConfigure<UserPresenceMonitorOptions>(o =>
			o.RefreshInterval = refreshInterval
		);

		return builder;

	}

	///// <summary>
	///// Registers all domain services including validation, authorization, and request/notification handling.
	///// </summary>
	///// <remarks>
	///// <para>
	///// This method performs the following registrations:
	///// <list type="number">
	///// <item>Domain context initializer for environment awareness</item>
	///// <item>FluentValidation validators from all scanned assemblies</item>
	///// <item>Authorization evaluator and resource/policy validators</item>
	///// <item>Conductor (dispatcher and publisher) with the following intercept pipeline:
	/////   <list type="bullet">
	/////   <item>Validation - Validates requests using FluentValidation</item>
	/////   <item>Authorization - Authorizes requests against resource and policy validators</item>
	/////   <item>Performance Monitoring - Tracks handler execution metrics</item>
	/////   <item>Query Caching - Caches query results based on configuration</item>
	/////   </list>
	///// </item>
	///// </list>
	///// </para>
	///// <para>
	///// Configuration is loaded from the "Conductor" section of appsettings.json.
	///// </para>
	///// <para>
	///// This should be called after all other service registrations but before building the application.
	///// </para>
	///// </remarks>
	///// <param name="services">The service collection to configure.</param>
	///// <param name="configuration">Application configuration for binding Conductor settings.</param>
	///// <returns>The service collection for method chaining.</returns>
	//public static IServiceCollection AddDomainServices(
	//	this IServiceCollection services,
	//	IConfiguration configuration) {

	//	//
	//	// Domain Context Initializer
	//	//
	//	services.AddDomainContextInitilizer();

	//	//
	//	// Collect Assemblies
	//	//
	//	var assemblies = AssemblyScanner
	//		.ScanAssemblies()
	//		.ToArray();

	//	//
	//	// FluentValidation
	//	//
	//	services.AddFluentValidationAndAuthorization(assemblies);

	//	//
	//	// Conductor Configuration
	//	//
	//	var conductorSettings = new ConductorSettings();
	//	configuration.GetSection(ConductorSettings.SectionName).Bind(conductorSettings);
	//	services.TryAddSingleton(conductorSettings);

	//	//
	//	// Conductor
	//	//
	//	services.AddConductor(builder => {
	//		builder
	//			.RegisterFromAssemblies(assemblies)

	//			// Pre-processing intercepts
	//			.AddOpenIntercept(typeof(Validation<,>))        // Validate request structure
	//			.AddOpenIntercept(typeof(Authorization<,>))     // Authorize user access

	//			// Wrapping intercepts
	//			// Note: HandlerPerformance wraps QueryCaching to measure handler tier (cache + handler) duration
	//			.AddOpenIntercept(typeof(HandlerPerformance<,>)) // Measures handler tier performance

	//			// Pre-emptive Intercept
	//			.AddOpenIntercept(typeof(QueryCaching<,>));      // Cache proxy (returns cache OR calls handler)

	//	}, conductorSettings);

	//	return services;

	//}

	/// <summary>
	/// Registers all domain services including validation, authorization, and request/notification handling.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method performs the following registrations:
	/// <list type="number">
	/// <item>Domain context initializer for environment awareness</item>
	/// <item>FluentValidation validators from all scanned assemblies</item>
	/// <item>Authorization evaluator and resource/policy validators</item>
	/// <item>Conductor (dispatcher and publisher) with configurable intercept pipeline:
	///   <list type="bullet">
	///   <item>Validation - Validates requests using FluentValidation</item>
	///   <item>Authorization - Authorizes requests against resource and policy validators</item>
	///   <item>[Custom Intercepts] - Extensibility point for consumer-specific concerns</item>
	///   <item>Performance Monitoring - Tracks handler execution metrics</item>
	///   <item>Query Caching - Caches query results based on configuration</item>
	///   </list>
	/// </item>
	/// </list>
	/// </para>
	/// </remarks>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="assemblies">The assemblies to scan for handlers, validators, and authorizers.</param>
	/// <param name="configure">Optional configuration for Conductor settings and custom intercepts.</param>
	/// <returns>The service collection for method chaining.</returns>
	internal static IServiceCollection AddDomainServices(
		this IServiceCollection services,
		Assembly[] assemblies,
		Action<ConductorOptionsBuilder>? configure = null) {

		// Idempotency check
		if (services.Any(sd => sd.ServiceKey?.ToString() == DomainServicesRegisteredKey)) {
			throw new InvalidOperationException(
				"Domain services have already been registered. " +
				"Call AddDomainServices only once per service collection.");
		}
		services.AddKeyedSingleton<object>(DomainServicesRegisteredKey, new object());

		//
		// Domain Context Initializer
		//
		services.AddDomainContextInitilizer();

		//
		// FluentValidation
		//
		services.AddFluentValidationAndAuthorization(assemblies);

		//
		// Conductor Configuration
		//
		var optionsBuilder = new ConductorOptionsBuilder();
		configure?.Invoke(optionsBuilder);

		var conductorSettings = optionsBuilder.GetSettings();
		services.TryAddSingleton(conductorSettings);

		//
		// Conductor
		//
		services.AddConductor(builder => {
			builder.RegisterFromAssemblies(assemblies);
			optionsBuilder.ConfigureIntercepts(builder);
		}, conductorSettings);

		return services;
	}

	/// <summary>
	/// Registers all domain services using the provided <see cref="DomainServicesBuilder"/> 
	/// to determine which assemblies to scan.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="domainBuilder">The domain services builder containing registered assemblies.</param>
	/// <param name="configure">Optional configuration for Conductor settings and custom intercepts.</param>
	/// <returns>The service collection for method chaining.</returns>
	internal static IServiceCollection AddDomainServices(
		this IServiceCollection services,
		DomainServicesBuilder domainBuilder,
		Action<ConductorOptionsBuilder>? configure = null) {

		ArgumentNullException.ThrowIfNull(domainBuilder);

		var assemblies = domainBuilder.GetAssemblies().ToArray();
		if (assemblies.Length == 0) {
			assemblies = [.. AssemblyScanner.ScanAssemblies()];
		}

		return services.AddDomainServices(assemblies, configure);
	}

	/// <summary>
	/// Registers all domain services using automatic assembly scanning.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="configure">Optional configuration for Conductor settings and custom intercepts.</param>
	/// <returns>The service collection for method chaining.</returns>
	public static IServiceCollection AddDomainServices(
		this IServiceCollection services,
		Action<ConductorOptionsBuilder>? configure = null) {

		var assemblies = AssemblyScanner.ScanAssemblies().ToArray();
		return services.AddDomainServices(assemblies, configure);
	}

	static IServiceCollection AddFluentValidationAndAuthorization(this IServiceCollection services, params Assembly?[] assemblies) {

		var validatorOpenGenericType = typeof(IValidator<>);
		var resourceAuthorizorType = typeof(IAuthorizationResourceValidator<>);
		var policyAuthorizorType = typeof(IAuthorizationPolicyValidator);

		var availableTypes = assemblies
			.Where(a => a is not null)
			.SelectMany(a => a!.GetExportedTypes())
			.Distinct();


		// Normal Domain Validators
		// Excludes resource authorizors - they implement IValidator<T> too, but are registered separately
		var normalValidators = from type in availableTypes
							   where type.IsConcreteClass() &&
									 !type.ImplementsGenericInterface(resourceAuthorizorType)
							   let matchingInterface = type.GetFirstMatchingGenericInterface(validatorOpenGenericType)
							   where matchingInterface != null
							   select (matchingInterface, type);

		// Register each concrete Validator
		foreach (var (validatorInterface, validatorType) in normalValidators) {

			// Normal IService => Service registration
			services.TryAddEnumerable(new ServiceDescriptor(
				serviceType: validatorInterface,
				implementationType: validatorType,
				lifetime: ServiceLifetime.Transient
			));

			// Service => Service registration
			services.TryAddTransient(validatorType, validatorType);

		}

		// Resource Authorizors
		var resourceAuthorizors = from type in availableTypes
								  where type.IsConcreteClass()
								  let matchingInterface = type.GetFirstMatchingGenericInterface(resourceAuthorizorType)
								  where matchingInterface != null
								  select (matchingInterface, type);

		// Register each concrete Authorizor
		foreach (var (authorizorInterface, authorizorType) in resourceAuthorizors) {
			services.TryAddEnumerable(new ServiceDescriptor(
				serviceType: authorizorInterface,
				implementationType: authorizorType,
				lifetime: ServiceLifetime.Transient
			));

			// Service => Service registration
			services.AddTransient(authorizorType, authorizorType);

		}

		// Policy Authorizors
		var policyAuthValidators = from type in availableTypes
								   where type.IsConcreteClass() &&
										 type.IsAssignableTo(policyAuthorizorType)
								   select type;

		// Register each concrete Authorizor
		foreach (var validator in policyAuthValidators) {
			services.TryAddEnumerable(new ServiceDescriptor(
				serviceType: policyAuthorizorType,
				implementationType: validator,
				lifetime: ServiceLifetime.Transient
			));

			// Service => Service registration
			services.AddTransient(validator, validator);
		}

		return services;

	}

}