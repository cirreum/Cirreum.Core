namespace Cirreum.Authorization.Operations;

using Cirreum.Conductor;

/// <summary>
/// Intercept behavior that enforces authorization rules for any <see cref="IAuthorizableOperationBase"/>
/// implementations. See <see cref="IAuthorizableOperation"/>, <see cref="IAuthorizableOperation{TResponse}"/>,
/// and the grant interfaces for the interfaces that should be implemented directly.
/// </summary>
/// <remarks>
/// <para>
/// Convention-based pipeline behavior that automatically enforces authorization for
/// commands and queries implementing <see cref="IAuthorizableObject"/>.
/// </para>
/// <para>
/// Note: This is just one way to consume authorization validators. The underlying
/// <see cref="IAuthorizationEvaluator"/> can be used directly for permission checks
/// anywhere in the application.
/// </para>
/// </remarks>
sealed class Authorization<TOperation, TResponse>(
	IAuthorizationEvaluator authorizer
) : IIntercept<TOperation, TResponse>
	where TOperation : IAuthorizableOperationBase {

	public async Task<Result<TResponse>> HandleAsync(
		OperationContext<TOperation> context,
		OperationHandlerDelegate<TOperation, TResponse> next,
		CancellationToken cancellationToken) {

		var authResult = await authorizer
			.Evaluate(
				context.Operation,
				context.UserState,
				cancellationToken)
			.ConfigureAwait(false);

		if (authResult.IsFailure) {
			return Result<TResponse>.Fail(authResult.Error);
		}

		return await next(context, cancellationToken);

	}

}