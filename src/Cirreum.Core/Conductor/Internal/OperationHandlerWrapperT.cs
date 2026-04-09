namespace Cirreum.Conductor.Internal;
/// <summary>
/// Base wrapper class for operations with typed responses.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the operation.</typeparam>
internal abstract class OperationHandlerWrapper<TResponse> {

	/// <summary>
	/// Handles the operation by resolving the handler and building the intercept pipeline.
	/// </summary>
	public abstract Task<Result<TResponse>> HandleAsync(
		IOperation<TResponse> request,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken);
}
