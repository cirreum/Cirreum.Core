namespace Cirreum.Authorization;

using Cirreum.Exceptions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service responsible for evaluating authorization rules against any resource type.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a centralized mechanism for evaluating authorization rules on any <see cref="IAuthorizableResource"/>
/// via an <see cref="AuthorizationValidatorBase{TResource}"/>.
/// </para>
/// <para>
/// For Conductor requests: Authorization is automatically handled by the pipeline intercept.
/// You do not need to use this evaluator directly. Instead, implement <see cref="IAuthorizableResource"/> on your request
/// and create a corresponding <see cref="AuthorizationValidatorBase{TResource}"/> for rule enforcement.
/// </para>
/// <para>
/// For other use cases, this service can be injected directly into your components to perform authorization checks, ensuring 
/// consistent ABAC (Attribute-Based Access Control) enforcement across various scenarios:
/// <list type="bullet">
/// <item><description>Domain entities within service methods</description></item>
/// <item><description>UI components or views</description></item>
/// <item><description>API endpoints outside the request pipeline</description></item>
/// <item><description>Background jobs or scheduled tasks</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IAuthorizationEvaluator {
	/// <summary>
	/// Evaluates authorization rules for the specified resource and returns a <see cref="Result"/>.
	/// </summary>
	/// <remarks>
	/// Use this method when you want to handle authorization failures gracefully as part of your application flow.
	/// Failed authorization returns a <see cref="Result"/> containing the appropriate exception
	/// (<see cref="UnauthenticatedAccessException"/> or <see cref="ForbiddenAccessException"/>).
	/// </remarks>
	/// <typeparam name="TResource">The type of the <see cref="IAuthorizableResource"/> being evaluated.</typeparam>
	/// <param name="resource">The resource instance to evaluate authorization for.</param>
	/// <param name="requestId">A unique identifier for tracking this authorization request across logs and telemetry.</param>
	/// <param name="correlationId">A unique identifier for correlating related operations across service boundaries.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <exception cref="InvalidOperationException">Thrown when no applicable authorization rules are configured for the resource.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> containing a <see cref="Result"/> that indicates whether authorization succeeded.
	/// On failure, the result contains the authorization exception.
	/// </returns>
	ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource;

	/// <summary>
	/// Enforces authorization rules for the specified resource by throwing an exception if access is denied.
	/// </summary>
	/// <remarks>
	/// Use this method when unauthorized access should halt execution immediately.
	/// This is appropriate for scenarios where authorization failure is an exceptional condition
	/// that cannot be handled gracefully.
	/// </remarks>
	/// <typeparam name="TResource">The type of the <see cref="IAuthorizableResource"/> being evaluated.</typeparam>
	/// <param name="resource">The resource instance to evaluate authorization for.</param>
	/// <param name="requestId">A unique identifier for tracking this authorization request across logs and telemetry.</param>
	/// <param name="correlationId">A unique identifier for correlating related operations across service boundaries.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <exception cref="UnauthenticatedAccessException">Thrown when the current user is not authenticated.</exception>
	/// <exception cref="ForbiddenAccessException">Thrown when the current user lacks sufficient authorization.</exception>
	/// <exception cref="InvalidOperationException">Thrown when no applicable authorization rules are configured for the resource.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous authorization operation.</returns>
	ValueTask Enforce<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource;
}