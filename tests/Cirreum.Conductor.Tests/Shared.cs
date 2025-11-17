namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Conductor.Configuration;
using Cirreum.Conductor.Intercepts;
using Cirreum.Messaging;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

	public static IServiceCollection ArrangeConductor(
		ConductorSettings? conductorSettings = default,
		Action<IServiceCollection>? builder = null) {
		return ArrangeServices(builder)
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(Shared).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>));
			}, conductorSettings ?? SequentialSettings);
	}

	public static IServiceCollection ArrangeServices(
		Action<IServiceCollection>? builder = null,
		TestContext? testContext = null) {

		var services = new ServiceCollection();

		services.AddLogging(lb => {
			lb.ClearProviders();

			if (testContext is not null) {
				lb.AddProvider(new TestContextLoggerProvider(testContext));
			} else {
				// Fallback if used outside tests
				lb.AddDebug();
			}

			lb.SetMinimumLevel(LogLevel.Trace);
		});

		services.AddDomainContextInitilizer();

		services.TryAddSingleton(SequentialSettings);

		services.AddSingleton<IDomainEnvironment>(sp =>
			new TestApplicationEnvironment());

		services.AddSingleton<IUserState>(sp => {
			return TestUserState.CreateAuthenticated();
		});

		services.AddSingleton<IAuthorizationEvaluator, DefaultAuthorizationEvaluator>();

		services.AddSingleton<IUserStateAccessor, MockUserStateAccessor>();

		services.AddSingleton<IDistributedTransportPublisher, EmptyTransportPublisher>();

		services.AddSingleton(typeof(INotificationHandler<>), typeof(DistributedMessageHandler<>));

		services.AddSingleton<IAuthorizationRoleRegistry, TestAuthorizationRoleRegistry>();


		if (builder is not null) {
			builder(services);
		}

		return services;

	}

	public static IDispatcher ArrangeSimpleDispatcher(Action<IServiceCollection>? builder = null) {

		var services = ArrangeServices(builder);

		// Register concrete Publisher (internal, accessible via InternalsVisibleTo)
		services.TryAddSingleton(sp => new Publisher(
			sp,
			SequentialSettings.PublisherStrategy,
			sp.GetRequiredService<ILogger<Publisher>>()));

		// Register concrete Dispatcher (internal, accessible via InternalsVisibleTo)
		services.TryAddSingleton(sp => new Dispatcher(
			sp,
			sp.GetRequiredService<Publisher>()));

		// Register public interface facades - all resolve to same singleton instances
		services.TryAddSingleton<IPublisher>(sp => sp.GetRequiredService<Publisher>());
		services.TryAddSingleton<IDispatcher>(sp => sp.GetRequiredService<Dispatcher>());
		services.TryAddSingleton<IConductor>(sp => sp.GetRequiredService<Dispatcher>());

		var sp = services.BuildServiceProvider();

		return sp.GetRequiredService<IDispatcher>();

	}

}

public class TestApplicationEnvironment : IDomainEnvironment {
	public string ApplicationName => "Test";
	public string EnvironmentName => "Development";
	public DomainRuntimeType RuntimeType { get; } = DomainRuntimeType.UnitTest;
}

public class MockUserStateAccessor : IUserStateAccessor {
	public ValueTask<IUserState> GetUser() {
		// Return a mock user state
		return new ValueTask<IUserState>(TestUserState.CreateAuthenticated());
	}
}