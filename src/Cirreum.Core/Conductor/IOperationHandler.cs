namespace Cirreum.Conductor;

/// <summary>
/// Handles an operation that produces a <see cref="Result"/> with no value.
/// </summary>
/// <typeparam name="TOperation">The operation type.</typeparam>
public interface IOperationHandler<in TOperation>
	where TOperation : IOperation {

	/// <summary>
	/// Executes the operation.
	/// </summary>
	/// <param name="operation">The operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> containing a <see cref="Result"/> indicating
	/// success or failure of the operation.
	/// </returns>
	Task<Result> HandleAsync(
		TOperation operation,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles an operation that produces a <see cref="Result{TResultValue}"/>.
/// </summary>
/// <typeparam name="TOperation">The operation type.</typeparam>
/// <typeparam name="TResultValue">The value produced on success.</typeparam>
public interface IOperationHandler<in TOperation, TResultValue>
	where TOperation : IOperation<TResultValue> {

	/// <summary>
	/// Executes the operation.
	/// </summary>
	/// <param name="operation">The operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The outcome of the operation.</returns>
	Task<Result<TResultValue>> HandleAsync(
		TOperation operation,
		CancellationToken cancellationToken = default);
}