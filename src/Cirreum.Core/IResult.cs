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
	/// This method is designed for side effects such as logging, UI updates, or state changes.
	/// </summary>
	/// <param name="onSuccess">Action to execute if successful.</param>
	/// <param name="onFailure">Action to execute with the error if failed.</param>
	/// <example>
	/// <code>
	/// result.Switch(
	///     onSuccess: value => _logger.LogInformation("Success: {Value}", value),
	///     onFailure: error => _logger.LogError(error, "Failed: {Message}", error.Message)
	/// );
	/// </code>
	/// </example>
	void Switch(Action onSuccess, Action<Exception> onFailure);
}

public interface IResult<out T> : IResult {

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	new T? GetValue();

	/// <summary>
	/// Executes the appropriate action based on success or failure state.
	/// This method is designed for side effects such as logging, UI updates, or state changes.
	/// </summary>
	/// <param name="onSuccess">Action to execute with the value if successful.</param>
	/// <param name="onFailure">Action to execute with the error if failed.</param>
	/// <example>
	/// <code>
	/// result.Switch(
	///     onSuccess: value => _logger.LogInformation("Success: {Value}", value),
	///     onFailure: error => _logger.LogError(error, "Failed: {Message}", error.Message)
	/// );
	/// </code>
	/// </example>
	void Switch(Action<T> onSuccess, Action<Exception> onFailure);

}