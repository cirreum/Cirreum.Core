namespace Cirreum.Conductor.Intercepts;

using Cirreum.Authorization;
using Cirreum.Conductor;

/// <summary>
/// Intercept behavior that enforces authorization rules for any <see cref="IAuthorizableRequestBase"/>
/// implementations. See <see cref="IAuthorizableCommand"/>, <see cref="IAuthorizableCommand{TResponse}"/>,
/// <see cref="IAuthorizableQuery{TResponse}"/>, and their owner-scoped variants for the interfaces
/// that should be implemented directly.
/// </summary>
/// <remarks>
/// <para>
/// Convention-based pipeline behavior that automatically enforces authorization for
/// commands and queries implementing <see cref="IAuthorizableResource"/>.
/// </para>
/// <para>
/// Note: This is just one way to consume authorization validators. The underlying
/// <see cref="IAuthorizationEvaluator"/> can be used directly for permission checks
/// anywhere in the application.
/// </para>
/// </remarks>
sealed class Authorization<TRequest, TResponse>(
	IAuthorizationEvaluator authorizer
) : IIntercept<TRequest, TResponse>
	where TRequest : IAuthorizableRequestBase {

	public async Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken) {

		var authResult = await authorizer
			.Evaluate(
				context.Request,
				context.Operation,
				cancellationToken)
			.ConfigureAwait(false);

		if (authResult.IsFailure) {
			return Result<TResponse>.Fail(authResult.Error);
		}

		return await next(context, cancellationToken);

	}

}