namespace Cirreum.Presence;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Base interface for configuring user presence services.
/// </summary>
public interface IUserPresenceBuilder {
	/// <summary>
	/// The <see cref="IServiceCollection"/>.
	/// </summary>
	IServiceCollection Services { get; }
}