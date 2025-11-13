namespace Cirreum.Conductor.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Concrete wrapper implementation for requests without typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
	where TRequest : class, IRequest {

	public override async ValueTask<Result> Handle(
		IRequest request,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken) {

		var typedRequest = (TRequest)request;
		var requestName = typeof(TRequest).Name;

		logger.DispatchingRequest(requestName);

		var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
		if (handler is null) {
			logger.NoHandlerRegistered(requestName);
			return Result.Fail(new InvalidOperationException(
				$"No handler registered for request type '{requestName}'"));
		}

		var handlerTypeName = handler.GetType().Name;

		// Get all intercepts for this request type (using Unit as TResponse)
		var intercepts = serviceProvider
			.GetServices<IIntercept<TRequest, Unit>>()
			.ToList();

		if (intercepts.Count == 0) {
			try {
				return await handler.HandleAsync(typedRequest, cancellationToken);
			} catch (Exception ex) {
				logger.HandlerThrewException(handlerTypeName, ex);
				return Result.Fail(ex);
			}
		}

		logger.BuildingPipeline(intercepts.Count, requestName);

		// Execute the pipeline recursively
		try {
			var unitResult = await ExecutePipelineAsync(
				typedRequest,
				handler,
				intercepts,
				0,
				cancellationToken);

			// Convert Result<Unit> back to Result
			return unitResult.IsSuccess
				? Result.Success
				: Result.Fail(unitResult.Error ?? new Exception("Unknown Exception"));

		} catch (Exception ex) {
			logger.HandlerThrewException(handlerTypeName, ex);
			return Result.Fail(ex);
		}
	}

	private static async ValueTask<Result<Unit>> ExecutePipelineAsync(
		TRequest request,
		IRequestHandler<TRequest> handler,
		List<IIntercept<TRequest, Unit>> intercepts,
		int index,
		CancellationToken cancellationToken) {

		if (index >= intercepts.Count) {
			// Base case: execute the actual handler and convert to Result<Unit>
			var result = await handler.HandleAsync(request, cancellationToken);
			return result; // Implicit conversion from Result to Result<Unit>
		}

		// Recursive case: execute current intercept with continuation
		var currentIntercept = intercepts[index];
		return await currentIntercept.HandleAsync(
			request,
			ct => ExecutePipelineAsync(request, handler, intercepts, index + 1, ct),
			cancellationToken);
	}
}