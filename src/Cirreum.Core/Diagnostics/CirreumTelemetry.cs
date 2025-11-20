namespace Cirreum.Diagnostics;

/// <summary>
/// Centralized telemetry constants for the Cirreum framework.
/// Use these names when configuring OpenTelemetry instrumentation.
/// </summary>
public static class CirreumTelemetry {

	/// <summary>
	/// The version of the Cirreum library.
	/// </summary>
	public static readonly string Version =
		typeof(DomainRuntimeType).Assembly.GetName().Version?.ToString() ?? "1.0.0";

	/// <summary>
	/// All activity source names used by Cirreum components.
	/// </summary>
	public static class ActivitySources {
		/// <summary>
		/// Activity source for Conductor dispatcher operations (handlers, intercepts, validation, authorization).
		/// </summary>
		public const string ConductorDispatcher = "cirreum.conductor.dispatcher";

		/// <summary>
		/// Activity source for Conductor dispatcher operations (handlers, intercepts, validation, authorization).
		/// </summary>
		public const string ConductorPublisher = "cirreum.conductor.publisher";

		/// <summary>
		/// Activity source for remote service client operations (HTTP, gRPC).
		/// </summary>
		public const string RemoteServicesClient = "cirreum.remote-services.client";

	}

	/// <summary>
	/// All meter names used by Cirreum components.
	/// </summary>
	public static class Meters {

		/// <summary>
		/// Meter for Conductor dispatcher metrics (request counts, durations, failures).
		/// </summary>
		public const string ConductorDispatcher = "cirreum.conductor.dispatcher";

		/// <summary>
		/// Meter for Conductor dispatcher metrics (request counts, durations, failures).
		/// </summary>
		public const string ConductorPublisher = "cirreum.conductor.publisher";

		/// <summary>
		/// Meter for Conductor cache metrics (hits, misses, evictions, durations).
		/// </summary>
		public const string ConductorCache = "cirreum.conductor.cache";

		/// <summary>
		/// Meter for remote service client metrics (request counts, durations, status codes).
		/// </summary>
		public const string RemoteServicesClient = "cirreum.remote-services.client";
	}

}