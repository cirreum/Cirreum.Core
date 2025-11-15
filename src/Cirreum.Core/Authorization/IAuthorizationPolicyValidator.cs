namespace Cirreum.Authorization;

using FluentValidation.Results;

/// <summary>
/// Defines a contract for validating authorization policies against resources within specific execution contexts.
/// </summary>
/// <remarks>
/// <para>
/// This interface establishes the foundation for implementing authorization policy validators that can determine
/// their applicability to resources and perform validation logic. Implementations are responsible for defining
/// policy-specific validation rules and determining when those rules should be applied.
/// </para>
/// <para>
/// Validators implementing this interface support conditional application based on resource characteristics
/// and execution context, enabling flexible and context-aware authorization scenarios across different
/// application runtime environments.
/// </para>
/// </remarks>
public interface IAuthorizationPolicyValidator {

	/// <summary>
	/// Gets the unique name that identifies this authorization policy.
	/// </summary>
	/// <value>
	/// A string that uniquely identifies the authorization policy implemented by this validator.
	/// This name is used for policy registration, lookup, and diagnostic purposes.
	/// </value>
	string PolicyName { get; }

	/// <summary>
	/// Gets the execution priority order for this validator relative to other validators.
	/// </summary>
	/// <value>
	/// An integer representing the execution order, where lower values indicate higher priority
	/// and earlier execution. Validators are typically executed in ascending order of this value.
	/// </value>
	int Order { get; }

	/// <summary>
	/// Gets the application runtime types that this validator is designed to operate within.
	/// </summary>
	/// <value>
	/// An array of <see cref="ApplicationRuntimeType"/> values specifying the runtime environments
	/// where this validator is applicable and should be executed.
	/// </value>
	ApplicationRuntimeType[] SupportedRuntimeTypes { get; }

	/// <summary>
	/// Determines whether this validator should be applied to the specified resource within the given execution context.
	/// </summary>
	/// <typeparam name="TResource">
	/// The type of the resource being evaluated. Must be non-nullable and implement <see cref="IAuthorizableResource"/>.
	/// </typeparam>
	/// <param name="resource">
	/// The resource instance to evaluate for validator applicability. Cannot be <see langword="null"/>.
	/// </param>
	/// <param name="runtimeType">
	/// The application runtime type in which the authorization is being evaluated.
	/// </param>
	/// <param name="timestamp">
	/// The timestamp of when the authorization check is occurring, useful for time-based policies.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if this validator should be applied to the specified resource within the given context;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// This method allows validators to conditionally apply their logic based on resource characteristics,
	/// runtime type, timestamp, or other environmental factors. Implementations should return <see langword="true"/>
	/// only when the validator's policy is relevant to the provided resource and context combination.
	/// </remarks>
	bool AppliesTo<TResource>(
		TResource resource,
		ApplicationRuntimeType runtimeType,
		DateTimeOffset timestamp)
		where TResource : notnull, IAuthorizableResource;

	/// <summary>
	/// Asynchronously validates the authorization of a resource within the specified authorization context.
	/// </summary>
	/// <typeparam name="TResource">
	/// The type of the resource being authorized. Must be non-nullable and implement <see cref="IAuthorizableResource"/>.
	/// </typeparam>
	/// <param name="context">
	/// The authorization context containing the resource and all necessary information for performing the validation.
	/// Cannot be <see langword="null"/>.
	/// </param>
	/// <param name="cancellationToken">
	/// A cancellation token that can be used to cancel the validation operation.
	/// Defaults to <see cref="CancellationToken.None"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{ValidationResult}"/> representing the asynchronous validation operation.
	/// The result contains a <see cref="ValidationResult"/> indicating whether authorization was granted,
	/// along with any validation errors or additional diagnostic information.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method performs the core authorization validation logic for the policy. Implementations should
	/// examine the provided context and apply policy-specific rules to determine whether the resource
	/// access should be authorized.
	/// </para>
	/// <para>
	/// The authorization context provides access to the OperationContext (via context.Operation), which contains
	/// runtime type, timestamp, user state, and other execution details. Validators can use these to implement
	/// sophisticated authorization logic.
	/// </para>
	/// <para>
	/// A successful validation (indicated by <see cref="ValidationResult.IsValid"/> being <see langword="true"/>)
	/// means the authorization policy permits the requested access. A failed validation should include
	/// descriptive error information in the <see cref="ValidationResult.Errors"/> collection.
	/// </para>
	/// </remarks>
	Task<ValidationResult> ValidateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken = default)
		where TResource : notnull, IAuthorizableResource;
}