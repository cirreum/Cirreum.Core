namespace Cirreum.Conductor.Internal;

using Microsoft.Extensions.Logging;

/// <summary>
/// Base wrapper class for requests with typed responses.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
internal abstract class RequestHandlerWrapper<TResponse> {
	/// <summary>
	/// Handles the request by resolving the handler and building the intercept pipeline.
	/// </summary>
	public abstract ValueTask<Result<TResponse>> Handle(
		string environment,
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		ILogger logger,
		CancellationToken cancellationToken);
}