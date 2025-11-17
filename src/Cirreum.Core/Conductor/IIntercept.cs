namespace Cirreum.Conductor;

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
	Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken);
}