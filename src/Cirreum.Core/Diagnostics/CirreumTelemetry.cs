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
		public const string ConductorDispatcher = "Cirreum.Conductor.Dispatcher";

		/// <summary>
		/// Activity source for Conductor publisher operations.
		/// </summary>
		public const string ConductorPublisher = "Cirreum.Conductor.Publisher";

		/// <summary>
		/// Activity source for remote service client operations (HTTP, gRPC).
		/// </summary>
		public const string RemoteServicesClient = "Cirreum.RemoteServices.Client";
	}

	/// <summary>
	/// All meter names used by Cirreum components.
	/// </summary>
	public static class Meters {
		/// <summary>
		/// Meter for Conductor dispatcher metrics (request counts, durations, failures).
		/// </summary>
		public const string ConductorDispatcher = "Cirreum.Conductor.Dispatcher";

		/// <summary>
		/// Meter for Conductor publisher metrics.
		/// </summary>
		public const string ConductorPublisher = "Cirreum.Conductor.Publisher";

		/// <summary>
		/// Meter for Conductor Cacheable Query metrics (hits, misses, evictions, durations).
		/// </summary>
		public const string ConductorCache = "Cirreum.Conductor.QueryCache";

		/// <summary>
		/// Meter for remote service client metrics (request counts, durations, status codes).
		/// </summary>
		public const string RemoteServicesClient = "Cirreum.RemoteServices.Client";
	}

}