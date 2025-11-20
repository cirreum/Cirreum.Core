namespace Cirreum;

using Cirreum.Conductor.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public interface IDomainApplicationBuilder {

	/// <summary>
	/// Gets a collection of logging providers for the application to compose. This is useful for adding new logging providers.
	/// </summary>
	ILoggingBuilder Logging { get; }

	/// <summary>
	/// Gets a collection of services for the application to compose. This is useful for adding user provided or framework provided services.
	/// </summary>
	IServiceCollection Services { get; }

	/// <summary>
	/// Configures Conductor settings and custom intercepts for the domain services.
	/// </summary>
	/// <param name="configure">Action to configure Conductor options.</param>
	/// <returns>The builder for method chaining.</returns>
	IDomainApplicationBuilder ConfigureConductor(Action<ConductorOptionsBuilder> configure);
}