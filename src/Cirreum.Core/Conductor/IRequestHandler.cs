namespace Cirreum.Conductor;

/// <summary>
/// Defines a handler for a request that does not return a value.
/// </summary>
/// <typeparam name="TRequest">
/// The type of request being handled. Must implement <see cref="IRequest"/>.
/// </typeparam>
public interface IRequestHandler<in TRequest>
	where TRequest : IRequest {

	/// <summary>
	/// Handles the request asynchronously.
	/// </summary>
	/// <param name="request">The request instance to handle.</param>
	/// <param name="cancellationToken">
	/// A token used to propagate notifications that the operation should be canceled.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> containing a <see cref="Result"/> indicating
	/// success or failure of the operation.
	/// </returns>
	ValueTask<Result> HandleAsync(
		TRequest request,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for a request that returns a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">
/// The type of request being handled. Must implement <see cref="IRequest{TResponse}"/>.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of response returned when the request is handled.
/// </typeparam>
public interface IRequestHandler<in TRequest, TResponse>
	where TRequest : IRequest<TResponse> {

	/// <summary>
	/// Handles the request asynchronously and produces a response.
	/// </summary>
	/// <param name="request">The request instance to handle.</param>
	/// <param name="cancellationToken">
	/// A token used to propagate notifications that the operation should be canceled.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> containing a <see cref="Result{TResponse}"/>
	/// that represents the outcome of the operation and the response value.
	/// </returns>
	ValueTask<Result<TResponse>> HandleAsync(
		TRequest request,
		CancellationToken cancellationToken = default);
}
