namespace Cirreum;

using Cirreum.Presence;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interface for graph-enabled authentication builders.
/// Provides access to service collection, user profile enrichment, and user presence capabilities.
/// </summary>
public interface IGraphEnabledBuilder {
	/// <summary>
	/// Gets the service collection.
	/// </summary>
	IServiceCollection Services { get; }

	/// <summary>
	/// Gets the user profile enrichment builder.
	/// </summary>
	IUserProfileEnrichmentBuilder Enrichment { get; }

	/// <summary>
	/// Gets the user presence builder.
	/// </summary>
	IUserPresenceBuilder Presence { get; }
}