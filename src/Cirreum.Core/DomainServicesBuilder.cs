namespace Cirreum;

using System.Reflection;

/// <summary>
/// Builder class for configuring domain service assemblies during application startup.
/// </summary>
/// <remarks>
/// This class provides a fluent API for registering assemblies that contain domain
/// services, validators, and authorization handlers to be discovered during service registration.
/// </remarks>
public class DomainServicesBuilder {

	/// <summary>
	/// Gets the collection of assemblies that have been registered with this builder.
	/// </summary>
	/// <remarks>
	/// This collection ensures that assemblies containing domain services are loaded and preserved
	/// during application startup, serving two critical purposes:
	/// <para>
	/// <strong>Assembly Loading:</strong> By referencing types through the builder methods,
	/// we ensure that assemblies are loaded into the current AppDomain. This is essential because
	/// reflection-based scanning (used by MediatR, FluentValidation, and other discovery mechanisms)
	/// can only find types in assemblies that are already loaded.
	/// </para>
	/// <para>
	/// <strong>Linker Preservation:</strong> The type references created through generic
	/// methods like <see cref="AddAssemblyContaining{T}()"/> prevent the compiler and linker
	/// from optimizing away seemingly "unused" types, particularly important in AOT compilation
	/// scenarios or when using aggressive code trimming.
	/// </para>
	/// <para>
	/// The collection maintains uniqueness automatically, preventing duplicate assembly registrations.
	/// </para>
	/// </remarks>
	internal HashSet<Assembly> Assemblies { get; } = [];

	/// <summary>
	/// Gets a read-only view of the registered assemblies.
	/// </summary>
	public IReadOnlyCollection<Assembly> GetAssemblies() => this.Assemblies;

	/// <summary>
	/// Adds an assembly to the collection of assemblies to be scanned for domain services.
	/// </summary>
	/// <param name="assembly">The assembly to add.</param>
	/// <returns>The <see cref="DomainServicesBuilder"/> instance for method chaining.</returns>
	public DomainServicesBuilder AddAssembly(Assembly assembly) {
		ArgumentNullException.ThrowIfNull(assembly);
		this.Assemblies.Add(assembly);
		return this;
	}

	/// <summary>
	/// Adds the assembly containing the specified type to the collection of assemblies to be scanned.
	/// </summary>
	/// <typeparam name="T">A type contained in the assembly to add.</typeparam>
	/// <returns>The <see cref="DomainServicesBuilder"/> instance for method chaining.</returns>
	public DomainServicesBuilder AddAssemblyContaining<T>() {
		return this.AddAssembly(typeof(T).Assembly);
	}

	/// <summary>
	/// Adds the assembly containing the specified type to the collection of assemblies to be scanned.
	/// </summary>
	/// <param name="type">A type contained in the assembly to add.</param>
	/// <returns>The <see cref="DomainServicesBuilder"/> instance for method chaining.</returns>
	public DomainServicesBuilder AddAssemblyContaining(Type type) {
		ArgumentNullException.ThrowIfNull(type);
		return this.AddAssembly(type.Assembly);
	}

}