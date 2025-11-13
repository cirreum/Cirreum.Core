namespace Cirreum;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Core authentication builder.
/// </summary>
public interface IAuthenticationBuilder {
	/// <summary>
	/// The <see cref="IServiceCollection"/>.
	/// </summary>
	IServiceCollection Services { get; }
}