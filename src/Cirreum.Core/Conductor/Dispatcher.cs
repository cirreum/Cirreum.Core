namespace Cirreum.Conductor;

using Cirreum.Conductor.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/> that routes requests to their handlers
/// through a pipeline of intercepts.
/// </summary>
/// <remarks>
/// This dispatcher uses a wrapper-based caching strategy to avoid reflection overhead in the hot path.
/// Request type wrappers are created once and cached for the lifetime of the application.
/// </remarks>
public sealed class Dispatcher(
	IDomainEnvironment domainEnvironment,
	IServiceProvider serviceProvider,
	IPublisher publisher,
	ILogger<Dispatcher> logger
) : IDispatcher {

	private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> _voidHandlerCache = new();
	private static readonly ConcurrentDictionary<Type, object> _responseHandlerCache = new();

	/// <inheritdoc />
	public async Task<Result> DispatchAsync(
		IRequest request,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);
		try {

			var wrapper = _voidHandlerCache.GetOrAdd(request.GetType(), static rt => {
				var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(rt);
				return (RequestHandlerWrapper)(Activator.CreateInstance(wrapperType)
					?? throw new InvalidOperationException($"Could not create wrapper for {rt.Name}"));
			});

			var result = await wrapper.HandleAsync(
				domainEnvironment,
				request,
				serviceProvider,
				publisher,
				logger,
				cancellationToken);

			return result;

		} catch (Exception ex) {
			return Result.Fail(ex);
		}
	}

	/// <inheritdoc />
	public Task<Result<TResponse>> DispatchAsync<TResponse>(
		IRequest<TResponse> request,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);
		try {

			var wrapper = (RequestHandlerWrapper<TResponse>)_responseHandlerCache.GetOrAdd(request.GetType(), rt => {
				var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(rt, typeof(TResponse));
				return Activator.CreateInstance(wrapperType)
					?? throw new InvalidOperationException($"Could not create wrapper for {rt.Name}");
			});

			return wrapper.HandleAsync(
				domainEnvironment,
				request,
				serviceProvider,
				publisher,
				logger,
				cancellationToken);

		} catch (Exception ex) {
			return Task.FromResult(Result<TResponse>.Fail(ex));
		}
	}

}