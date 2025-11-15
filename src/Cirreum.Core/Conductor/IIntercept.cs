namespace Cirreum.Conductor;

/// <summary>
/// Represents a delegate that handles a request and returns a result asynchronously.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to observe cancellation requests.</param>
/// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation, containing a <see cref="Result{T}"/>
/// of type <typeparamref name="TResponse"/>.</returns>
public delegate ValueTask<Result<TResponse>> RequestHandlerDelegate<TResponse>(
	CancellationToken cancellationToken);

/// <summary>
/// Defines a contract for intercepting and processing a request before or after it is handled by the next delegate in
/// the pipeline.
/// </summary>
/// <remarks>Implementations of this interface can be used to add cross-cutting concerns, such as logging,
/// validation, or performance monitoring, to the request handling pipeline. The interceptor can modify the request, the
/// response, or both.</remarks>
/// <typeparam name="TRequest">The type of the request to be intercepted. Must be a non-nullable type.</typeparam>
/// <typeparam name="TResponse">The type of the response returned after processing the request.</typeparam>
public interface IIntercept<TRequest, TResponse> where TRequest : notnull {
	ValueTask<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken);

}