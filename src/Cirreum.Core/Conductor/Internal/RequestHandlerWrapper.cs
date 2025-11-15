namespace Cirreum.Conductor.Internal;

using Microsoft.Extensions.Logging;

/// <summary>
/// Base wrapper class for requests without typed responses.
/// </summary>
internal abstract class RequestHandlerWrapper {
	/// <summary>
	/// Handles the request by resolving the handler and building the intercept pipeline.
	/// </summary>
	public abstract ValueTask<Result> Handle(
		string environment,
		IRequest request,
		IServiceProvider serviceProvider,
		IPublisher publisher,
		ILogger logger,
		CancellationToken cancellationToken);
}