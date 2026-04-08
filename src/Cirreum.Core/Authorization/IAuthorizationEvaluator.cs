namespace Cirreum.Authorization;

using Cirreum.Security;

/// <summary>
/// Defines the contract for evaluating authorization rules against <see cref="IAuthorizableObject"/> types.
/// </summary>
public interface IAuthorizationEvaluator {

	/// <summary>
	/// Evaluates authorization for an <see cref="IAuthorizableObject"/> in ad-hoc mode.
	/// Retrieves the current user state internally via <see cref="IUserStateAccessor"/>.
	/// </summary>
	/// <typeparam name="TAuthorizableObject">The type of <see cref="IAuthorizableObject"/> being authorized.</typeparam>
	/// <param name="authorizableObject">The authorizable object to evaluate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A result indicating success or failure of the authorization check.</returns>
	/// <remarks>
	/// Use this overload when calling authorization outside of the request pipeline,
	/// such as in background jobs, API controllers, or other ad-hoc scenarios.
	/// </remarks>
	ValueTask<Result> Evaluate<TAuthorizableObject>(
		TAuthorizableObject authorizableObject,
		CancellationToken cancellationToken = default)
		where TAuthorizableObject : IAuthorizableObject;

	/// <summary>
	/// Evaluates authorization for an <see cref="IAuthorizableObject"/> using an existing user state.
	/// </summary>
	/// <typeparam name="TAuthorizableObject">The type of <see cref="IAuthorizableObject"/> being authorized.</typeparam>
	/// <param name="authorizableObject">The authorizable object to evaluate.</param>
	/// <param name="userState">The caller's state, already retrieved by the pipeline.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A result indicating success or failure of the authorization check.</returns>
	/// <remarks>
	/// Use this overload when calling from the Conductor pipeline or when you already
	/// have the caller's <see cref="IUserState"/>. This avoids a redundant
	/// <see cref="IUserStateAccessor.GetUser"/> call.
	/// </remarks>
	ValueTask<Result> Evaluate<TAuthorizableObject>(
		TAuthorizableObject authorizableObject,
		IUserState userState,
		CancellationToken cancellationToken = default)
		where TAuthorizableObject : IAuthorizableObject;

}
