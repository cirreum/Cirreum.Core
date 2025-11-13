namespace Cirreum.Conductor;

/// <summary>
/// Dispatches requests to their appropriate handlers.
/// </summary>
public interface IDispatcher {

	/// <summary>
	/// Dispatches the specified request asynchronously and returns a <see cref="Result"/>.
	/// </summary>
	/// <remarks>
	/// This overload is intended for requests that do not return a typed response,
	/// such as commands that perform state changes or side effects.
	/// </remarks>
	/// <param name="request">The request to dispatch. Must implement <see cref="IRequest"/>.</param>
	/// <param name="cancellationToken">
	/// A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
	/// The returned <see cref="Result"/> indicates whether the request was handled successfully.
	/// </returns>
	ValueTask<Result> DispatchAsync(
		IRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Dispatches the specified request asynchronously and returns a <see cref="Result{T}"/> containing the response.
	/// </summary>
	/// <remarks>
	/// This overload is intended for requests that return a typed response,
	/// such as queries that retrieve data or commands that return confirmation values.
	/// </remarks>
	/// <typeparam name="TResponse">The type of the response returned by the request.</typeparam>
	/// <param name="request">The request to dispatch. Must implement <see cref="IRequest{TResponse}"/>.</param>
	/// <param name="cancellationToken">
	/// A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
	/// The returned <see cref="Result{T}"/> contains the response of type <typeparamref name="TResponse"/>.
	/// </returns>
	ValueTask<Result<TResponse>> DispatchAsync<TResponse>(
		IRequest<TResponse> request,
		CancellationToken cancellationToken = default);

}