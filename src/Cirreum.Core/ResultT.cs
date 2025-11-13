namespace Cirreum;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the outcome of an operation, encapsulating either a successful result with a value or a failure with an
/// associated error.
/// </summary>
/// <remarks>Use <see cref="Result{T}"/> to model operations that may fail, providing a unified way to handle both
/// success and error cases without relying on exceptions for control flow. The result exposes properties and methods to
/// inspect the outcome, retrieve the value or error, and compose further operations in a fluent manner. This type is
/// thread-safe for read-only usage and is commonly used in functional and error-handling scenarios.</remarks>
/// <typeparam name="T">The type of the value returned if the operation is successful.</typeparam>
public readonly struct Result<T> : IResult, IEquatable<Result<T>> {

	/// <summary>
	/// Creates a successful result with the specified value.
	/// </summary>
	public static Result<T> Success(T value) {
		ArgumentNullException.ThrowIfNull(value);
		return new(true, value, null);
	}

	/// <summary>
	/// Creates a failed result with the specified error.
	/// </summary>
	public static Result<T> Fail(Exception error) {
		ArgumentNullException.ThrowIfNull(error);
		return new(false, default, error);
	}

	/// <summary>
	/// Creates a failed result with an error message.
	/// </summary>
	public static Result<T> Fail(string errorMessage) {
		ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
		return new(false, default, new Exception(errorMessage));
	}

	private readonly bool _isSuccess;
	private readonly T? _value;
	private readonly Exception? _error;


	/// <summary>
	/// Initializes a new instance of the Result class with the specified success state, value, and error information.
	/// </summary>
	/// <remarks>This constructor is typically used internally to create a Result instance representing either a
	/// successful outcome with a value or a failure with an associated exception.</remarks>
	/// <param name="isSuccess">A value indicating whether the operation was successful. Set to <see langword="true"/> if successful; otherwise,
	/// <see langword="false"/>.</param>
	/// <param name="value">The value produced by the operation if successful; otherwise, <see langword="null"/>.</param>
	/// <param name="error">The exception representing the error if the operation failed; otherwise, <see langword="null"/>.</param>
	private Result(bool isSuccess, T? value, Exception? error) {
		this._isSuccess = isSuccess;
		this._value = value;
		this._error = error;

		// Internal sanity checks (debug-only):
		Debug.Assert(!isSuccess || value is not null, "Success must carry a non-null value.");
		Debug.Assert(isSuccess || error is not null, "Failure must carry a non-null error.");
	}

	/// <summary>
	/// Gets a value indicating whether the operation succeeded.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess => this._isSuccess;

	/// <summary>
	/// Gets a value indicating whether the result represents a failure state.
	/// </summary>
	public bool IsFailure => !_isSuccess;

	/// <summary>
	/// Gets the value if the operation succeeded.
	/// </summary>
	public T? Value => this._value;

	/// <summary>
	/// Gets the error if the operation failed.
	/// </summary>
	public Exception? Error => this._error;

	/// <summary>
	/// Attempts to retrieve the error associated with this result if it represents a failure.
	/// </summary>
	/// <remarks>Use this method to safely access the error without throwing an exception. This is typically used in
	/// scenarios where you want to handle errors conditionally based on the result state.</remarks>
	/// <param name="error">When this method returns <see langword="true"/>, contains the <see cref="Exception"/> that caused the failure;
	/// otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if this result represents a failure and an error is available; otherwise, <see
	/// langword="false"/>.</returns>
	public bool TryGetError([NotNullWhen(true)] out Exception? error) {
		if (this.IsFailure) {
			error = _error!;
			return true;
		}
		error = null;
		return false;
	}

	/// <summary>
	/// Attempts to retrieve the value if the result is successful.
	/// </summary>
	/// <param name="value">When this method returns, contains the value if the result is successful;
	/// otherwise, the default value for <typeparamref name="T"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the result is successful and the value is retrieved; otherwise, 
	/// <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// This method provides a safe way to access the result value without needing to check 
	/// <see cref="IsSuccess"/> separately. When the method returns <see langword="true"/>, the
	/// <paramref name="value"/> parameter is guaranteed to be non-null for non-nullable types.
	/// </remarks>
	/// <example>
	/// <code>
	/// var result = GetUser(userId);
	/// if (result.TryGetValue(out var user)) {
	///     // Use user safely - compiler knows it's not null
	///     Console.WriteLine($"Found user: {user.Name}");
	///     return;
	/// }
	/// // Handle failure
	/// Console.WriteLine($"Failed: {result.Error.Message}");
	/// </code>
	/// </example>
	public bool TryGetValue([NotNullWhen(true)] out T? value) {
		if (this.IsSuccess) {
			value = this.Value;
			return true;
		}
		value = default;
		return false;
	}

	/// <summary>
	/// Deconstructs the <see cref="Result{T}"/> into its components.
	/// </summary>
	/// <param name="success">When this method returns, contains the success flag.</param>
	/// <param name="value">When this method returns, contains the value if successful; otherwise, the default value for <typeparamref name="T"/>.</param>
	/// <param name="exception">When this method returns, contains the exception if failed; otherwise, <see langword="null"/>.</param>
	public void Deconstruct(out bool success, out T? value, out Exception? exception) {
		success = this._isSuccess;
		value = this.Value;
		exception = this.Error;
	}

	/// <summary>
	/// Executes an action if the result is successful.
	/// </summary>
	/// <param name="action">The action to execute with the value.</param>
	/// <returns>The current <see cref="Result{T}"/> for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public Result<T> OnSuccess(Action<T> action) {
		ArgumentNullException.ThrowIfNull(action);
		if (!this.IsSuccess) {
			return this;
		}

		try {
			action(this.Value!);
			return this;
		} catch (Exception ex) {
			return Result<T>.Fail(ex);
		}
	}

	/// <summary>
	/// Executes an action if the result is failed.
	/// </summary>
	/// <param name="action">The action to execute with the exception.</param>
	/// <returns>The current <see cref="Result{T}"/> for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public Result<T> OnFailure(Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		if (!this.IsSuccess && this.Error is not null) {
			action(this.Error);
		}
		return this;
	}

	/// <summary>
	/// Transforms the value if the result is successful.
	/// </summary>
	/// <typeparam name="TResult">The type of the transformed value.</typeparam>
	/// <param name="selector">The transformation function.</param>
	/// <returns>A new <see cref="Result{TResult}"/> with the transformed value if the operation was successful; otherwise, a failed result with the original exception.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
	public Result<TResult> Map<TResult>(Func<T, TResult> selector) {
		ArgumentNullException.ThrowIfNull(selector);

		if (!this.IsSuccess) {
			return Result<TResult>.Fail(this.Error!);
		}

		try {
			return Result<TResult>.Success(selector(this.Value!));
		} catch (Exception ex) {
			return Result<TResult>.Fail(ex);
		}
	}

	/// <summary>
	/// Filters the current result based on a specified condition.
	/// </summary>
	/// <param name="predicate">A function that defines the condition to evaluate the current value.
	/// The function should return <see langword="true"/> to retain the value, or <see langword="false"/>
	/// to fail the result.</param>
	/// <param name="errorMessage">The error message to associate with the result if the condition is not met.</param>
	/// <returns>A <see cref="Result{T}"/> that contains the current value if the condition is met; otherwise, 
	/// a failed result with the specified error message.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="errorMessage"/> is null or whitespace.</exception>
	public Result<T> Where(Func<T, bool> predicate, string errorMessage) {
		ArgumentNullException.ThrowIfNull(predicate);
		if (string.IsNullOrWhiteSpace(errorMessage)) {
			throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
		}

		if (!this.IsSuccess) {
			return this;  // Already failed, return same instance
		}

		try {
			return predicate(this.Value!) ? this : Fail(errorMessage);
		} catch (Exception ex) {
			return Fail(ex);  // Convert exception to failure
		}
	}

	/// <summary>
	/// Chains another operation that returns a Result if the current result is successful.
	/// </summary>
	/// <typeparam name="TResult">The type of the value in the chained result.</typeparam>
	/// <param name="selector">The function that returns the next result operation.</param>
	/// <returns>The result of the chained operation if successful, or the original failure.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
	public Result<TResult> Then<TResult>(Func<T, Result<TResult>> selector) {
		ArgumentNullException.ThrowIfNull(selector);

		if (!this.IsSuccess) {
			return Result<TResult>.Fail(this.Error!);
		}

		try {
			return selector(this.Value!);
		} catch (Exception ex) {
			return Result<TResult>.Fail(ex);
		}

	}


	public bool Equals(Result<T> other) =>
		this._isSuccess == other._isSuccess &&
		EqualityComparer<T>.Default.Equals(this._value, other._value) &&
		Equals(this._error, other._error);

	public override bool Equals(object? obj) =>
		obj is Result<T> other && this.Equals(other);

	public override int GetHashCode() =>
		HashCode.Combine(this._isSuccess, this._value, this._error);


	/// <summary>
	/// Returns a string representation of the <see cref="Result{T}"/>.
	/// </summary>
	/// <returns>A string that represents the current <see cref="Result{T}"/>.</returns>
	public override string ToString() {
		return this.IsSuccess
			? $"Success({this.Value})"
			: $"Fail({this.Error?.GetType().Name}: {this.Error?.Message})";
	}

	public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
	public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);


	/// <summary>
	/// Implicitly converts a value to a successful <see cref="Result{T}"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A successful <see cref="Result{T}"/> containing the specified value.</returns>
	public static implicit operator Result<T>(T value) => Success(value);

	/// <summary>
	/// Implicitly converts an exception to a failed <see cref="Result{T}"/>.
	/// </summary>
	/// <param name="exception">The exception to convert.</param>
	/// <returns>A failed <see cref="Result{T}"/> containing the specified exception.</returns>
	public static implicit operator Result<T>(Exception exception) => Fail(exception);

}