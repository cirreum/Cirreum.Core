namespace Cirreum;

using System.Diagnostics;

/// <summary>
/// Provides shared context and configuration keys for the Cirreum domain framework.
/// </summary>
public static class DomainContext {

	/// <summary>
	/// The key used to store the resolved <see cref="DomainRuntimeType"/> in the
	/// <see cref="Microsoft.Extensions.Hosting.IHostApplicationBuilder.Properties"/> dictionary
	/// during application configuration and setup.
	/// </summary>
	public const string RuntimeTypeKey = "Cirreum:DomainRuntimeType";

	private static bool _initialized = false;

	internal static void Initialize(IDomainEnvironment domainEnvironment) {
		if (!_initialized) {
			_initialized = true;
			Environment = domainEnvironment.EnvironmentName;
			RuntimeType = domainEnvironment.RuntimeType;
			CurrentActivityKind = ResolveActivityKind(domainEnvironment.RuntimeType);
		}
	}

	internal static string Environment { get; private set; } = "Development";

	internal static DomainRuntimeType RuntimeType { get; private set; } = DomainRuntimeType.WebApi;

	internal static ActivityKind CurrentActivityKind { get; private set; } = ActivityKind.Internal;

	private static ActivityKind ResolveActivityKind(DomainRuntimeType runtimeType) {
		return runtimeType switch {

			// Client applications - user-facing interfaces
			DomainRuntimeType.BlazorWasm => ActivityKind.Client,
			DomainRuntimeType.MauiHybrid => ActivityKind.Client,
			DomainRuntimeType.Console => ActivityKind.Client,

			// Server applications - handle incoming requests
			DomainRuntimeType.WebApi => ActivityKind.Server,
			DomainRuntimeType.WebApp => ActivityKind.Server,

			// Internal/background processing
			DomainRuntimeType.Function => ActivityKind.Internal,
			DomainRuntimeType.UnitTest => ActivityKind.Internal,

			_ => ActivityKind.Internal
		};
	}

}