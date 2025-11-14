namespace Cirreum;

using Cirreum.Conductor;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the result of an operation without a typed value.
/// </summary>
public readonly struct Result : IResult, IEquatable<Result> {

	/// <summary>
	/// Represents a successful result with no associated error information.
	/// </summary>
	public static Result Success { get; } = new(true, null);

	/// <summary>
	/// Creates a failed result with the specified error.
	/// </summary>
	public static Result Fail(Exception error) {
		ArgumentNullException.ThrowIfNull(error);
		return new(false, error);
	}

	/// <summary>
	/// Creates a failed result with an error message.
	/// </summary>
	public static Result Fail(string errorMessage) {
		ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
		return new(false, new Exception(errorMessage));
	}

	private readonly bool _isSuccess;
	private readonly Exception? _error;

	/// <summary>
	/// Gets the error if the operation failed.
	/// </summary>
	public Exception? Error => this._error;

	/// <summary>
	/// Initializes a new instance of the Result class with the specified success state and error information.
	/// </summary>
	/// <param name="isSuccess">A value indicating whether the operation was successful. Specify <see langword="true"/> for success; otherwise,
	/// <see langword="false"/>.</param>
	/// <param name="error">The exception associated with a failed operation, or <see langword="null"/> if the operation was successful.</param>
	private Result(bool isSuccess, Exception? error) {
		this._isSuccess = isSuccess;
		this._error = error;
		// Invariants: success -> null error, failure -> non-null error
		Debug.Assert(isSuccess ? error is null : error is not null,
			"Success must carry a null error; failure must carry a non-null error.");
	}

	/// <summary>
	/// Gets a value indicating whether the operation succeeded.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsSuccess => this._isSuccess;

	/// <summary>
	/// Gets a value indicating whether the result represents a failure state.
	/// </summary>
	public bool IsFailure => !_isSuccess;

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
	/// Executes an action if the result is successful
	/// </summary>
	public Result OnSuccess(Action action) {
		ArgumentNullException.ThrowIfNull(action);
		if (!this.IsSuccess) {
			return this;
		}

		try {
			action();
			return this;
		} catch (Exception ex) {
			return Fail(ex);
		}
	}

	/// <summary>
	/// Executes an action if the result is failed
	/// </summary>
	public Result OnFailure(Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		if (!this.IsSuccess && this.Error is not null) {
			action(this.Error);
		}
		return this;
	}

	/// <summary>
	/// Transforms the current result into a new result of the specified type using the provided
	/// value factory function.
	/// </summary>
	/// <typeparam name="T">The type of the value in the resulting <see cref="Result{T}"/>.</typeparam>
	/// <param name="valueFactory">A function that produces the value for the new result if the current result is successful.</param>
	/// <returns>A successful <see cref="Result{T}"/> containing the value produced by <paramref name="valueFactory"/> if the
	/// current result is successful; otherwise, a failed <see cref="Result{T}"/> containing the current error.</returns>
	public Result<T> Map<T>(Func<T> valueFactory) {
		ArgumentNullException.ThrowIfNull(valueFactory);

		if (!this.IsSuccess) {
			return Result<T>.Fail(this.Error!);
		}

		try {
			return Result<T>.Success(valueFactory());
		} catch (Exception ex) {
			return Result<T>.Fail(ex);
		}
	}

	/// <summary>
	/// Chains another void operation
	/// </summary>
	public Result Then(Func<Result> next) {
		ArgumentNullException.ThrowIfNull(next);

		if (!this.IsSuccess) {
			return this;
		}

		try {
			return next();
		} catch (Exception ex) {
			return Fail(ex);
		}
	}



	/// <summary>
	/// Implicitly converts a <see cref="Result"/> to <see cref="Result{T}"/> with <see cref="Unit"/> as the value type.
	/// </summary>
	public static implicit operator Result<Unit>(Result result) =>
		result._isSuccess
			? Result<Unit>.Success(Unit.Value)
			: Result<Unit>.Fail(result._error!);

	public bool Equals(Result other) =>
		this._isSuccess == other._isSuccess &&
		Equals(this._error, other._error);

	public override bool Equals(object? obj) =>
		obj is Result other && this.Equals(other);

	public override int GetHashCode() =>
		HashCode.Combine(this._isSuccess, this._error);

	public override string ToString() {
		return this.IsSuccess
			? "Success"
			: $"Fail({this.Error?.GetType().Name}: {this.Error?.Message})";
	}

	public static bool operator ==(Result left, Result right) => left.Equals(right);
	public static bool operator !=(Result left, Result right) => !left.Equals(right);

	#region IResult Implementation

	/// <summary>
	/// Gets the underlying value - always null for non-generic Result.
	/// </summary>
	/// <returns>Always returns null since non-generic Result carries no value.</returns>
	object? IResult.GetValue() => null;

	/// <summary>
	/// Executes the appropriate action based on success or failure state.
	/// </summary>
	/// <param name="onSuccess">Action to execute if successful (receives null).</param>
	/// <param name="onFailure">Action to execute with the error if failed.</param>
	/// <exception cref="ArgumentNullException">Thrown when either parameter is null.</exception>
	/// <remarks>
	/// This method is an explicit interface implementation to avoid confusion with the strongly-typed
	/// <see cref="OnSuccess"/> and <see cref="OnFailure"/> methods. For non-generic Result, the success
	/// action always receives null since there is no value to pass. Any exceptions thrown by the actions
	/// are allowed to propagate to the caller.
	/// </remarks>
	void IResult.Switch(Action<object?> onSuccess, Action<Exception> onFailure) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		if (this.IsSuccess) {
			onSuccess(null);  // Non-generic Result has no value
		} else {
			// Debug assertion to catch internal consistency issues
			Debug.Assert(this._error is not null, "Failed result must have a non-null error.");
			onFailure(this._error!);
		}
	}

	#endregion


}