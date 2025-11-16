namespace Cirreum.Conductor.Intercepts;

using Cirreum.Authorization;
using Cirreum.Conductor;

/// <summary>
/// Intercept behavior that enforces authorization rules for any <see cref="IAuthorizableRequestBase"/>
/// implementations. See <see cref="IAuthorizableRequest"/> and <see cref="IAuthorizableRequest{TResponse}"/>
/// for interface that should be implemented directly.
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
public sealed class Authorization<TRequest, TResponse>(
	IAuthorizationEvaluator authorizor
) : IIntercept<TRequest, TResponse>
	where TRequest : IAuthorizableRequestBase {

	public async ValueTask<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken) {

		// Pass the OperationContext we already have from the pipeline
		// This avoids rebuilding user state and environment information
		var authResult = await authorizor.Evaluate(
			context.Request,
			context.Operation,
			cancellationToken);

		if (authResult.IsFailure) {
			return Result<TResponse>.Fail(authResult.Error);
		}

		return await next(cancellationToken);
	}
}