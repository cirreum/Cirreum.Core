namespace Cirreum.Conductor.Tests;

using Cirreum.Conductor.Configuration;
using Cirreum.Messaging;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
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

		services.AddSingleton<IDomainEnvironment>(sp =>
			new TestApplicationEnvironment());

		services.AddSingleton<IUserState>(sp => {
			return TestUserState.CreateAuthenticated();
		});

		services.AddSingleton<IUserStateAccessor, MockUserStateAccessor>();

		services.AddSingleton<IDistributedTransportPublisher, EmptyTransportPublisher>();

		services.AddSingleton(typeof(INotificationHandler<>), typeof(DistributedMessageHandler<>));

		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, PublisherStrategy.Sequential, sp.GetRequiredService<ILogger<Publisher>>()));

		if (builder is not null) {
			builder(services);
		}

		return services;

	}

	public static IDispatcher ArrangeSimpleDispatcher(Action<IServiceCollection>? builder = null) {

		var services = ArrangeServices(builder);

		services.AddSingleton<IDispatcher, Dispatcher>();

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