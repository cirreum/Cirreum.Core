namespace Cirreum.Conductor;

/// <summary>
/// Defines a handler for an operation that does not return a value.
/// </summary>
/// <typeparam name="TOperation">
/// The type of operation being handled. Must implement <see cref="IOperation"/>.
/// </typeparam>
public interface IOperationHandler<in TOperation>
	where TOperation : IOperation {

	/// <summary>
	/// Handles the operation asynchronously.
	/// </summary>
	/// <param name="operation">The operation instance to handle.</param>
	/// <param name="cancellationToken">
	/// A token used to propagate notifications that the operation should be canceled.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> containing a <see cref="Result"/> indicating
	/// success or failure of the operation.
	/// </returns>
	Task<Result> HandleAsync(
		TOperation operation,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for an operation that returns a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TOperation">
/// The type of operation being handled. Must implement <see cref="IOperation{TResponse}"/>.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of response returned when the operation is handled.
/// </typeparam>
public interface IOperationHandler<in TOperation, TResponse>
	where TOperation : IOperation<TResponse> {

	/// <summary>
	/// Handles the operation asynchronously and produces a response.
	/// </summary>
	/// <param name="operation">The operation instance to handle.</param>
	/// <param name="cancellationToken">
	/// A token used to propagate notifications that the operation should be canceled.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> containing a <see cref="Result{TResponse}"/>
	/// that represents the outcome of the operation and the response value.
	/// </returns>
	Task<Result<TResponse>> HandleAsync(
		TOperation operation,
		CancellationToken cancellationToken = default);
}
