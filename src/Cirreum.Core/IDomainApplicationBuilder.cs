namespace Cirreum;

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

}