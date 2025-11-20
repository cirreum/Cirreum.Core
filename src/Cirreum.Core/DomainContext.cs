namespace Cirreum;

using System.Diagnostics;

internal static class DomainContext {

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