namespace Cirreum;

/// <summary>
/// Represents the result of an operation, providing information about its success or failure.
/// </summary>
/// <remarks>
/// This interface provides a common abstraction for both <see cref="Result"/> and <see cref="Result{T}"/>,
/// enabling runtime-agnostic handling of operation outcomes across different hosting environments
/// (Server, WASM, Functions).
/// </remarks>
public interface IResult {

	/// <summary>
	/// Gets a value indicating whether the operation completed successfully.
	/// </summary>
	bool IsSuccess { get; }

	/// <summary>
	/// Gets a value indicating whether the operation failed.
	/// </summary>
	bool IsFailure { get; }

	/// <summary>
	/// Gets the error that caused the failure, if any.
	/// Returns null if the operation was successful.
	/// </summary>
	Exception? Error { get; }

	/// <summary>
	/// Gets the underlying value if this is a Result{T}, otherwise returns null.
	/// For non-generic Result, this always returns null.
	/// </summary>
	/// <returns>The boxed value for Result{T}, or null for non-generic Result.</returns>
	object? GetValue();

	/// <summary>
	/// Executes the appropriate action based on success or failure state.
	/// </summary>
	/// <param name="onSuccess">Action to execute if successful.</param>
	/// <param name="onFailure">Action to execute with the error if failed.</param>
	/// <param name="onCallbackError">
	/// Optional action invoked if <paramref name="onSuccess"/> or
	/// <paramref name="onFailure"/> throws.  
	/// If this parameter is <c>null</c>, any exception thrown by the selected
	/// action is rethrown to the caller.  
	/// If it is non-null, the exception is passed to this handler and is
	/// not rethrown.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when either parameter is null.</exception>
	void Switch(
		Action onSuccess,
		Action<Exception> onFailure,
		Action<Exception>? onCallbackError = null);

	/// <summary>
	/// Asynchronously executes the appropriate function based on the success or failure state
	/// of the result.
	/// </summary>
	/// <param name="onSuccess">
	/// A function to invoke when the result is successful.  
	/// </param>
	/// <param name="onFailure">
	/// A function to invoke when the result represents a failure.  
	/// The function receives the associated <see cref="Exception"/>.
	/// </param>
	/// <param name="onCallbackError">
	/// Optional func invoked if <paramref name="onSuccess"/> or
	/// <paramref name="onFailure"/> throws.  
	/// If this parameter is <c>null</c>, any exception thrown by the selected
	/// funcs is rethrown to the caller.  
	/// If it is non-null, the exception is passed to this handler and is
	/// not rethrown.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes when the invoked function has completed.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method provides a way to attach asynchronous side-effect processors.  
	/// If the result is successful, <paramref name="onSuccess"/> is invoked with the value.  
	/// If the result is a failure, <paramref name="onFailure"/> is invoked with the error.  
	/// Any exception thrown by either function is allowed to propagate to the caller.
	/// </remarks>
	ValueTask SwitchAsync(
		Func<ValueTask> onSuccess,
		Func<Exception, ValueTask> onFailure,
		Func<Exception, ValueTask>? onCallbackError = null);

	/// <summary>
	/// Asynchronously executes the appropriate function based on the success or failure state
	/// of the result.
	/// </summary>
	/// <param name="onSuccess">
	/// A function to invoke when the result is successful.  
	/// </param>
	/// <param name="onFailure">
	/// A function to invoke when the result represents a failure.  
	/// The function receives the associated <see cref="Exception"/>.
	/// </param>
	/// <param name="onCallbackError">
	/// Optional func invoked if <paramref name="onSuccess"/> or
	/// <paramref name="onFailure"/> throws.  
	/// If this parameter is <c>null</c>, any exception thrown by the selected
	/// funcs is rethrown to the caller.  
	/// If it is non-null, the exception is passed to this handler and is
	/// not rethrown.
	/// </param>
	/// <returns>
	/// A <see cref="Task"/> that completes when the invoked function has completed.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method provides a way to attach asynchronous side-effect processors.  
	/// If the result is successful, <paramref name="onSuccess"/> is invoked with the value.  
	/// If the result is a failure, <paramref name="onFailure"/> is invoked with the error.  
	/// Any exception thrown by either function is allowed to propagate to the caller.
	/// </remarks>
	Task SwitchAsyncTask(
		Func<Task> onSuccess,
		Func<Exception, Task> onFailure,
		Func<Exception, Task>? onCallbackError = null);

}

public interface IResult<out T> : IResult {

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	new T? GetValue();

	/// <summary>
	/// Executes one of the provided actions depending on whether the result
	/// represents success or failure, without modifying the result.
	/// </summary>
	/// <param name="onSuccess">
	/// The action to invoke when the result is successful.
	/// Receives the value of type <typeparamref name="T"/>.
	/// </param>
	/// <param name="onFailure">
	/// The action to invoke when the result represents a failure.
	/// Receives the associated <see cref="IResult.Error"/>.
	/// </param>
	/// <param name="onCallbackError">
	/// Optional action invoked if <paramref name="onSuccess"/> or
	/// <paramref name="onFailure"/> throws.  
	/// If this parameter is <c>null</c>, any exception thrown by the selected
	/// action is rethrown to the caller.  
	/// If it is non-null, the exception is passed to this handler and is
	/// not rethrown.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/>
	/// is <c>null</c>.
	/// </exception>
	void Switch(
		Action<T> onSuccess,
		Action<Exception> onFailure,
		Action<Exception>? onCallbackError = null);

	/// <summary>
	/// Asynchronously executes the appropriate function based on the success or failure state
	/// of the result.
	/// </summary>
	/// <param name="onSuccess">
	/// A function to invoke when the result is successful.  
	/// The function receives the result value of type <typeparamref name="T"/>.
	/// </param>
	/// <param name="onFailure">
	/// A function to invoke when the result represents a failure.  
	/// The function receives the associated <see cref="Exception"/>.
	/// </param>
	/// <param name="onCallbackError">
	/// Optional func invoked if <paramref name="onSuccess"/> or
	/// <paramref name="onFailure"/> throws.  
	/// If this parameter is <c>null</c>, any exception thrown by the selected
	/// funcs is rethrown to the caller.  
	/// If it is non-null, the exception is passed to this handler and is
	/// not rethrown.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes when the invoked function has completed.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method provides a way to attach asynchronous side-effect processors.  
	/// If the result is successful, <paramref name="onSuccess"/> is invoked with the value.  
	/// If the result is a failure, <paramref name="onFailure"/> is invoked with the error.  
	/// Any exception thrown by either function is allowed to propagate to the caller.
	/// </remarks>
	ValueTask SwitchAsync(
		Func<T, ValueTask> onSuccess,
		Func<Exception, ValueTask> onFailure,
		Func<Exception, ValueTask>? onCallbackError = null);

	/// <summary>
	/// Asynchronously executes the appropriate function based on the success or failure state
	/// of the result.
	/// </summary>
	/// <param name="onSuccess">
	/// A function to invoke when the result is successful.  
	/// The function receives the result value of type <typeparamref name="T"/>.
	/// </param>
	/// <param name="onFailure">
	/// A function to invoke when the result represents a failure.  
	/// The function receives the associated <see cref="Exception"/>.
	/// </param>
	/// <param name="onCallbackError">
	/// Optional func invoked if <paramref name="onSuccess"/> or
	/// <paramref name="onFailure"/> throws.  
	/// If this parameter is <c>null</c>, any exception thrown by the selected
	/// funcs is rethrown to the caller.  
	/// If it is non-null, the exception is passed to this handler and is
	/// not rethrown.
	/// </param>
	/// <returns>
	/// A <see cref="Task"/> that completes when the invoked function has completed.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method provides a way to attach asynchronous side-effect processors.  
	/// If the result is successful, <paramref name="onSuccess"/> is invoked with the value.  
	/// If the result is a failure, <paramref name="onFailure"/> is invoked with the error.  
	/// Any exception thrown by either function is allowed to propagate to the caller.
	/// </remarks>
	Task SwitchAsyncTask(
		Func<T, Task> onSuccess,
		Func<Exception, Task> onFailure,
		Func<Exception, Task>? onCallbackError = null);

}