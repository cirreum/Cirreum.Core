namespace Cirreum;

using Cirreum.Diagnostics;
using OpenTelemetry;

/// <summary>
/// Extension methods for configuring Cirreum telemetry with OpenTelemetry.
/// </summary>
public static class CirreumTelemetryExtensions {

	/// <summary>
	/// Adds Cirreum instrumentation to OpenTelemetry for distributed tracing and metrics.
	/// </summary>
	/// <param name="builder">The OpenTelemetry builder.</param>
	/// <returns>The builder for chaining.</returns>
	/// <remarks>
	/// <para>
	/// This method registers all Cirreum activity sources and meters for comprehensive observability:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// <term>Conductor Operations</term>
	/// <description>Traces and metrics for request handlers, validation, authorization, and intercepts</description>
	/// </item>
	/// <item>
	/// <term>Cache Operations</term>
	/// <description>Metrics for cache hits, misses, evictions, and operation durations</description>
	/// </item>
	/// <item>
	/// <term>Remote Services</term>
	/// <description>Traces and metrics for HTTP client operations, including status codes and durations</description>
	/// </item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code>
	/// builder.Services.AddOpenTelemetry()
	///     .ConfigureResource(resource => resource.AddService("MyApp"))
	///     .AddCirreumInstrumentation()
	///     .UseOtlpExporter();
	/// </code>
	/// </example>
	public static OpenTelemetryBuilder AddCirreum(
		this OpenTelemetryBuilder builder) {

		return builder
			.WithTracing(tracing => tracing
				.AddSource(CirreumTelemetry.ActivitySources.ConductorDispatcher)
				.AddSource(CirreumTelemetry.ActivitySources.ConductorPublisher)
				.AddSource(CirreumTelemetry.ActivitySources.RemoteServicesClient))
			.WithMetrics(metrics => metrics
				.AddMeter(CirreumTelemetry.Meters.ConductorDispatcher)
				.AddMeter(CirreumTelemetry.Meters.ConductorPublisher)
				.AddMeter(CirreumTelemetry.Meters.ConductorCache)
				.AddMeter(CirreumTelemetry.Meters.RemoteServicesClient));
	}
}