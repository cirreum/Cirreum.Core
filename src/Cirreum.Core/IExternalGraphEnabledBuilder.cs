namespace Cirreum;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interface for external graph-enabled authentication builders.
/// Provides access to service collection and user profile enrichment capabilities.
/// </summary>
public interface IExternalGraphEnabledBuilder {
	/// <summary>
	/// Gets the service collection.
	/// </summary>
	IServiceCollection Services { get; }

	/// <summary>
	/// Gets the user profile enrichment builder.
	/// </summary>
	IUserProfileEnrichmentBuilder Enrichment { get; }
}