namespace Cirreum;
/// <summary>
/// Provides extension methods for async operations with <see cref="Result{T}"/>.
/// </summary>
public static class ResultAsyncExtensions {


	// ============ ON SUCCESS ASYNC =============
	// ===========================================

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
	public static async Task<Result> OnSuccessAsyncTask(
		this Task<Result> resultTask,
		Action action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccess(action);
	}

	/// <summary>
	/// Also works with Task&lt;Result&lt;T&gt;&gt; for compatibility.
	/// </summary>
	public static async Task<Result<T>> OnSuccessAsyncTask<T>(
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


	// ======= ON SUCCESS TRY ASYNC ACTION =======
	// ===========================================

	/// <summary>
	/// Awaits the asynchronous operation and, if the resulting <see cref="Result"/> represents a success,
	/// executes the specified action.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is used to perform synchronous side-effects on the success path of an asynchronous
	/// result pipeline. Examples include logging, caching, metrics, or additional validation.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws an exception, the exception is caught and wrapped into a new
	/// failed <see cref="Result"/>. The optional <paramref name="errorSelector"/> allows transforming or
	/// wrapping the thrown exception before generating the failure.
	/// </para>
	/// <para>
	/// If the result represents a failure, the action is not executed and the original result is returned
	/// unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation producing the <see cref="Result"/>.</param>
	/// <param name="action">
	/// A synchronous action to execute when the result is successful.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function used to transform exceptions thrown by <paramref name="action"/> before being wrapped
	/// into a failed result.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding:
	/// <list type="bullet">
	/// <item>The original result if it is a failure.</item>
	/// <item>The original result if the success action completes successfully.</item>
	/// <item>A failed result if the success action throws.</item>
	/// </list>
	/// </returns>
	public static async ValueTask<Result> OnSuccessTryAsync(
		this ValueTask<Result> resultTask,
		Action action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccessTry(action, errorSelector);
	}

	/// <summary>
	/// Awaits the asynchronous operation and invokes the specified action when the resulting
	/// <see cref="Result"/> is successful.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is useful for introducing synchronous success-side behavior into an asynchronous
	/// result workflow—such as emitting success telemetry, caching the value, or updating related state.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the exception is caught and converted into a failed
	/// <see cref="Result"/>. If provided, <paramref name="errorSelector"/> can transform or wrap the thrown
	/// exception before producing the failure.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous result operation to evaluate.</param>
	/// <param name="action">
	/// A synchronous action to run when the result is successful.
	/// </param>
	/// <param name="errorSelector">
	/// Optional delegate for transforming exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding the original result unless the action throws,
	/// in which case a failed result is returned.
	/// </returns>
	public static async Task<Result> OnSuccessTryAsyncTask(
		this Task<Result> resultTask,
		Action action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccessTry(action, errorSelector);
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the specified action if the resulting
	/// <see cref="Result{T}"/> represents a success.
	/// </summary>
	/// <typeparam name="T">The type of the successful value contained in the result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method is intended for applying synchronous side-effects to successfully produced values.
	/// Typical scenarios include updating state, writing logs, applying transformations, or initiating
	/// downstream operations.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the thrown exception is caught and converted into a failed
	/// <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> may be used to wrap or transform
	/// the thrown exception before constructing the new failure.
	/// </para>
	/// <para>
	/// If the original result is a failure, the action is skipped and the original result is returned.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous result operation yielding the <see cref="Result{T}"/>.</param>
	/// <param name="action">
	/// A synchronous action invoked only when the result is successful.  
	/// Receives the success value of type <typeparamref name="T"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function to transform exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding:
	/// <list type="bullet">
	/// <item>The original result if it is a failure.</item>
	/// <item>The original result if the success action succeeds.</item>
	/// <item>A failed result if the success action throws.</item>
	/// </list>
	/// </returns>
	public static async Task<Result<T>> OnSuccessTryAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Action<T> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccessTry(action, errorSelector);
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the provided action when the resulting
	/// <see cref="Result{T}"/> indicates success.
	/// </summary>
	/// <typeparam name="T">The type of the value contained in a successful result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method enables synchronous success-side operations within an asynchronous result-processing
	/// workflow. It is useful for logging, post-processing, updating in-memory caches, or raising events.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws an exception, the exception is caught and wrapped into a new
	/// failed <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> can transform or wrap that
	/// exception before constructing the new failure.
	/// </para>
	/// <para>
	/// If the result is a failure, the action is not invoked and the original result is returned unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation that produces the <see cref="Result{T}"/>.</param>
	/// <param name="action">
	/// A synchronous action to perform on the successful value.
	/// </param>
	/// <param name="errorSelector">
	/// Optional delegate for transforming exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding either the original result or a failed result
	/// if the success action throws.
	/// </returns>
	public static async ValueTask<Result<T>> OnSuccessTryAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Action<T> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);
		return result.OnSuccessTry(action, errorSelector);
	}


	// ======= ON SUCCESS TRY ASYNC FUNC =========
	// ===========================================

	/// <summary>
	/// Awaits the asynchronous operation and, if the resulting <see cref="Result"/> represents a success,
	/// executes the specified asynchronous action.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method allows you to perform asynchronous side-effects—such as logging, telemetry emission,
	/// caching, or follow-up operations—when an asynchronous result evaluates to success.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws an exception, the exception is caught and wrapped into a new
	/// failed <see cref="Result"/>. The optional <paramref name="errorSelector"/> allows the thrown exception
	/// to be transformed or wrapped into a more meaningful exception before becoming the failure.
	/// </para>
	/// <para>
	/// If the result represents a failure, the action is not executed and the original result is returned
	/// unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">
	/// The asynchronous operation that produces the <see cref="Result"/>.
	/// </param>
	/// <param name="action">
	/// An asynchronous function executed only when the result indicates success.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function used to transform exceptions thrown by <paramref name="action"/> before they are
	/// wrapped into a failed result.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding:
	/// <list type="bullet">
	/// <item>The original result, if it represents a failure.</item>
	/// <item>The original result, if the asynchronous success action completes successfully.</item>
	/// <item>A failed result, if the success action throws.</item>
	/// </list>
	/// </returns>
	public static async ValueTask<Result> OnSuccessTryAsync(
		this ValueTask<Result> resultTask,
		Func<Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			await action().ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and invokes the specified asynchronous action if the resulting
	/// <see cref="Result"/> represents a success.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method enables asynchronous success-side behavior within a result-processing pipeline. It is
	/// suitable for scenarios such as updating distributed caches, writing telemetry, performing secondary
	/// fetches, or raising async notifications.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws an exception, the exception is caught and converted into a
	/// failed <see cref="Result"/>. If supplied, <paramref name="errorSelector"/> may wrap or transform the
	/// exception before it becomes the failure.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation producing the <see cref="Result"/>.</param>
	/// <param name="action">
	/// An asynchronous function executed when the result is successful.
	/// </param>
	/// <param name="errorSelector">
	/// Optional delegate that transforms exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding the original result unless the action throws, in which case a
	/// failed result is returned.
	/// </returns>
	public static async Task<Result> OnSuccessTryAsyncTask(
		this Task<Result> resultTask,
		Func<Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			await action().ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the specified asynchronous action when the resulting
	/// <see cref="Result{T}"/> represents a success.
	/// </summary>
	/// <typeparam name="T">The type of the successful value contained in the result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method is used for asynchronous post-processing once a value of type <typeparamref name="T"/>
	/// has been successfully produced. Typical scenarios include writing audit data, sending notifications,
	/// computing dependent state, or chaining additional asynchronous reads.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the thrown exception is caught and wrapped into a new failed
	/// <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> may transform that exception
	/// before it is wrapped.
	/// </para>
	/// <para>
	/// If the result is a failure, the action is not executed and the original result is returned.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation yielding the <see cref="Result{T}"/>.</param>
	/// <param name="action">
	/// An asynchronous function invoked only when the result is successful.  
	/// Receives the value of type <typeparamref name="T"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function used to transform exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding either the original result or a new failed result
	/// if the asynchronous success action throws.
	/// </returns>
	public static async Task<Result<T>> OnSuccessTryAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Func<T, Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			await action(result.Value!).ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result<T>.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the specified asynchronous action if the resulting
	/// <see cref="Result{T}"/> indicates success.
	/// </summary>
	/// <typeparam name="T">The type of the value contained in a successful result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method enables asynchronous success-side behavior in a result-processing pipeline. It is
	/// particularly useful for operations such as persisting data, emitting distributed events, or performing
	/// asynchronous materialization of related state.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws an exception, the exception is caught and wrapped into a new
	/// failed <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> allows customizing or
	/// transforming that exception before constructing the failure.
	/// </para>
	/// <para>
	/// If the result is a failure, the action is skipped and the original result is preserved.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">
	/// The asynchronous operation that yields the <see cref="Result{T}"/>.
	/// </param>
	/// <param name="action">
	/// An asynchronous function executed only when the result is successful.  
	/// Receives the successful value of type <typeparamref name="T"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function that transforms exceptions thrown by <paramref name="action"/> before they are
	/// wrapped into a failed result.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding:
	/// <list type="bullet">
	/// <item>The original result if it is a failure.</item>
	/// <item>The original result if the asynchronous action completes successfully.</item>
	/// <item>A failed result if the asynchronous action throws.</item>
	/// </list>
	/// </returns>
	public static async ValueTask<Result<T>> OnSuccessTryAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Func<T, Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			await action(result.Value!).ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result<T>.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}


	// =========== ON FAILURE ASYNC ==============
	// ===========================================

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
	public static async Task<Result<T>> OnFailureAsyncTask<T>(
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
	public static async Task<Result> OnFailureAsyncTask(
		this Task<Result> resultTask,
		Action<Exception> action) {
		ArgumentNullException.ThrowIfNull(action);
		var result = await resultTask.ConfigureAwait(false);
		return result.OnFailure(action);
	}


	// ======= ON FAILURE TRY ASYNC ACTION =======
	// ===========================================

	/// <summary>
	/// Awaits the asynchronous operation and, if the resulting <see cref="Result"/> represents a failure,
	/// executes the specified action using the associated exception.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is intended for scenarios where additional failure-side processing is required—such as
	/// logging, compensating actions, or emitting telemetry—when a <see cref="Result"/> indicates failure.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws an exception, the exception is caught and converted into a new
	/// failed <see cref="Result"/>. The optional <paramref name="errorSelector"/> allows transforming or wrapping
	/// the thrown exception before it becomes the failure.
	/// </para>
	/// <para>
	/// If the original result is successful, or represents a failure without an associated exception, the
	/// <paramref name="action"/> is skipped and the original result is returned unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">
	/// The asynchronous operation producing the <see cref="Result"/> to inspect.
	/// </param>
	/// <param name="action">
	/// A synchronous action to execute using the failure's <see cref="Exception"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional delegate used to transform any exception thrown by <paramref name="action"/> before it is
	/// wrapped into a new failed <see cref="Result"/>.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding:
	/// <list type="bullet">
	/// <item>The original result if it does not represent a failure.</item>
	/// <item>The original result if <paramref name="action"/> completes successfully.</item>
	/// <item>A failed result if <paramref name="action"/> throws.</item>
	/// </list>
	/// </returns>
	public static async ValueTask<Result> OnFailureTryAsync(
		this ValueTask<Result> resultTask,
		Action<Exception> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			action(result.Error);
			return result;
		} catch (Exception ex) {
			return Result.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the provided action if the resulting
	/// <see cref="Result"/> indicates a failure.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use this method to introduce synchronous side-effects—such as logging, metrics, or cleanup—in the
	/// failure branch of an asynchronous result workflow.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the thrown exception is captured and wrapped into a new failed
	/// <see cref="Result"/>. If supplied, <paramref name="errorSelector"/> allows you to transform that exception
	/// before constructing the new failure.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation yielding the <see cref="Result"/>.</param>
	/// <param name="action">
	/// A synchronous delegate invoked only when the result is a failure.  
	/// Receives the failure's <see cref="Exception"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function for transforming exceptions thrown by <paramref name="action"/> prior to wrapping.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding either the original result or a failed result if the action throws.
	/// </returns>
	public static async Task<Result> OnFailureTryAsyncTask(
		this Task<Result> resultTask,
		Action<Exception> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			action(result.Error);
			return result;
		} catch (Exception ex) {
			return Result.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the given action when the resulting
	/// <see cref="Result{T}"/> represents a failure.
	/// </summary>
	/// <typeparam name="T">The type of the successful value contained in the result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method enables synchronous failure-side processing within an asynchronous result pipeline.
	/// For example, performing local error handling, telemetry emission, or compensating actions.
	/// </para>
	/// <para>
	/// If the original result is successful—or is a failure without an associated exception—the action is
	/// not invoked, and the original result is returned.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the thrown exception is caught and converted into a new failed
	/// <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> can be used to transform the thrown
	/// exception before forming the new failure.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation yielding the <see cref="Result{T}"/>.</param>
	/// <param name="action">
	/// The action to execute if the result represents a failure.  
	/// Receives the failure's <see cref="Exception"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional delegate used to transform exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> producing either the original result or a new failed result.
	/// </returns>
	public static async Task<Result<T>> OnFailureTryAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Action<Exception> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			action(result.Error);
			return result;
		} catch (Exception ex) {
			return Result<T>.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and, if the resulting <see cref="Result{T}"/> indicates a failure,
	/// executes the specified action with the associated exception.
	/// </summary>
	/// <typeparam name="T">The type of the value contained in a successful result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method is designed to support synchronous failure-handling logic in an asynchronous result
	/// workflow. Typical scenarios include error logging, fallback assignment, or cleanup steps.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the resulting exception is caught and converted into a new failed
	/// <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> may be used to control how that
	/// exception is transformed before being wrapped.
	/// </para>
	/// <para>
	/// If the original result is successful or contains no exception instance, the action is not called and
	/// the original result is returned unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">
	/// The asynchronous operation that produces the <see cref="Result{T}"/>.
	/// </param>
	/// <param name="action">
	/// A synchronous delegate executed when the result represents a failure.  
	/// Receives the failure's <see cref="Exception"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function to transform exceptions thrown by <paramref name="action"/> before creating the
	/// new failed result.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding the original result or a new failed result if the action throws.
	/// </returns>
	public static async ValueTask<Result<T>> OnFailureTryAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Action<Exception> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			action(result.Error);
			return result;
		} catch (Exception ex) {
			return Result<T>.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}


	// ======= ON FAILURE TRY ASYNC FUNC =========
	// ===========================================

	/// <summary>
	/// Awaits the asynchronous operation and, if the resulting <see cref="Result"/> represents a failure,
	/// executes the specified asynchronous action using the associated error.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method is intended for scenarios where additional asynchronous processing is required when
	/// a failure occurs—such as logging, compensating actions, cleanup routines, or telemetry.
	/// </para>
	/// <para>
	/// If the <paramref name="action"/> throws an exception, that exception is caught and converted into a new
	/// failed <see cref="Result"/>. The optional <paramref name="errorSelector"/> can be used to transform or wrap
	/// the thrown exception before it becomes the new failure.
	/// </para>
	/// <para>
	/// If the original result is successful—or if it is a failure without an error instance—then the
	/// <paramref name="action"/> is not executed, and the original result is returned unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">
	/// The asynchronous operation that produces the <see cref="Result"/> to evaluate.
	/// </param>
	/// <param name="action">
	/// An asynchronous function to execute if the result represents a failure.  
	/// Receives the <see cref="Exception"/> associated with the failure.
	/// </param>
	/// <param name="errorSelector">
	/// An optional function used to transform an exception thrown by <paramref name="action"/> into a different
	/// exception before constructing the failed <see cref="Result"/>.  
	/// If <c>null</c>, the thrown exception is used as-is.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> producing:
	/// <list type="bullet">
	/// <item>The original result if it was successful or had no error instance.</item>
	/// <item>The original result if <paramref name="action"/> completes successfully.</item>
	/// <item>A failed result if <paramref name="action"/> throws, containing the transformed exception.</item>
	/// </list>
	/// </returns>
	public static async ValueTask<Result> OnFailureTryAsync(
		this ValueTask<Result> resultTask,
		Func<Exception, Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			await action(result.Error).ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and invokes the specified asynchronous action if the
	/// produced <see cref="Result"/> is a failure.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method enables asynchronous failure-handling pipelines where additional operations
	/// must run only when the result is unsuccessful.
	/// </para>
	/// <para>
	/// If the <paramref name="action"/> throws an exception, the thrown exception is converted into a new failed
	/// <see cref="Result"/>. If provided, <paramref name="errorSelector"/> can wrap or transform the exception
	/// before it becomes the new failure.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation producing the <see cref="Result"/>.</param>
	/// <param name="action">
	/// An asynchronous function executed when the result represents a failure.  
	/// Receives the failure's <see cref="Exception"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional delegate used to transform any exception thrown by <paramref name="action"/> before it is
	/// wrapped into a failed <see cref="Result"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding the original result, or a new failed result if the failure handler throws.
	/// </returns>
	public static async Task<Result> OnFailureTryAsyncTask(
		this Task<Result> resultTask,
		Func<Exception, Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			await action(result.Error).ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the specified asynchronous failure handler if the
	/// resulting <see cref="Result{T}"/> represents a failure.
	/// </summary>
	/// <typeparam name="T">The type of the successful value contained in the result.</typeparam>
	/// <remarks>
	/// <para>
	/// This method is useful for composing asynchronous failure-handling logic within a result pipeline,
	/// such as emitting error telemetry, performing retries, or populating fallback data.
	/// </para>
	/// <para>
	/// If the original result is successful—or if it is a failure without an associated error—then the
	/// <paramref name="action"/> is not executed and the original result is returned.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws, the exception is converted into a new failed
	/// <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> can transform the thrown exception
	/// before it is wrapped.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation producing the <see cref="Result{T}"/>.</param>
	/// <param name="action">
	/// An asynchronous function executed only if the result represents a failure.
	/// Receives the failure's <see cref="Exception"/>.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function used to transform exceptions thrown by <paramref name="action"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> yielding either the original result or a failed result if the failure handler throws.
	/// </returns>
	public static async Task<Result<T>> OnFailureTryAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Func<Exception, Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			await action(result.Error).ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result<T>.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}

	/// <summary>
	/// Awaits the asynchronous operation and executes the provided asynchronous action when the
	/// resulting <see cref="Result{T}"/> represents a failure.
	/// </summary>
	/// <typeparam name="T">The type of the value contained in a successful result.</typeparam>
	/// <remarks>
	/// <para>
	/// The supplied <paramref name="action"/> runs only when the result is a failure and contains a non-null
	/// <see cref="Exception"/>. This makes the method ideal for performing asynchronous side-effects such as
	/// error logging, audit events, or compensation logic.
	/// </para>
	/// <para>
	/// If <paramref name="action"/> throws its own exception, that exception is caught and converted into a new
	/// failed <see cref="Result{T}"/>. The optional <paramref name="errorSelector"/> may be used to transform or
	/// wrap the thrown exception before constructing the new failure.
	/// </para>
	/// <para>
	/// If the original result is successful, or represents a failure without an exception instance, the action is
	/// skipped and the original result is returned unchanged.
	/// </para>
	/// </remarks>
	/// <param name="resultTask">The asynchronous operation yielding the <see cref="Result{T}"/>.</param>
	/// <param name="action">
	/// An asynchronous function to execute when the result represents a failure.
	/// Receives the <see cref="Exception"/> associated with the failure.
	/// </param>
	/// <param name="errorSelector">
	/// Optional function for transforming exceptions thrown by <paramref name="action"/> before wrapping them in
	/// a new failed result.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> yielding either:
	/// <list type="bullet">
	/// <item>The original result, if successful.</item>
	/// <item>The original result, if the failure handler succeeds.</item>
	/// <item>A failed result, if the failure handler throws.</item>
	/// </list>
	/// </returns>
	public static async ValueTask<Result<T>> OnFailureTryAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Func<Exception, Task> action,
		Func<Exception, Exception>? errorSelector = null) {
		ArgumentNullException.ThrowIfNull(action);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsFailure || result.Error is null) {
			return result;
		}

		try {
			await action(result.Error).ConfigureAwait(false);
			return result;
		} catch (Exception ex) {
			return Result<T>.Fail(errorSelector?.Invoke(ex) ?? ex);
		}
	}


	// ================ MAP ASYNC ================
	// ===========================================

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
	public static async Task<Result<T>> MapAsyncTask<T>(
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
	public static async Task<Result<TResult>> MapAsyncTask<T, TResult>(
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
	public static async Task<Result<TResult>> MapAsyncTask<T, TResult>(
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


	// ================ THEN ASYNC ===============
	// ===========================================

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
	public static async Task<Result> ThenAsyncTask(
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
	public static async Task<Result<TResult>> ThenAsyncTask<T, TResult>(
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
	// ===========================================

	/// <summary>
	/// Filters the result based on a specified condition.
	/// </summary>
	/// <typeparam name="T">The type of the value in the result.</typeparam>
	/// <param name="resultTask">The task containing the result.</param>
	/// <param name="predicate">The condition to evaluate.</param>
	/// <param name="error">The error message if the condition is not met.</param>
	/// <returns>A task that represents the asynchronous operation, containing the filtered result.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="error"/> is null or whitespace.</exception>
	public static async ValueTask<Result<T>> WhereAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Func<T, bool> predicate,
		Exception error) {
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(error);

		var result = await resultTask.ConfigureAwait(false);
		return result.Where(predicate, error);
	}

	/// <summary>
	/// Filters the result based on an asynchronous condition.
	/// </summary>
	public static async ValueTask<Result<T>> WhereAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Func<T, ValueTask<bool>> predicate,
		Exception error) {
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(error);

		var result = await resultTask.ConfigureAwait(false);

		if (!result.IsSuccess) {
			return result;
		}

		try {
			var passes = await predicate(result.Value!).ConfigureAwait(false);
			return passes ? result : Result<T>.Fail(error);
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
	/// <param name="error">The error message to associate with the result if the predicate is not satisfied. Cannot be null or whitespace.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a successful result if the predicate is
	/// satisfied; otherwise, a failed result with the specified error message.</returns>
	/// <exception cref="ArgumentException">Thrown if errorMessage is null or consists only of white-space characters.</exception>
	public static async Task<Result<T>> WhereAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Func<T, bool> predicate,
		Exception error) {
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(error);

		var result = await resultTask.ConfigureAwait(false);
		return result.Where(predicate, error);
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
	/// <param name="error">The error message to use if the predicate returns <see langword="false"/>.</param>
	/// <returns>A task that represents the asynchronous operation. The result is successful if the original result is successful
	/// and the predicate returns <see langword="true"/>; otherwise, a failed result with the specified error message or
	/// the exception encountered during predicate evaluation.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="error"/> is null, empty, or consists only of white-space characters.</exception>
	public static async Task<Result<T>> WhereAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Func<T, Task<bool>> predicate,
		Exception error) {
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(error);

		var result = await resultTask.ConfigureAwait(false);

		if (result.IsSuccess) {
			try {
				var passes = await predicate(result.Value).ConfigureAwait(false);
				return passes ? result : Result<T>.Fail(error);
			} catch (Exception ex) {
				return Result<T>.Fail(ex);
			}
		}

		return result;
	}


	// =============== SWITCH ASYNC ===============
	// ===========================================

	/// <summary>
	/// Awaits the asynchronous <see cref="Task{TResult}"/> producing a <see cref="Result"/>,
	/// and executes the appropriate action based on its success or failure state.
	/// </summary>
	/// <param name="resultTask">The task that yields the <see cref="Result"/>.</param>
	/// <param name="onSuccess">Action to execute when the result is successful.</param>
	/// <param name="onFailure">Action to execute when the result represents a failure.</param>
	/// <returns>
	/// A <see cref="Task"/> that completes after the appropriate branch action has been executed.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method enables ergonomic branching on the outcome of an asynchronous operation without
	/// manually awaiting the result before calling <see cref="Result.Switch"/>.
	/// </remarks>
	public static async Task SwitchAsyncTask(
		this Task<Result> resultTask,
		Action onSuccess,
		Action<Exception> onFailure) {

		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		var result = await resultTask.ConfigureAwait(false);
		result.Switch(onSuccess, onFailure);
	}

	/// <summary>
	/// Awaits the <see cref="ValueTask{TResult}"/> producing a <see cref="Result"/>,
	/// and executes the appropriate action depending on its success or failure.
	/// </summary>
	/// <param name="resultTask">The value task that yields the <see cref="Result"/>.</param>
	/// <param name="onSuccess">Action to execute if the result is successful.</param>
	/// <param name="onFailure">Action to execute if the result is a failure.</param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes after the selected branch action runs.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method matches the behavior of <see cref="SwitchAsyncTask(Task{Result}, Action, Action{Exception})"/>
	/// but supports <see cref="ValueTask{TResult}"/> for improved performance in synchronous or pooled paths.
	/// </remarks>
	public static async ValueTask SwitchAsync(
		this ValueTask<Result> resultTask,
		Action onSuccess,
		Action<Exception> onFailure) {

		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		var result = await resultTask.ConfigureAwait(false);
		result.Switch(onSuccess, onFailure);
	}

	/// <summary>
	/// Awaits the asynchronous <see cref="Task{TResult}"/> producing a <see cref="Result{T}"/>,
	/// and executes the appropriate action depending on whether the result contains a value
	/// or an error.
	/// </summary>
	/// <typeparam name="T">The type of the value in the successful result.</typeparam>
	/// <param name="resultTask">The task that yields the <see cref="Result{T}"/>.</param>
	/// <param name="onSuccess">Action to execute when the result is successful.</param>
	/// <param name="onFailure">Action to execute when the result is a failure.</param>
	/// <returns>A <see cref="Task"/> that completes after executing the corresponding action.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method provides an ergonomic way to process successful or failed outcomes
	/// without requiring an explicit await before calling <see cref="Result{T}.Switch"/>.
	/// </remarks>
	public static async Task SwitchAsyncTask<T>(
		this Task<Result<T>> resultTask,
		Action<T> onSuccess,
		Action<Exception> onFailure) {

		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		var result = await resultTask.ConfigureAwait(false);
		result.Switch(onSuccess, onFailure);
	}

	/// <summary>
	/// Awaits the <see cref="ValueTask{TResult}"/> producing a <see cref="Result{T}"/>,
	/// and invokes the appropriate action based on its success or failure state.
	/// </summary>
	/// <typeparam name="T">The type of the contained value if the result is successful.</typeparam>
	/// <param name="resultTask">The value task yielding the <see cref="Result{T}"/>.</param>
	/// <param name="onSuccess">Action executed when the result is successful.</param>
	/// <param name="onFailure">Action executed when the result is a failure.</param>
	/// <returns>A <see cref="ValueTask"/> that completes after the appropriate action has run.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method mirrors <see cref="SwitchAsyncTask{T}(Task{Result{T}}, Action{T}, Action{Exception})"/>
	/// but supports asynchronous pipelines using <see cref="ValueTask{TResult}"/>.
	/// </remarks>
	public static async ValueTask SwitchAsync<T>(
		this ValueTask<Result<T>> resultTask,
		Action<T> onSuccess,
		Action<Exception> onFailure) {

		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		var result = await resultTask.ConfigureAwait(false);
		result.Switch(onSuccess, onFailure);
	}


	// =============== MATCH ASYNC ===============
	// ===========================================

	/// <summary>
	/// Asynchronously projects the specified result into a value of type
	/// <typeparamref name="TOut"/> by invoking the appropriate asynchronous
	/// function for the success or failure case.
	/// </summary>
	public static async Task<TOut> MatchAsync<TOut>(
		this Result result,
		Func<ValueTask<TOut>> onSuccess,
		Func<Exception, ValueTask<TOut>> onFailure) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		if (result.IsSuccess) {
			return await onSuccess().ConfigureAwait(false);
		}

		return await onFailure(result.Error).ConfigureAwait(false);
	}

	/// <summary>
	/// Asynchronously projects the specified result into a value of type
	/// <typeparamref name="TOut"/> by invoking the appropriate asynchronous
	/// function for the success or failure case.
	/// </summary>
	public static async Task<TOut> MatchAsyncTask<TOut>(
		this Result result,
		Func<Task<TOut>> onSuccess,
		Func<Exception, Task<TOut>> onFailure) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		if (result.IsSuccess) {
			return await onSuccess().ConfigureAwait(false);
		}

		return await onFailure(result.Error).ConfigureAwait(false);
	}

	/// <summary>
	/// Asynchronously projects the specified result into a value of type
	/// <typeparamref name="TOut"/> by invoking the appropriate asynchronous
	/// function for the success or failure case.
	/// </summary>
	public static async Task<TOut> MatchAsync<T, TOut>(
		this Result<T> result,
		Func<T, ValueTask<TOut>> onSuccess,
		Func<Exception, ValueTask<TOut>> onFailure) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		if (result.IsSuccess) {
			return await onSuccess(result.Value).ConfigureAwait(false);
		}

		return await onFailure(result.Error).ConfigureAwait(false);
	}

	/// <summary>
	/// Asynchronously projects the specified result into a value of type
	/// <typeparamref name="TOut"/> by invoking the appropriate asynchronous
	/// function for the success or failure case.
	/// </summary>
	public static async Task<TOut> MatchAsyncTask<T, TOut>(
		this Result<T> result,
		Func<T, Task<TOut>> onSuccess,
		Func<Exception, Task<TOut>> onFailure) {
		ArgumentNullException.ThrowIfNull(onSuccess);
		ArgumentNullException.ThrowIfNull(onFailure);

		if (result.IsSuccess) {
			return await onSuccess(result.Value).ConfigureAwait(false);
		}

		return await onFailure(result.Error).ConfigureAwait(false);
	}

}