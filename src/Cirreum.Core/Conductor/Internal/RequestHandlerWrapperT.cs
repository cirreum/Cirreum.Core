namespace Cirreum.Conductor.Internal;
/// <summary>
/// Base wrapper class for requests with typed responses.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
internal abstract class RequestHandlerWrapper<TResponse> {

	/// <summary>
	/// Handles the request by resolving the handler and building the intercept pipeline.
	/// </summary>
	public abstract Task<Result<TResponse>> HandleAsync(
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		CancellationToken cancellationToken);
}