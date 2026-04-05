namespace Cirreum.Conductor;

/// <summary>
/// Defines a contract for intercepting and processing a request before or after it is handled by the next delegate in
/// the pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface can be used to add cross-cutting concerns, such as logging,
/// validation, or performance monitoring, to the request handling pipeline. The interceptor can
/// modify the request, the response, or both.
/// </para>
/// <para>
/// <b>Contract:</b> Implementations MUST invoke <c>next</c> at most once per call to
/// <c>HandleAsync</c>. The dispatcher walks the pipeline with a shared mutable cursor for zero-
/// allocation dispatch; calling <c>next</c> a second time would advance past the intended
/// interceptor. Interceptors that need retry, loop, or fan-out semantics must build their own
/// cursor/snapshot and re-dispatch explicitly rather than calling <c>next</c> repeatedly.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The type of the request to be intercepted. Must be a non-nullable type.</typeparam>
/// <typeparam name="TResponse">The type of the response returned after processing the request.</typeparam>
public interface IIntercept<TRequest, TResponse> where TRequest : notnull {
	Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken);
}