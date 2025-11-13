namespace Cirreum;

using Cirreum.Authorization;
using Cirreum.Authorization.Visualization;
using Cirreum.Conductor.Configuration;
using Cirreum.Conductor.Intercepts;
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
	/// Registers domain-related services, including validation, authorization and conductor, into the specified dependency
	/// injection container.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method adds FluentValidation, Cirreum Fluent Authorization, Cirreum Conductor to the service collection.
	/// The event publishing behavior can be customized via the publisher strategy parameter. All relevant assemblies are
	/// automatically scanned for service registration.
	/// </para>
	/// <para>
	/// This should be the last call before the call to Build on the Application Builder.
	/// </para>
	/// </remarks>
	/// <param name="services">The service collection to which domain services will be added. Must not be null.</param>
	/// <param name="configuration">The application configuration used to bind options. Must not be null.</param>
	/// <returns>The same service collection instance with domain services registered.</returns>
	public static IServiceCollection AddDomainServices(
		this IServiceCollection services,
		IConfiguration configuration) {

		//
		// Collect Assemblies
		//
		var assemblies = AssemblyScanner
			.ScanAssemblies()
			.ToArray();

		//
		// FluentValidation
		//
		services.AddFluentValidationAndAuthorization(assemblies);

		//
		// Conductor Configuration
		//
		var conductorSettings = new ConductorSettings();
		configuration.GetSection(ConductorSettings.SectionName).Bind(conductorSettings);
		services.AddSingleton(conductorSettings);

		//
		// Conductor
		//
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(assemblies)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(Performance<,>));
		}, conductorSettings);

		return services;

	}

	static IServiceCollection AddFluentValidationAndAuthorization(this IServiceCollection services, params Assembly?[] assemblies) {

		var validatorOpenGenericType = typeof(IValidator<>);
		var resourceAuthorizorType = typeof(IAuthorizationResourceValidator<>);
		var policyAuthorizorType = typeof(IAuthorizationPolicyValidator);

		var availableTypes = assemblies
			.Where(a => a is not null)
			.SelectMany(a => a!.GetExportedTypes())
			.Distinct();


		// Validators
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
		var authorizors = from type in availableTypes
						  where type.IsConcreteClass()
						  let matchingInterface = type.GetFirstMatchingGenericInterface(resourceAuthorizorType)
						  where matchingInterface != null
						  select (matchingInterface, type);

		// Register each concrete Authorizor
		foreach (var (authorizorInterface, authorizorType) in authorizors) {

			services.TryAddEnumerable(new ServiceDescriptor(
				serviceType: authorizorInterface,
				implementationType: authorizorType,
				lifetime: ServiceLifetime.Transient
			));

			// Service => Service registration
			services.AddTransient(authorizorType, authorizorType);

		}

		// Policy Authorizors
		var policyValidators = from type in availableTypes
							   where type.IsConcreteClass() &&
									 type.IsAssignableTo(policyAuthorizorType)
							   select type;

		// Register each concrete Authorizor
		foreach (var policyValidator in policyValidators) {

			services.TryAddEnumerable(new ServiceDescriptor(
				serviceType: policyAuthorizorType,
				implementationType: policyValidator,
				lifetime: ServiceLifetime.Transient
			));

			// Service => Service registration
			services.AddTransient(policyValidator, policyValidator);
		}

		return services;

	}

}