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
	/// Returns a completed task representing a successful result.
	/// </summary>
	public static Task<Result> SuccessTask { get; } = Task.FromResult(Success);

	/// <summary>
	/// Creates a failed result with the specified error.
	/// </summary>
	public static Result Fail(Exception error) {
		ArgumentNullException.ThrowIfNull(error);
		return new(false, error);
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
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess => this._isSuccess;

	/// <summary>
	/// Gets a value indicating whether the result represents a failure state.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
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
	/// Executes an action if the result is successful.
	/// </summary>
	/// <param name="action">The action to execute with the value.</param>
	/// <returns>The current <see cref="Result"/> for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public Result OnSuccess(Action action) {
		ArgumentNullException.ThrowIfNull(action);
		if (this.IsSuccess) {
			action();
		}
		return this;
	}

	/// <summary>
	/// Executes an action if the result is successful.
	/// </summary>
	/// <param name="action">The action to execute with the value.</param>
	/// <param name="errorSelector">An optional function to transform any exception thrown by the action into a different exception.</param>
	/// <returns>The current <see cref="Result"/> for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public Result OnSuccessTry(
		Action action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		if (this.IsSuccess) {
			try {
				action();
				return this;
			} catch (Exception ex) {
				var error = errorSelector?.Invoke(ex) ?? ex;
				return Fail(error);
			}
		}

		return this;

	}

	/// <summary>
	/// Executes an action if the result is failed.
	/// </summary>
	/// <param name="action">The action to execute with the exception.</param>
	/// <returns>The current <see cref="Result{T}"/> for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public Result OnFailure(Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		if (this.IsFailure) {
			action(this.Error);
		}
		return this;
	}

	/// <summary>
	/// Executes an action if the result is failed.
	/// </summary>
	/// <param name="action">The action to execute with the exception.</param>
	/// <param name="errorSelector">An optional function to transform any exception thrown by the action into a different exception.</param>
	/// <returns>The current <see cref="Result{T}"/> for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public Result OnFailureTry(
		Action<Exception> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);
		if (this.IsSuccess || this.Error is null) {
			return this;
		}

		try {
			action(this.Error);
			return this;
		} catch (Exception ex) {
			var error = errorSelector?.Invoke(ex) ?? ex;
			return Fail(error);
		}
	}

	/// <summary>
	/// Executes an action to inspect the current result without modifying it.
	/// The action is invoked regardless of whether the result represents success or failure.
	/// </summary>
	/// <param name="action">The action to execute with the current result.</param>
	/// <returns>The current <see cref="Result"/> unchanged.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	/// <remarks>
	/// Use this method for side effects like logging or telemetry that should occur
	/// regardless of the result state. Any exception thrown by the action is allowed to propagate.
	/// </remarks>
	public Result Inspect(Action<Result> action) {
		ArgumentNullException.ThrowIfNull(action);
		action(this);
		return this;
	}

	/// <summary>
	/// Executes an action to inspect the current result, catching any exceptions thrown by the action.
	/// If the action throws and this is a success result, converts to a failure result.
	/// </summary>
	public Result InspectTry(
		Action<Result> action,
		Func<Exception, Exception>? errorSelector = null) {

		ArgumentNullException.ThrowIfNull(action);

		try {
			action(this);
			return this;
		} catch (Exception ex) {
			// If already failed, keep original failure
			if (this.IsFailure) {
				return this;
			}

			// Success → Failure if inspection throws
			var error = errorSelector?.Invoke(ex) ?? ex;
			return Fail(error);
		}
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
	/// Projects the current result into a value of type <typeparamref name="TOut"/>
	/// by invoking the appropriate function for the success or failure case.
	/// </summary>
	public TOut Match<TOut>(
		Func<TOut> onSuccess,
		Func<Exception, TOut> onFailure) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		return this.IsSuccess
			? onSuccess()
			: onFailure(this.Error);
	}


	/// <summary>
	/// Implicitly converts a non-generic <see cref="Result"/> into a
	/// <see cref="Result{T}"/> with <see cref="Unit"/> as the value type.
	/// </summary>
	/// <remarks>
	/// This conversion preserves the success or failure state of the original
	/// <see cref="Result"/>. On success, the resulting <see cref="Result{T}"/>
	/// contains <see cref="Unit.Value"/>. On failure, the error is propagated
	/// to the resulting <see cref="Result{T}"/>.
	/// </remarks>
	/// <param name="result">
	/// The <see cref="Result"/> instance to convert.
	/// </param>
	public static implicit operator Result<Unit>(Result result) =>
		result._isSuccess
			? Result<Unit>.Success(Unit.Value)
			: Result<Unit>.Fail(result._error!);


	/// <summary>
	/// Implicitly converts a <see cref="Result{T}"/> with <see cref="Unit"/>
	/// as the value type into a non-generic <see cref="Result"/>.
	/// </summary>
	/// <remarks>
	/// This conversion discards the <see cref="Unit"/> value and preserves
	/// only the success or failure state. If the source result represents a
	/// failure, the associated error is propagated to the resulting
	/// <see cref="Result"/>.
	/// </remarks>
	/// <param name="result">
	/// The <see cref="Result{Unit}"/> instance to convert.
	/// </param>
	public static implicit operator Result(Result<Unit> result) =>
		result.IsSuccess
			? Success
			: Fail(result.Error!);


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


	#region IResultT Implementation

	// =============== IMPLEMENTATION ===============
	// Normal implementations for IResult

	// we implment explicitly to avoid polluting the public API
	object? IResult.GetValue() => null;

	public void Switch(
		Action onSuccess,
		Action<Exception> onFailure,
		Action<Exception>? onCallbackError = null) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		try {

			if (this.IsSuccess) {
				onSuccess();
				return;
			}

			onFailure(this.Error);

		} catch (Exception ex) {
			if (onCallbackError is not null) {
				onCallbackError(ex);
				return;
			}
			throw;
		}

	}

	public async ValueTask SwitchAsync(
		Func<ValueTask> onSuccess,
		Func<Exception, ValueTask> onFailure,
		Func<Exception, ValueTask>? onCallbackError = null) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		try {

			if (this.IsSuccess) {
				await onSuccess().ConfigureAwait(false);
				return;
			}

			await onFailure(this.Error).ConfigureAwait(false);

		} catch (Exception ex) {
			if (onCallbackError is not null) {
				await onCallbackError(ex).ConfigureAwait(false);
				return;
			}
			throw;
		}

	}

	public async Task SwitchAsyncTask(
		Func<Task> onSuccess,
		Func<Exception, Task> onFailure,
		Func<Exception, Task>? onCallbackError = null) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		try {

			if (this.IsSuccess) {
				await onSuccess().ConfigureAwait(false);
				return;
			}

			await onFailure(this.Error).ConfigureAwait(false);

		} catch (Exception ex) {
			if (onCallbackError is not null) {
				await onCallbackError(ex).ConfigureAwait(false);
				return;
			}
			throw;
		}

	}

	#endregion

}