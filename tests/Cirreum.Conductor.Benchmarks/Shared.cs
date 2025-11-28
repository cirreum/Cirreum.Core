namespace Cirreum.Conductor.Benchmarks;

using Cirreum;
using Cirreum.Authorization;
using Cirreum.Conductor;
using Cirreum.Conductor.Configuration;
using Cirreum.Messaging;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
		Action<IServiceCollection>? builder = null) {

		var services = new ServiceCollection();

		services.AddLogging(lb => {
			lb.ClearProviders();
			lb.AddDebug();
			lb.SetMinimumLevel(LogLevel.Trace);
		});

		services.AddSingleton<IDomainEnvironment>(sp =>
			new TestApplicationEnvironment());

		services.AddSingleton<IUserState>(sp => {
			return TestUserState.CreateAuthenticated();
		});

		services.AddSingleton<IAuthorizationRoleRegistry, TestAuthorizationRoleRegistry>();

		services.AddSingleton<IUserStateAccessor, MockUserStateAccessor>();

		services.AddSingleton<IDistributedTransportPublisher, EmptyTransportPublisher>();

		// services.AddSingleton(typeof(INotificationHandler<>), typeof(DistributedMessageHandler<>));


		if (builder is not null) {
			builder(services);
		}

		return services;

	}

	public static IServiceProvider ArrangeSimpleDispatcher(Action<IServiceCollection>? builder = null) {

		var services = new ServiceCollection();

		services.AddLogging(lb => {
			lb.ClearProviders();
			lb.AddDebug();
			lb.SetMinimumLevel(LogLevel.Warning);
		});

		services.AddSingleton<IDomainEnvironment>(sp =>
			new TestApplicationEnvironment());

		services.AddDomainContextInitilizer();

		services.AddTransient<IPublisher>(sp =>
		new Publisher(
			sp,
			PublisherStrategy.Sequential,
			sp.GetRequiredService<ILogger<Publisher>>()));

		services.AddTransient<IDispatcher, Dispatcher>();

		if (builder is not null) {
			builder(services);
		}

		return services.BuildServiceProvider();

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