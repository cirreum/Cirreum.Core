namespace Cirreum;

/// <summary>
/// Provides extension methods for async operations with <see cref="Result{T}"/>.
/// </summary>
public static class ResultAsyncExtensions {


	// =============== ON SUCCESS ASYNC ===============

	/// <summary>
	/// Executes the specified action if the asynchronous operation represented by the <see cref="ValueTask{Result}"/>
	/// completes successfully.
	/// </summary>
	/// <remarks>The <paramref name="action"/> is executed only if the result of the asynchronous operation indicates
	/// success. If the operation fails, the action is not executed, and the original failure result is returned.</remarks>
	/// <param name="resultTask">The asynchronous operation to evaluate.</param>
	/// <param name="action">The action to execute if the operation is successful. This action is not executed if the operation fails.</param>
	/// <returns>A <see cref="ValueTask{Result}"/> representing the result of the operation, with the action executed if the
	/// operation was successful.</returns>
	public static async ValueTask<Result> OnSuccessAsync(
		this ValueTask<Result> resultTask,
		Action action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccess(action);
	}

	/// <summary>
	/// Executes the specified action if the asynchronous operation completes successfully.
	/// </summary>
	public static async Task<Result> OnSuccessAsync(
		this Task<Result> resultTask,
		Action action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccess(action);
	}

	/// <summary>
	/// Also works with Task&lt;Result&lt;T&gt;&gt; for compatibility.
	/// </summary>
	public static async Task<Result<T>> OnSuccessAsync<T>(
		this Task<Result<T>> resultTask,
		Action<T> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccess(action);
	}

	/// <summary>
	/// Executes an action if the result is successful.
	/// </summary>
	/// <typeparam name="T">The type of the value in the result.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="action">The action to execute with the value.</param>
	/// <returns>A task that represents the asynchronous operation, containing the original result.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public static async ValueTask<Result<T>> OnSuccessAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Action<T> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccess(action);
	}


	// =============== ON FAILURE ASYNC ===============

	/// <summary>
	/// Executes an action if the result is failed.
	/// </summary>
	/// <typeparam name="T">The type of the value in the result.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="action">The action to execute with the exception.</param>
	/// <returns>A task that represents the asynchronous operation, containing the original result.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public static async ValueTask<Result<T>> OnFailureAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnFailure(action);
	}

	/// <summary>
	/// Also works with Task&lt;Result&lt;T&gt;&gt; for compatibility.
	/// </summary>
	public static async Task<Result<T>> OnFailureAsync<T>(
		this Task<Result<T>> resultTask,
		Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnFailure(action);
	}

	/// <summary>
	/// Invokes the specified action if the asynchronous operation represented by the result task fails, and returns the
	/// original result.
	/// </summary>
	/// <remarks>This method enables chaining custom failure-handling logic for asynchronous operations. The action
	/// is only called if the Result is a failure, and is not called for successful results.</remarks>
	/// <param name="resultTask">A ValueTask representing an asynchronous operation that yields a Result.</param>
	/// <param name="action">The action to execute if the Result indicates failure. The exception associated with the failure is passed to this
	/// action. Cannot be null.</param>
	/// <returns>A Result containing the outcome of the original asynchronous operation. If the operation failed, the action is
	/// invoked before returning the Result.</returns>
	public static async ValueTask<Result> OnFailureAsync(
		this ValueTask<Result> resultTask,
		Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnFailure(action);
	}

	/// <summary>
	/// Invokes the specified action if the asynchronous operation represented by the result task fails, and returns the
	/// original result.
	/// </summary>
	/// <remarks>This method is typically used for logging or handling errors in asynchronous workflows without
	/// altering the result. The action is only invoked if the result indicates a failure.</remarks>
	/// <param name="resultTask">A task that represents the asynchronous operation whose result will be inspected for failure.</param>
	/// <param name="action">The action to execute if the result contains a failure. The exception associated with the failure is passed to this
	/// action. Cannot be null.</param>
	/// <returns>A task that represents the original result after the failure action has been executed, if applicable.</returns>
	public static async Task<Result> OnFailureAsync(
		this Task<Result> resultTask,
		Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnFailure(action);
	}


	// =============== MAP ASYNC ===============

	/// <summary>
	/// Asynchronously transforms the result of a <see cref="ValueTask{TResult}"/> where <c>TResult</c>
	/// is a <see cref="Result"/>, into a new result using the specified value factory function.
	/// </summary>
	/// <remarks>
	/// This method awaits the completion of <paramref name="resultTask"/> and applies
	/// <paramref name="valueFactory"/> only if the original result is successful.
	/// If the result represents a failure, the failure is propagated without invoking the factory.
	/// </remarks>
	/// <typeparam name="T">The type of the value produced by <paramref name="valueFactory"/>.</typeparam>
	/// <param name="resultTask">The asynchronous result to transform.</param>
	/// <param name="valueFactory">
	/// A function that produces a value of type <typeparamref name="T"/> when the original result is successful.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> where <c>TResult</c> is a <see cref="Result{T}"/> containing
	/// the transformed value.
	/// </returns>
	public static async ValueTask<Result<T>> MapAsync<T>(
		this ValueTask<Result> resultTask,
		Func<T> valueFactory) {
		ArgumentNullException.ThrowIfNull(valueFactory);
		var result = await resultTask.ConfigureAwait(false);
		return result.Map(valueFactory);
	}

	/// <summary>
	/// Transforms the value if the result is successful.
	/// </summary>
	/// <typeparam name="T">The type of the value in the result.</typeparam>
	/// <typeparam name="TResult">The type of the transformed value.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="selector">The transformation function.</param>
	/// <returns>A task that represents the asynchronous operation, containing a new result with the transformed value if successful, or the original failure.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
	public static async ValueTask<Result<TResult>> MapAsync<T, TResult>(
		this ValueTask<Result<T>> resultTask,
		Func<T, TResult> selector) {
		ArgumentNullException.ThrowIfNull(selector);
		var result = await resultTask.ConfigureAwait(false);
		return result.Map(selector);
	}

	/// <summary>
	/// Transforms the value asynchronously if the result is successful.
	/// </summary>
	/// <typeparam name="T">The type of the value in the result.</typeparam>
	/// <typeparam name="TResult">The type of the transformed value.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="selector">The asynchronous transformation function.</param>
	/// <returns>A task that represents the asynchronous operation, containing a new result with the transformed value if successful, or the original failure.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
	public static async ValueTask<Result<TResult>> MapAsync<T, TResult>(
		this ValueTask<Result<T>> resultTask,
		Func<T, ValueTask<TResult>> selector) {
		ArgumentNullException.ThrowIfNull(selector);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return Result<TResult>.Fail(result.Error!);
		}

		try {
			var transformedValue = await selector(result.Value!).ConfigureAwait(false);
			return Result<TResult>.Success(transformedValue);
		} catch (Exception ex) {
			return Result<TResult>.Fail(ex);
		}
	}

	/// <summary>
	/// Asynchronously maps a completed non-generic result to a generic result by invoking the specified value factory if
	/// the original result is successful.
	/// </summary>
	/// <typeparam name="T">The type of the value to be produced if the result is successful.</typeparam>
	/// <param name="resultTask">A task that represents the asynchronous operation returning a non-generic result.</param>
	/// <param name="valueFactory">A delegate that produces a value of type <typeparamref name="T"/> if the original result is successful. Cannot be
	/// null.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{T}"/> with the
	/// mapped value if successful; otherwise, it contains the failure from the original result.</returns>
	public static async Task<Result<T>> MapAsync<T>(
		this Task<Result> resultTask,
		Func<T> valueFactory) {
		ArgumentNullException.ThrowIfNull(valueFactory);
		var result = await resultTask.ConfigureAwait(false);
		return result.Map(valueFactory);
	}

	/// <summary>
	/// Asynchronously applies the specified mapping function to the value contained in a completed result, if the result
	/// is successful.
	/// </summary>
	/// <remarks>If the input result is a failure, the mapping function is not invoked and the error is propagated.
	/// This method is typically used to chain asynchronous result transformations.</remarks>
	/// <typeparam name="T">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TResult">The type of the value returned by the mapping function.</typeparam>
	/// <param name="resultTask">A task that represents the asynchronous operation returning a result to be mapped.</param>
	/// <param name="selector">A function to transform the value of a successful result. Cannot be null.</param>
	/// <returns>A task that represents the asynchronous operation. The result contains the mapped value if the original result was
	/// successful; otherwise, it contains the original error.</returns>
	public static async Task<Result<TResult>> MapAsync<T, TResult>(
		this Task<Result<T>> resultTask,
		Func<T, TResult> selector) {
		ArgumentNullException.ThrowIfNull(selector);
		var result = await resultTask.ConfigureAwait(false);
		return result.Map(selector);
	}

	/// <summary>
	/// Asynchronously transforms the successful result value of a task using the specified selector function, returning a
	/// new result that reflects the transformation or any error encountered.
	/// </summary>
	/// <remarks>If the input result is not successful, the selector function is not invoked and the error is
	/// propagated. Any exception thrown by the selector function is captured and returned as a failed result.</remarks>
	/// <typeparam name="T">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TResult">The type of the value produced by the selector function.</typeparam>
	/// <param name="resultTask">A task that produces a result to be transformed if successful.</param>
	/// <param name="selector">A function that asynchronously transforms the value of a successful result. Cannot be null.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a successful result with the
	/// transformed value if the input result is successful and the selector completes successfully; otherwise, a failed
	/// result containing the error from the input or any exception thrown by the selector.</returns>
	public static async Task<Result<TResult>> MapAsync<T, TResult>(
		this Task<Result<T>> resultTask,
		Func<T, Task<TResult>> selector) {
		ArgumentNullException.ThrowIfNull(selector);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return Result<TResult>.Fail(result.Error!);
		}

		try {
			var transformedValue = await selector(result.Value!).ConfigureAwait(false);
			return Result<TResult>.Success(transformedValue);
		} catch (Exception ex) {
			return Result<TResult>.Fail(ex);
		}
	}


	// =============== THEN ASYNC ===============

	/// <summary>
	/// Executes a subsequent asynchronous operation if the preceding operation completes successfully.
	/// </summary>
	/// <remarks>If the preceding operation fails, the subsequent operation is not executed, and the failure result
	/// is returned. If an exception occurs during the execution of the subsequent operation, the result will indicate
	/// failure with the exception details.</remarks>
	/// <param name="resultTask">The <see cref="ValueTask{Result}"/> representing the preceding operation.</param>
	/// <param name="next">A function that returns a <see cref="ValueTask{Result}"/> representing the subsequent operation to execute if the
	/// preceding operation is successful.</param>
	/// <returns>A <see cref="ValueTask{Result}"/> representing the result of the subsequent operation if the preceding operation is
	/// successful; otherwise, the result of the preceding operation.</returns>
	public static async ValueTask<Result> ThenAsync(
		this ValueTask<Result> resultTask,
		Func<ValueTask<Result>> next) {

		ArgumentNullException.ThrowIfNull(next);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			return await next().ConfigureAwait(false);
		} catch (Exception ex) {
			return Result.Fail(ex);
		}
	}

	/// <summary>
	/// Chains another async operation that returns a Result if the current result is successful.
	/// </summary>
	/// <typeparam name="T">The type of the value in the current result.</typeparam>
	/// <typeparam name="TResult">The type of the value in the chained result.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="selector">The function that returns the next async result operation.</param>
	/// <returns>A task that represents the asynchronous operation, containing the result of the chained operation if successful, or the original failure.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
	public static async ValueTask<Result<TResult>> ThenAsync<T, TResult>(
		this ValueTask<Result<T>> resultTask,
		Func<T, ValueTask<Result<TResult>>> selector) {
		ArgumentNullException.ThrowIfNull(selector);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return Result<TResult>.Fail(result.Error!);
		}

		try {
			return await selector(result.Value!).ConfigureAwait(false);
		} catch (Exception ex) {
			return Result<TResult>.Fail(ex);
		}
	}

	/// <summary>
	/// Chains an asynchronous operation to execute if the preceding task completes successfully, propagating failure
	/// otherwise.
	/// </summary>
	/// <remarks>If the next operation throws an exception, the returned result will contain the exception as a
	/// failure. This method simplifies error propagation in asynchronous workflows by short-circuiting on
	/// failure.</remarks>
	/// <param name="resultTask">A task that represents the result of a previous operation. The next operation will only execute if this result
	/// indicates success.</param>
	/// <param name="next">A delegate that returns a task representing the next operation to perform if the previous result is successful.
	/// Cannot be null.</param>
	/// <returns>A task that represents the result of the chained operation. If the preceding result is not successful, the returned
	/// task contains that failure; otherwise, it contains the result of the next operation.</returns>
	public static async Task<Result> ThenAsync(
		this Task<Result> resultTask,
		Func<Task<Result>> next) {
		ArgumentNullException.ThrowIfNull(next);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			return await next().ConfigureAwait(false);
		} catch (Exception ex) {
			return Result.Fail(ex);
		}
	}

	/// <summary>
	/// Chains an asynchronous operation to a preceding asynchronous result, propagating errors and returning the outcome
	/// of the selector function if the initial result is successful.
	/// </summary>
	/// <remarks>If the initial result is not successful, the selector function is not invoked and the error is
	/// propagated. If the selector function throws an exception, the resulting result will contain that exception as an
	/// error.</remarks>
	/// <typeparam name="T">The type of the value contained in the initial result.</typeparam>
	/// <typeparam name="TResult">The type of the value produced by the selector function and contained in the resulting result.</typeparam>
	/// <param name="resultTask">A task that represents the initial asynchronous operation, yielding a result of type <typeparamref name="T"/>.</param>
	/// <param name="selector">A function to invoke if the initial result is successful. The function receives the value of type <typeparamref
	/// name="T"/> and returns a task that yields a result of type <typeparamref name="TResult"/>.</param>
	/// <returns>A task that represents the asynchronous operation. The result contains the outcome of the selector function if the
	/// initial result is successful; otherwise, it contains the error from the initial result or from the selector
	/// function.</returns>
	public static async Task<Result<TResult>> ThenAsync<T, TResult>(
		this Task<Result<T>> resultTask,
		Func<T, Task<Result<TResult>>> selector) {
		ArgumentNullException.ThrowIfNull(selector);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return Result<TResult>.Fail(result.Error!);
		}

		try {
			return await selector(result.Value!).ConfigureAwait(false);
		} catch (Exception ex) {
			return Result<TResult>.Fail(ex);
		}
	}


	// =============== WHERE ASYNC ===============

	/// <summary>
	/// Filters the result based on a specified condition.
	/// </summary>
	/// <typeparam name="T">The type of the value in the result.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="predicate">The condition to evaluate.</param>
	/// <param name="errorMessage">The error message if the condition is not met.</param>
	/// <returns>A task that represents the asynchronous operation, containing the filtered result.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="errorMessage"/> is null or whitespace.</exception>
	public static async ValueTask<Result<T>> WhereAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Func<T, bool> predicate,
		string errorMessage) {
		ArgumentNullException.ThrowIfNull(predicate);
		if (string.IsNullOrWhiteSpace(errorMessage)) {
			throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
		}

		var result = await resultTask.ConfigureAwait(false);
		return result.Where(predicate, errorMessage);
	}

	/// <summary>
	/// Filters the result based on an asynchronous condition.
	/// </summary>
	public static async ValueTask<Result<T>> WhereAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Func<T, ValueTask<bool>> predicate,
		string errorMessage) {
		ArgumentNullException.ThrowIfNull(predicate);
		if (string.IsNullOrWhiteSpace(errorMessage)) {
			throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
		}

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			var passes = await predicate(result.Value!).ConfigureAwait(false);
			return passes ? result : Result<T>.Fail(errorMessage);
		} catch (Exception ex) {
			return Result<T>.Fail(ex);
		}

	}

	/// <summary>
	/// Asynchronously evaluates a predicate against the value of a completed result and returns a failed result with a
	/// specified error message if the predicate is not satisfied.
	/// </summary>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="resultTask">A task that represents the asynchronous operation returning a result to be evaluated.</param>
	/// <param name="predicate">The function that defines the condition to evaluate against the result's value. Must not be null.</param>
	/// <param name="errorMessage">The error message to associate with the result if the predicate is not satisfied. Cannot be null or whitespace.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a successful result if the predicate is
	/// satisfied; otherwise, a failed result with the specified error message.</returns>
	/// <exception cref="ArgumentException">Thrown if errorMessage is null or consists only of white-space characters.</exception>
	public static async Task<Result<T>> WhereAsync<T>(
		this Task<Result<T>> resultTask,
		Func<T, bool> predicate,
		string errorMessage) {
		ArgumentNullException.ThrowIfNull(predicate);
		if (string.IsNullOrWhiteSpace(errorMessage)) {
			throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
		}

		var result = await resultTask.ConfigureAwait(false);
		return result.Where(predicate, errorMessage);
	}

	/// <summary>
	/// Asynchronously evaluates a predicate on the successful result value and returns a failed result with the specified
	/// error message if the predicate is not satisfied.
	/// </summary>
	/// <remarks>If the original result is not successful, the predicate is not evaluated and the original result is
	/// returned. If the predicate throws an exception, the returned result will be a failure containing the
	/// exception.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="resultTask">A task that represents the asynchronous operation returning a result to be filtered.</param>
	/// <param name="predicate">An asynchronous function that evaluates the result value and returns <see langword="true"/> to keep the result, or
	/// <see langword="false"/> to convert it to a failure.</param>
	/// <param name="errorMessage">The error message to use if the predicate returns <see langword="false"/>.</param>
	/// <returns>A task that represents the asynchronous operation. The result is successful if the original result is successful
	/// and the predicate returns <see langword="true"/>; otherwise, a failed result with the specified error message or
	/// the exception encountered during predicate evaluation.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="errorMessage"/> is null, empty, or consists only of white-space characters.</exception>
	public static async Task<Result<T>> WhereAsync<T>(
		this Task<Result<T>> resultTask,
		Func<T, Task<bool>> predicate,
		string errorMessage) {
		ArgumentNullException.ThrowIfNull(predicate);
		if (string.IsNullOrWhiteSpace(errorMessage)) {
			throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
		}

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			var passes = await predicate(result.Value!).ConfigureAwait(false);
			return passes ? result : Result<T>.Fail(errorMessage);
		} catch (Exception ex) {
			return Result<T>.Fail(ex);
		}
	}


}