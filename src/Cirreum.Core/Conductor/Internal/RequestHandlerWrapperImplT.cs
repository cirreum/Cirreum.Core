namespace Cirreum.Conductor.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Concrete wrapper implementation for requests with typed responses.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse>
  : RequestHandlerWrapper<TResponse>
	where TRequest : class, IRequest<TResponse> {

	public override async ValueTask<Result<TResponse>> Handle(
		IRequest<TResponse> request,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken) {

		var typedRequest = (TRequest)request;
		var requestName = typeof(TRequest).Name;

		logger.DispatchingRequest(requestName);

		var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
		if (handler is null) {
			logger.NoHandlerRegistered(requestName);
			return Result<TResponse>.Fail(new InvalidOperationException(
				$"No handler registered for request type '{requestName}'"));
		}
		var handlerTypeName = handler.GetType().Name;

		// Get all intercepts for this request type (using Unit as TResponse)	
		var intercepts = serviceProvider
			.GetServices<IIntercept<TRequest, TResponse>>()
			.ToList();
		if (intercepts.Count == 0) {
			try {
				return await handler.HandleAsync(typedRequest, cancellationToken);
			} catch (Exception ex) {
				logger.HandlerThrewException(handlerTypeName, ex);
				return Result<TResponse>.Fail(ex);
			}
		}

		logger.BuildingPipeline(intercepts.Count, requestName);

		// Execute the pipeline recursively
		try {
			return await ExecutePipelineAsync(
				typedRequest,
				handler,
				intercepts,
				0,
				cancellationToken);
		} catch (Exception ex) {
			logger.HandlerThrewException(handlerTypeName, ex);
			return Result<TResponse>.Fail(ex);
		}

	}

	private static async ValueTask<Result<TResponse>> ExecutePipelineAsync(
		TRequest request,
		IRequestHandler<TRequest, TResponse> handler,
		List<IIntercept<TRequest, TResponse>> intercepts,
		int index,
		CancellationToken cancellationToken) {

		if (index >= intercepts.Count) {
			// Base case: execute the actual handler
			return await handler.HandleAsync(request, cancellationToken);
		}

		// Recursive case: execute current intercept with continuation
		var currentIntercept = intercepts[index];
		return await currentIntercept.HandleAsync(
			request,
			ct => ExecutePipelineAsync(request, handler, intercepts, index + 1, ct),
			cancellationToken);
	}

}