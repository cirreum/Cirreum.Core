namespace Cirreum.Authorization;

using FluentValidation.Results;
using System.Reflection;

/// <summary>
/// Provides a base implementation for authorization policy validators that operate on resources 
/// decorated with specific attributes.
/// </summary>
/// <typeparam name="TAttribute">The type of attribute that this validator operates on. Must inherit from <see cref="Attribute"/>.</typeparam>
/// <remarks>
/// This abstract class serves as a foundation for creating authorization validators that determine 
/// their applicability based on the presence of custom attributes on resource types. It provides 
/// common functionality for attribute detection while allowing derived classes to implement 
/// specific validation logic.
/// </remarks>
public abstract class AttributeValidatorBase<TAttribute>
	: IAuthorizationPolicyValidator where TAttribute : Attribute {

	/// <inheritdoc/>
	public abstract string PolicyName { get; }

	/// <inheritdoc/>
	public abstract int Order { get; }

	/// <inheritdoc/>
	public abstract ApplicationRuntimeType[] SupportedRuntimeTypes { get; }

	/// <inheritdoc/>
	public virtual bool AppliesTo<TResource>(TResource resource, ExecutionContext context)
		where TResource : notnull, IAuthorizableResource =>
		resource.GetType().GetCustomAttribute<TAttribute>() != null;

	/// <summary>
	/// Retrieves a custom attribute of the specified type from the provided resource.
	/// </summary>
	/// <remarks>
	/// This method uses reflection to inspect the type of the provided resource for the specified custom
	/// attribute. If the attribute is not present, the method returns <see langword="null"/>.
	/// </remarks>
	/// <typeparam name="TResource">The type of the resource from which the attribute is retrieved. Must be a non-nullable type.</typeparam>
	/// <param name="resource">The resource object whose type is inspected for the custom attribute. Cannot be <see langword="null"/>.</param>
	/// <returns>An instance of the specified attribute type if found; otherwise, <see langword="null"/>.</returns>
	protected virtual TAttribute? GetAttribute<TResource>(TResource resource) where TResource : notnull =>
		resource.GetType().GetCustomAttribute<TAttribute>();

	/// <inheritdoc/>
	public abstract Task<ValidationResult> ValidateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource;

}