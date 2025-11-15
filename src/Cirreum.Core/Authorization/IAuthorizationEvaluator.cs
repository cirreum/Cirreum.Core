namespace Cirreum.Authorization;

/// <summary>
/// Defines the contract for evaluating authorization rules for resources.
/// </summary>
public interface IAuthorizationEvaluator {

	/// <summary>
	/// Evaluates authorization for a resource in ad-hoc mode.
	/// Builds the operation context from scratch by retrieving current user state.
	/// </summary>
	/// <typeparam name="TResource">The type of resource being authorized.</typeparam>
	/// <param name="resource">The resource to authorize.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A result indicating success or failure of the authorization check.</returns>
	/// <remarks>
	/// Use this overload when calling authorization outside of the request pipeline,
	/// such as in background jobs, API controllers, or other ad-hoc scenarios.
	/// </remarks>
	ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource;

	/// <summary>
	/// Evaluates authorization for a resource using an existing operation context.
	/// </summary>
	/// <typeparam name="TResource">The type of resource being authorized.</typeparam>
	/// <param name="resource">The resource to authorize.</param>
	/// <param name="operation">The operation context containing user state and environment information.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A result indicating success or failure of the authorization check.</returns>
	/// <remarks>
	/// Use this overload when calling from the Conductor pipeline or when you already
	/// have an OperationContext available. This avoids rebuilding user state and
	/// environment information, improving performance.
	/// </remarks>
	ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		OperationContext operation,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource;

}