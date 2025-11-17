namespace Cirreum.Conductor;

using Cirreum.Conductor.Internal;
using System;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/> and <see cref="IConductor"/> 
/// that routes requests to their handlers through a pipeline of intercepts,
/// and publishes notifications to all registered handlers.
/// </summary>
/// <remarks>
/// This dispatcher uses a wrapper-based caching strategy to avoid reflection overhead in the hot path.
/// Request type wrappers are created once and cached for the lifetime of the application.
/// </remarks>
sealed class Dispatcher(
	IServiceProvider serviceProvider,
	IPublisher publisher
) : IConductor {

	#region IDispatcher Implementation

	/// <inheritdoc />
	public Task<Result> DispatchAsync<TRequest>(
		TRequest request,
		CancellationToken cancellationToken = default)
		where TRequest : IRequest {

		ArgumentNullException.ThrowIfNull(request);

		var wrapper = TypeCache.VoidHandlers.GetOrAdd(request.GetType(), static requestType => {
			var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
			return (RequestHandlerWrapper)(Activator.CreateInstance(wrapperType)
				?? throw new InvalidOperationException($"Could not create wrapper for {requestType.Name}"));
		});

		return wrapper.HandleAsync(
			request,
			serviceProvider,
			publisher,
			cancellationToken);
	}

	/// <inheritdoc />
	public Task<Result<TResponse>> DispatchAsync<TResponse>(
		IRequest<TResponse> request,
		CancellationToken cancellationToken = default) {

		ArgumentNullException.ThrowIfNull(request);

		var wrapper = (RequestHandlerWrapper<TResponse>)TypeCache.ResponseHandlers.GetOrAdd(
			request.GetType(),
			static requestType => {
				var wrapperType = typeof(RequestHandlerWrapperImpl<,>)
					.MakeGenericType(requestType, typeof(TResponse));
				return Activator.CreateInstance(wrapperType)
					?? throw new InvalidOperationException($"Could not create wrapper for {requestType.Name}");
			});

		return wrapper.HandleAsync(
			request,
			serviceProvider,
			publisher,
			cancellationToken);
	}

	#endregion

	#region IPublisher Implementation

	/// <inheritdoc />
	public Task<Result> PublishAsync<TNotification>(
		TNotification notification,
		PublisherStrategy? strategy = null,
		CancellationToken cancellationToken = default)
		where TNotification : INotification {

		return publisher.PublishAsync(notification, strategy, cancellationToken);
	}

	#endregion
}