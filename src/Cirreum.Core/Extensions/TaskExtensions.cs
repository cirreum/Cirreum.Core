namespace System.Threading.Tasks;

using Cirreum;
using System;
using System.Threading;

public static class TaskExtensions {

	private static readonly TimeSpan Zero = TimeSpan.Zero;

	#region Public Methods

	/// <summary>
	/// Calls the specified <paramref name="condition"/> repeatedly until it returns <see langword="true"/>
	/// or the timeout expires.
	/// </summary>
	/// <param name="condition">The condition to call.</param>
	/// <param name="timeoutSeconds">The maximum seconds to wait for the condition to return true.</param>
	/// <param name="delayMilliseconds">The delay between condition checks in milliseconds.</param>
	/// <returns>A Task representing the wait operation, completing with the final condition result.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutSeconds or delayMilliseconds is negative.</exception>
	public static Task<bool> WaitUntilTrueAsync(
		this Func<bool> condition,
		int timeoutSeconds,
		int delayMilliseconds = 500) {
		ValidateParameters(timeoutSeconds, delayMilliseconds);
		Task<bool> Condition() => Task.FromResult(condition());
		return WaitForConditionAsync(
			Condition,
			TimeSpan.FromSeconds(timeoutSeconds),
			delayMilliseconds,
			true,
			CancellationToken.None);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="true"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilTrueAsync(
		this Func<Task<bool>> condition,
		int timeoutSeconds,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeoutSeconds, delayMilliseconds);
		return WaitForConditionAsync(
			condition,
			TimeSpan.FromSeconds(timeoutSeconds),
			delayMilliseconds,
			true,
			cancellationToken);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="true"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilTrueAsync(
		this Func<CancellationToken, Task<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeout, delayMilliseconds);
		return WaitForConditionAsync(
			() => condition(cancellationToken),
			timeout,
			delayMilliseconds,
			true,
			cancellationToken);
	}

	/// <summary>
	/// Calls the specified <paramref name="condition"/> repeatedly until it returns <see langword="false"/>
	/// or the timeout expires.
	/// </summary>
	public static Task<bool> WaitUntilFalseAsync(
		this Func<bool> condition,
		int timeoutSeconds,
		int delayMilliseconds = 500) {
		ValidateParameters(timeoutSeconds, delayMilliseconds);
		Task<bool> Condition() => Task.FromResult(condition());
		return WaitForConditionAsync(
			Condition,
			TimeSpan.FromSeconds(timeoutSeconds),
			delayMilliseconds,
			false,
			CancellationToken.None);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="false"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilFalseAsync(
		this Func<Task<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeout, delayMilliseconds);
		return WaitForConditionAsync(
			condition,
			timeout,
			delayMilliseconds,
			false,
			cancellationToken);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="false"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilFalseAsync(
		this Func<CancellationToken, Task<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeout, delayMilliseconds);
		return WaitForConditionAsync(
			() => condition(cancellationToken),
			timeout,
			delayMilliseconds,
			false,
			cancellationToken);
	}

	#endregion

	#region Private Methods

	private static void ValidateParameters(TimeSpan timeout, int delayMilliseconds) {
		if (timeout < Zero) {
			throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be non-negative.");
		}

		if (delayMilliseconds < 0) {
			throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "Delay must be non-negative.");
		}
	}

	private static void ValidateParameters(int timeoutSeconds, int delayMilliseconds) {
		if (timeoutSeconds < 0) {
			throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be non-negative.");
		}

		if (delayMilliseconds < 0) {
			throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "Delay must be non-negative.");
		}
	}

	private static async Task<bool> WaitForConditionAsync(
		Func<Task<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds,
		bool targetValue,
		CancellationToken cancellationToken) {
		var delay = TimeSpan.FromMilliseconds(delayMilliseconds);
		var conditionResult = !targetValue;
		var sw = Timing.Start();

		while (conditionResult != targetValue) {
			if (Timing.GetElapsedTime(sw) >= timeout || cancellationToken.IsCancellationRequested) {
				break;
			}

			try {
				conditionResult = await condition().ConfigureAwait(false);
				if (conditionResult == targetValue) {
					break;
				}

				if (Timing.GetElapsedTime(sw) < timeout && !cancellationToken.IsCancellationRequested) {
					await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
				}
			} catch (OperationCanceledException) {
				break;
			}
		}

		return conditionResult;
	}

	#endregion

	#region Public Methods for ValueTask<bool>

	/// <summary>
	/// Calls the specified <paramref name="condition"/> repeatedly until it returns <see langword="true"/>
	/// or the timeout expires.
	/// </summary>
	public static Task<bool> WaitUntilTrueAsync(
		this Func<ValueTask<bool>> condition,
		int timeoutSeconds,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeoutSeconds, delayMilliseconds);
		return WaitForValueTaskConditionAsync(
			condition,
			TimeSpan.FromSeconds(timeoutSeconds),
			delayMilliseconds,
			true,
			cancellationToken);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="true"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilTrueAsync(
		this Func<CancellationToken, ValueTask<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeout, delayMilliseconds);
		return WaitForValueTaskConditionAsync(
			() => condition(cancellationToken),
			timeout,
			delayMilliseconds,
			true,
			cancellationToken);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="false"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilFalseAsync(
		this Func<ValueTask<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeout, delayMilliseconds);
		return WaitForValueTaskConditionAsync(
			condition,
			timeout,
			delayMilliseconds,
			false,
			cancellationToken);
	}

	/// <summary>
	/// Calls the specified async <paramref name="condition"/> repeatedly until it returns <see langword="false"/>
	/// or the timeout expires or cancellation is requested.
	/// </summary>
	public static Task<bool> WaitUntilFalseAsync(
		this Func<CancellationToken, ValueTask<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds = 500,
		CancellationToken cancellationToken = default) {
		ValidateParameters(timeout, delayMilliseconds);
		return WaitForValueTaskConditionAsync(
			() => condition(cancellationToken),
			timeout,
			delayMilliseconds,
			false,
			cancellationToken);
	}

	#endregion

	#region Private Methods

	private static async Task<bool> WaitForValueTaskConditionAsync(
		Func<ValueTask<bool>> condition,
		TimeSpan timeout,
		int delayMilliseconds,
		bool targetValue,
		CancellationToken cancellationToken) {
		var delay = TimeSpan.FromMilliseconds(delayMilliseconds);
		var conditionResult = !targetValue;
		var sw = Timing.Start();

		while (conditionResult != targetValue) {
			if (Timing.GetElapsedTime(sw) >= timeout || cancellationToken.IsCancellationRequested) {
				break;
			}

			try {
				conditionResult = await condition().ConfigureAwait(false);
				if (conditionResult == targetValue) {
					break;
				}

				if (Timing.GetElapsedTime(sw) < timeout && !cancellationToken.IsCancellationRequested) {
					await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
				}
			} catch (OperationCanceledException) {
				break;
			}
		}

		return conditionResult;
	}

	#endregion

}