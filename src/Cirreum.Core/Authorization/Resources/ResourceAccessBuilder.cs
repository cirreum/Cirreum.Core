namespace Cirreum.Authorization.Resources;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent builder for registering <see cref="IAccessEntryProvider{T}"/> implementations
/// with the resource access system.
/// </summary>
public sealed class ResourceAccessBuilder(IServiceCollection services) {

	/// <summary>
	/// Registers an <see cref="IAccessEntryProvider{T}"/> for the specified protected resource type.
	/// </summary>
	/// <typeparam name="TResource">The protected resource type.</typeparam>
	/// <typeparam name="TProvider">The provider implementation (registered as Scoped).</typeparam>
	/// <returns>This builder for chaining.</returns>
	public ResourceAccessBuilder AddProvider<TResource, TProvider>()
		where TResource : IProtectedResource
		where TProvider : class, IAccessEntryProvider<TResource> {

		services.AddScoped<IAccessEntryProvider<TResource>, TProvider>();
		return this;
	}
}
