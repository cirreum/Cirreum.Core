namespace Cirreum;

using Cirreum.Authorization;
using Cirreum.Authorization.Visualization;
using Cirreum.Conductor.Configuration;
using Cirreum.Extensions.Internal;
using Cirreum.Presence;
using FluentValidation;
using Microsoft.Extensions.Configuration;
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


	/// <summary>
	/// Adds domain services using Conductor as the core dispatcher/publisher engine,
	/// binding settings from configuration and applying domain conventions.
	/// </summary>
	/// <param name="services">
	/// The <see cref="IServiceCollection"/> to add services to.
	/// </param>
	/// <param name="configuration">
	/// The application <see cref="IConfiguration"/> used to bind <see cref="ConductorSettings"/>
	/// and any additional domain-specific configuration.
	/// </param>
	/// <param name="configureConductorOptions">
	/// Optional callback that allows callers (or a higher-level <c>DomainBuilder</c>) to
	/// customize Conductor behavior via <see cref="ConductorOptionsBuilder"/>, including
	/// overrides to configuration-bound settings and dispatcher lifetime.
	/// </param>
	/// <returns>
	/// The <see cref="IServiceCollection"/> for chaining.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This overload is intended to be the "one-stop" registration method for most applications.
	/// It configures a standard Conductor pipeline with validation, authorization, performance
	/// monitoring, and query caching, and registers domain request/notification handlers from
	/// the calling assembly.
	/// </para>
	/// <para>
	/// Advanced scenarios can still call
	/// <see cref="ConductorServiceCollectionExtensions.AddConductor(IServiceCollection, IConfiguration, Action{Cirreum.Conductor.ConductorBuilder}?, Action{ConductorOptionsBuilder}?)"/>
	/// directly for full control.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.
	/// </exception>
	public static IServiceCollection AddDomainServices(
		this IServiceCollection services,
		IConfiguration configuration,
		Action<ConductorOptionsBuilder>? configureConductorOptions = null) {

		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		// Idempotency check
		if (services.Any(sd => sd.ServiceKey?.ToString() == DomainServicesRegisteredKey)) {
			throw new InvalidOperationException(
				"Domain services have already been registered. " +
				"Call AddDomainServices only once per service collection.");
		}
		services.AddKeyedSingleton<object>(DomainServicesRegisteredKey, new object());


		//
		// Use Assembly Scanner to find all relevant assemblies
		//
		var assemblies = AssemblyScanner.ScanAssemblies().ToArray();

		//
		// FluentValidation
		//
		services.AddFluentValidationAndAuthorization(assemblies);

		//
		// Conductor
		//

		// We call the *internal* core with applyDefaultPipeline: true
		var optionsBuilder = new ConductorOptionsBuilder(configuration) {
			CustomInterceptsAllowed = true
		};

		// Let the caller (or DomainBuilder) add domain intercepts or tweak settings
		configureConductorOptions?.Invoke(optionsBuilder);
		services.AddConductorInternal(
			// Standard domain Conductor setup:
			configureConductor: conductor => {
				conductor.RegisterFromAssemblies(assemblies);
			},
			 // Conductor options configuration for domain:
			 optionsBuilder: optionsBuilder,
			 applyDefaultPipeline: true);

		//
		// Domain Context Initializer
		//
		services.AddDomainContextInitilizer();

		return services;
	}

	static IServiceCollection AddFluentValidationAndAuthorization(this IServiceCollection services, params Assembly?[] assemblies) {

		var validatorOpenGenericType = typeof(IValidator<>);
		var resourceAuthorizorType = typeof(IAuthorizationResourceValidator<>);
		var policyAuthorizorType = typeof(IAuthorizationPolicyValidator);

		var availableTypes = assemblies
			.Where(a => a is not null)
			.SelectMany(a => a!.GetTypes())
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