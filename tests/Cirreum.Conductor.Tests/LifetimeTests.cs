namespace Cirreum.Conductor.Tests;

using Cirreum.Conductor.Configuration;
using Cirreum.Conductor.Intercepts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LifetimeTests {

	public class LifetimeIntercept<TRequest, TResponse> : IIntercept<TRequest, TResponse>
		where TRequest : notnull {

		public async Task<Result<TResponse>> HandleAsync(
			RequestContext<TRequest> context,
			RequestHandlerDelegate<TRequest, TResponse> next,
			CancellationToken cancellationToken) {
			var result = await next(context, cancellationToken);
			return result;
		}
	}

	[TestMethod]
	public void DefaultDispatcher_IsTransient_MultipleResolvesAreDifferentInstances() {
		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				// No lifetime override -> should be Transient
			});

		using var sp = services.BuildServiceProvider();

		// Act
		var d1 = sp.GetRequiredService<IDispatcher>();
		var d2 = sp.GetRequiredService<IDispatcher>();

		// Assert
		Assert.AreNotSame(d1, d2, "Default dispatcher should be Transient.");
	}

	[TestMethod]
	public void MisconfiguredSingletonDispatcher_IsSameInstanceAcrossScopes() {
		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				options.WithLifetime(ServiceLifetime.Singleton);
			});

		using var sp = services.BuildServiceProvider();

		using var scope1 = sp.CreateScope();
		var d1 = scope1.ServiceProvider.GetRequiredService<IDispatcher>();

		using var scope2 = sp.CreateScope();
		var d2 = scope2.ServiceProvider.GetRequiredService<IDispatcher>();

		// Assert: this *should* be true for singleton. If you expected scoped, this test fails.
		Assert.AreSame(d1, d2, "Singleton dispatcher will be shared across scopes.");
	}


	[TestMethod]
	public void ScopedDispatcher_SameScopeSameInstance_DifferentScopesDifferentInstance() {
		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				options.WithLifetime(ServiceLifetime.Scoped);
			});

		using var sp = services.BuildServiceProvider();

		// Act
		using var scope1 = sp.CreateScope();
		var d1a = scope1.ServiceProvider.GetRequiredService<IDispatcher>();
		var d1b = scope1.ServiceProvider.GetRequiredService<IDispatcher>();

		using var scope2 = sp.CreateScope();
		var d2 = scope2.ServiceProvider.GetRequiredService<IDispatcher>();

		// Assert
		Assert.AreSame(d1a, d1b, "Scoped dispatcher should be the same instance within a scope.");
		Assert.AreNotSame(d1a, d2, "Scoped dispatcher should differ across scopes.");
	}


	[TestMethod]
	public void ScopedDispatcher_ResolvedFromRoot_ThrowsOnValidateScopes() {

		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				options.WithLifetime(ServiceLifetime.Scoped);
			});

		using var sp = services.BuildServiceProvider(new ServiceProviderOptions {
			ValidateScopes = true
		});

		// Act + Assert
		Assert.ThrowsExactly<InvalidOperationException>(
			() => sp.GetRequiredService<IDispatcher>());
	}

	[TestMethod]
	public void ScopedDispatcher_UsedFromSingleton_ThrowsOnValidateScopes() {

		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);

				// This is what we want to prove is *really* scoped:
				options.WithLifetime(ServiceLifetime.Scoped);
			});

		// This is intentionally wrong: singleton depending on a scoped service.
		services.AddSingleton<SingletonDependsOnDispatcher>();

		void Build() {
			using var sp = services.BuildServiceProvider(new ServiceProviderOptions {
				ValidateScopes = true
			});

			// Resolving the singleton from the root is what triggers the validation.
			sp.GetRequiredService<SingletonDependsOnDispatcher>();
		}

		// Act + Assert
		Assert.ThrowsExactly<InvalidOperationException>(Build);
	}

	[TestMethod]
	public void AddConductor_RegistersDispatcherAsTransientByDefault() {

		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				// no lifetime override -> should be Transient
			});

		// Act
		var dispatcherDescriptor = services.Single(
			sd => sd.ServiceType == typeof(IDispatcher));

		// Assert
		Assert.AreEqual(ServiceLifetime.Transient, dispatcherDescriptor.Lifetime);
	}

	[TestMethod]
	public void AddConductor_WithLifeTimeSingleton_RegistersDispatcherAsSingleton() {

		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				options.WithLifetime(ServiceLifetime.Singleton);
			});

		// Act
		var dispatcherDescriptor = services.Single(sd => sd.ServiceType == typeof(Dispatcher));

		// Assert
		Assert.AreEqual(ServiceLifetime.Singleton, dispatcherDescriptor.Lifetime);
	}


	[TestMethod]
	public void DispatcherFacades_ResolveToSameInstance_PerScope() {
		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				options.WithLifetime(ServiceLifetime.Scoped);
			});

		using var sp = services.BuildServiceProvider();

		// Act
		using var scope = sp.CreateScope();
		var concrete = scope.ServiceProvider.GetRequiredService<Dispatcher>();
		var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
		var conductor = scope.ServiceProvider.GetRequiredService<IConductor>();

		// Assert
		Assert.AreSame(concrete, dispatcher, "IDispatcher should be the same instance as Dispatcher.");
		Assert.AreSame(dispatcher, conductor, "IConductor should resolve to the same Dispatcher instance.");
	}

	[TestMethod]
	public void PublisherFacades_ResolveToSameInstance_PerScope() {
		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
				options.WithLifetime(ServiceLifetime.Scoped);
			});

		using var sp = services.BuildServiceProvider();

		// Act
		using var scope = sp.CreateScope();
		var concrete = scope.ServiceProvider.GetRequiredService<Publisher>();
		var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

		// Assert
		Assert.AreSame(concrete, publisher, "IPublisher should be the same instance as Publisher for the configured lifetime.");
	}

	[TestMethod]
	public void AddConductor_CalledTwiceOnSameServiceCollection_ThrowsInvalidOperation() {
		// Arrange
		var services = Shared.ArrangeServices();

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
			});

		// Act
		void SecondCall() => services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(Shared.SequentialSettings);
			});

		// Assert
		Assert.Throws<InvalidOperationException>(SecondCall);
	}

	[TestMethod]
	public void AddDomainServices_RegistersDispatcherAsScopedByDefault() {
		// Arrange
		var services = Shared.ArrangeServices();
		var configuration = new ConfigurationBuilder().Build(); // empty is fine for this

		services.AddDomainServices(configuration);

		// Act
		var dispatcherDescriptor = services.Single(sd => sd.ServiceType == typeof(Dispatcher));

		// Assert
		Assert.AreEqual(ServiceLifetime.Transient, dispatcherDescriptor.Lifetime,
			"AddDomainServices should configure Dispatcher as Transient by default.");

	}

	[TestMethod]
	public void AddDomainServices_HonorsConductorLifetimeOverride() {
		// Arrange
		var services = Shared.ArrangeServices();
		var configuration = new ConfigurationBuilder().Build();

		services.AddDomainServices(
			configuration,
			configureConductorOptions: options => {
				// Caller explicitly wants scoped dispatcher
				options.WithLifetime(ServiceLifetime.Scoped);
			});

		// Act
		var dispatcherDescriptor = services.Single(sd => sd.ServiceType == typeof(Dispatcher));

		// Assert
		Assert.AreEqual(
			ServiceLifetime.Scoped,
			dispatcherDescriptor.Lifetime,
			"AddDomainServices should honor lifetime overrides passed via ConductorOptionsBuilder.");
	}

	[TestMethod]
	public void AddConductor_UsesProvidedCacheSettings() {
		// Arrange
		var services = Shared.ArrangeServices();

		var customSettings = new ConductorSettings {
			PublisherStrategy = PublisherStrategy.Sequential,
			Cache = new ConductorCacheSettings {
				Provider = CacheProvider.InMemory,
				DefaultExpiration = TimeSpan.FromSeconds(42)
			}
		};

		services.AddConductor(
			conductor => {
				conductor.RegisterFromAssemblies(typeof(Shared).Assembly);
			},
			options => {
				options.WithSetting(customSettings);
			});

		using var sp = services.BuildServiceProvider();

		// Act
		// However you expose the effective settings in runtime —
		// if you're registering ConductorSettings as a singleton or IOptions<ConductorSettings>,
		// pull it that way. Example:
		var resolvedSettings = sp.GetRequiredService<ConductorSettings>();

		// Assert
		Assert.AreEqual(TimeSpan.FromSeconds(42), resolvedSettings.Cache.DefaultExpiration);
		Assert.AreEqual(CacheProvider.InMemory, resolvedSettings.Cache.Provider);
	}


	[TestMethod]
	public void Publisher_LifetimeMatchesDispatcher() {
		var services = Shared.ArrangeServices();

		services.AddConductor(
			_ => { },
			o => o.WithLifetime(ServiceLifetime.Scoped));

		var dc = services.First(sd => sd.ServiceType == typeof(Dispatcher));
		var pub = services.First(sd => sd.ServiceType == typeof(Publisher));

		Assert.AreEqual(dc.Lifetime, pub.Lifetime);
	}

	[TestMethod]
	public void RequestHandlers_AreAlwaysTransient() {
		var services = Shared.ArrangeConductor();

		var handlerDescriptor = services.First(sd =>
			sd.ServiceType.IsGenericType &&
			sd.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

		Assert.AreEqual(ServiceLifetime.Transient, handlerDescriptor.Lifetime);
	}

	[TestMethod]
	public void Intercepts_AreRegisteredInExpectedOrder_ForRawAddConductor() {
		// Arrange
		var services = Shared.ArrangeConductor();

		// Act
		var interceptDescriptors = services
			.Where(sd =>
				sd.ServiceType.IsGenericType &&
				sd.ServiceType.GetGenericTypeDefinition() == typeof(IIntercept<,>))
			.ToList();

		Assert.HasCount(4, interceptDescriptors,
			$"Expected 4 intercept registrations, but found {interceptDescriptors.Count}.");

		static Type ToOpenGeneric(Type type)
			=> type.IsGenericType ? type.GetGenericTypeDefinition() : type;

		var implTypes = interceptDescriptors
			.Select(sd => sd.ImplementationType ?? throw new AssertFailedException(
				"Intercept ServiceDescriptor.ImplementationType was null; expected open generic registration."))
			.Select(ToOpenGeneric)
			.ToArray();

		// Validate expected order: Validation -> Authorization -> HandlerPerformance -> QueryCaching
		Assert.AreEqual(typeof(Validation<,>), implTypes[0], "First intercept should be Validation<,>.");
		Assert.AreEqual(typeof(Authorization<,>), implTypes[1], "Second intercept should be Authorization<,>.");
		Assert.AreEqual(typeof(HandlerPerformance<,>), implTypes[2], "Third intercept should be HandlerPerformance<,>.");
		Assert.AreEqual(typeof(QueryCaching<,>), implTypes[3], "Fourth intercept should be QueryCaching<,>.");
	}


	[TestMethod]
	public void DomainPipeline_Intercepts_AreRegisteredInExpectedOrder() {
		// Arrange
		var services = Shared.ArrangeServices();
		var configuration = new ConfigurationBuilder().Build();

		services.AddDomainServices(configuration, options => {
			options.AddCustomIntercepts(builder => {
				builder.AddOpenIntercept(typeof(LifetimeIntercept<,>)); // whatever your custom is
			});
		});

		// Act
		var interceptDescriptors = services
			.Where(sd =>
				sd.ServiceType.IsGenericType &&
				sd.ServiceType.GetGenericTypeDefinition() == typeof(IIntercept<,>))
			.ToList();

		// We expect exactly 5 intercept registrations:
		// 1. Validation
		// 2. Authorization
		// 3. Custom (LifetimeIntercept)
		// 4. HandlerPerformance
		// 5. QueryCaching
		Assert.HasCount(5, interceptDescriptors,
			$"Expected 5 intercept registrations, but found {interceptDescriptors.Count}.");

		static Type ToOpenGeneric(Type type)
			=> type.IsGenericType ? type.GetGenericTypeDefinition() : type;

		var orderedImplementationTypes = interceptDescriptors
			.Select(sd => sd.ImplementationType ?? throw new AssertFailedException(
				"Intercept ServiceDescriptor.ImplementationType was null; expected open generic registration."))
			.Select(ToOpenGeneric)
			.ToArray();

		// Assert order
		Assert.AreEqual(typeof(Validation<,>), orderedImplementationTypes[0],
			"First intercept should be Validation<,>.");
		Assert.AreEqual(typeof(Authorization<,>), orderedImplementationTypes[1],
			"Second intercept should be Authorization<,>.");

		// This is the "custom" intercept added via AddCustomIntercepts
		Assert.AreEqual(typeof(LifetimeIntercept<,>), orderedImplementationTypes[2],
			"Third intercept should be the custom Validation<,> intercept.");

		Assert.AreEqual(typeof(HandlerPerformance<,>), orderedImplementationTypes[3],
			"Fourth intercept should be HandlerPerformance<,>.");
		Assert.AreEqual(typeof(QueryCaching<,>), orderedImplementationTypes[4],
			"Fifth intercept should be QueryCaching<,>.");

	}

	[TestMethod]
	public void AddConductor_AddCustomIntercepts_Throws() {
		var services = Shared.ArrangeServices();

		Assert.Throws<InvalidOperationException>(() =>
			services.AddConductor(
				c => c.RegisterFromAssemblies(typeof(Shared).Assembly),
				o => o.AddCustomIntercepts(cb => {
					cb.AddOpenIntercept(typeof(LifetimeIntercept<,>));
				})
			));
	}


	private sealed class SingletonDependsOnDispatcher {
		public SingletonDependsOnDispatcher(IDispatcher _) {
			Console.WriteLine("SingletonDependsOnDispatcher created.");
		}
	}
}
