namespace Cirreum.Conductor;

/// <summary>
/// Represents a delegate that handles a request with context and returns a result asynchronously.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="context"></param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to observe cancellation requests.</param>
/// <returns><see cref="Result{T}"/> of type <typeparamref name="TResponse"/>.</returns>
public delegate Task<Result<TResponse>> RequestHandlerDelegate<TRequest, TResponse>(
	RequestContext<TRequest> context,
	CancellationToken cancellationToken)
	where TRequest : notnull;