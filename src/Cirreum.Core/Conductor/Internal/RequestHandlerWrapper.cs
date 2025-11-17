namespace Cirreum.Conductor.Internal;
/// <summary>
/// Base wrapper class for requests without typed responses.
/// </summary>
internal abstract class RequestHandlerWrapper {
	/// <summary>
	/// Handles the request by resolving the handler and building the intercept pipeline.
	/// </summary>
	public abstract Task<Result> HandleAsync(
		IRequest request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		CancellationToken cancellationToken);
}