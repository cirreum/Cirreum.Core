namespace Cirreum.Conductor.Benchmarks;

using Cirreum;
using Cirreum.Authorization;
using Cirreum.Caching;
using Cirreum.Conductor;
using Cirreum.Conductor.Configuration;
using Cirreum.Messaging;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class Shared {

	public static ConductorSettings SequentialSettings { get; } = new ConductorSettings {
		PublisherStrategy = PublisherStrategy.Sequential
	};

	public static ConductorSettings FireAndForgetSettings { get; } = new ConductorSettings {
		PublisherStrategy = PublisherStrategy.FireAndForget
	};

	public static ConductorSettings ParallelSettings { get; } = new ConductorSettings {
		PublisherStrategy = PublisherStrategy.Parallel
	};

	public static IServiceCollection ArrangeServices(
		Action<IServiceCollection>? builder = null) {

		var services = new ServiceCollection();

		services.AddLogging(lb => {
			lb.ClearProviders();
			lb.AddDebug();
			lb.SetMinimumLevel(LogLevel.Trace);
		});

		// Central cache settings
		services.AddSingleton(new CacheSettings {
			Provider = CacheProvider.InMemory,
			DefaultExpiration = new QueryCacheOverride {
			Expiration = TimeSpan.FromMinutes(5)
		}
		});
		services.AddSingleton<ICacheService, InMemoryCacheService>();

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
	private static readonly IUserState _cachedUser = TestUserState.CreateAuthenticated();

	public ValueTask<IUserState> GetUser() {
		// Return cached user state — mirrors production behavior where
		// IUserStateAccessor reads from the per-request cache (zero alloc).
		return new ValueTask<IUserState>(_cachedUser);
	}
}
