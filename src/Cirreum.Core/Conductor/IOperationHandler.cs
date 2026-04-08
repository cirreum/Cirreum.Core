namespace Cirreum.Conductor;

/// <summary>
/// Defines a handler for a request that does not return a value.
/// </summary>
/// <typeparam name="TOperation">
/// The type of request being handled. Must implement <see cref="IOperation"/>.
/// </typeparam>
public interface IOperationHandler<in TOperation>
	where TOperation : IOperation {

	/// <summary>
	/// Handles the request asynchronously.
	/// </summary>
	/// <param name="request">The request instance to handle.</param>
	/// <param name="cancellationToken">
	/// A token used to propagate notifications that the operation should be canceled.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> containing a <see cref="Result"/> indicating
	/// success or failure of the operation.
	/// </returns>
	Task<Result> HandleAsync(
		TOperation request,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for a request that returns a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TOperation">
/// The type of request being handled. Must implement <see cref="IOperation{TResponse}"/>.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of response returned when the request is handled.
/// </typeparam>
public interface IOperationHandler<in TOperation, TResponse>
	where TOperation : IOperation<TResponse> {

	/// <summary>
	/// Handles the request asynchronously and produces a response.
	/// </summary>
	/// <param name="request">The request instance to handle.</param>
	/// <param name="cancellationToken">
	/// A token used to propagate notifications that the operation should be canceled.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> containing a <see cref="Result{TResponse}"/>
	/// that represents the outcome of the operation and the response value.
	/// </returns>
	Task<Result<TResponse>> HandleAsync(
		TOperation request,
		CancellationToken cancellationToken = default);
}
