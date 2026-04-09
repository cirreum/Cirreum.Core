namespace Cirreum.Conductor;

/// <summary>
/// Represents a delegate that handles an operation with context and returns a result asynchronously.
/// </summary>
/// <typeparam name="TOperation">The type of operation being processed.</typeparam>
/// <typeparam name="TResultValue">The type of the response returned by the handler.</typeparam>
/// <param name="context">The context of the operation, including user state and request metadata.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to observe cancellation requests.</param>
/// <returns><see cref="Result{T}"/> of type <typeparamref name="TResultValue"/>.</returns>
public delegate Task<Result<TResultValue>> OperationHandlerDelegate<TOperation, TResultValue>(
	OperationContext<TOperation> context,
	CancellationToken cancellationToken)
	where TOperation : notnull;