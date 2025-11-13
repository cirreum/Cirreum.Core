namespace Cirreum.Conductor.Intercepts;

using Cirreum.Authorization;
using System.Diagnostics;

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
	IAuthorizationEvaluator evaluator
) : IIntercept<TRequest, TResponse>
	where TRequest : IAuthorizableRequestBase {

	public async ValueTask<Result<TResponse>> HandleAsync(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken) {

		var requestId = Activity.Current?.SpanId.ToString() ?? Guid.NewGuid().ToString("N")[..16];
		var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

		var authResult = await evaluator.Evaluate(request, requestId, correlationId, cancellationToken);
		if (!authResult.IsSuccess) {
			return Result<TResponse>.Fail(authResult.Error!);
		}

		return await next(cancellationToken);

	}

}