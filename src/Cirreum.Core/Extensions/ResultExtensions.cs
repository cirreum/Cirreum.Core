namespace Cirreum;

using FluentValidation;
using FluentValidation.Results;

/// <summary>
/// Provides extension methods for creating failed results that represent common error conditions, such as not found,
/// already exists, bad request, or validation failures.
/// </summary>
/// <remarks>These extension methods simplify the creation of standardized failed results by encapsulating common
/// exception types and error scenarios. They are intended to promote consistent error handling patterns throughout the
/// application. Each method returns a failed result containing an appropriate exception, making it easier to propagate
/// and handle errors in a uniform way.</remarks>
public static class ResultExtensions {

	extension(Result) {

		// ============================ NOT VALID ================================

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified message.</returns>
		public static Result<T> NotValid<T>(string message) {
			return Result<T>.Fail(new ValidationException(message));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message.
		/// </summary>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified message.</returns>
		public static Result NotValid(string message) {
			return Result.Fail(new ValidationException(message));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified validation failures.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="failures">One or more validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified failures.</returns>
		public static Result<T> NotValid<T>(params ReadOnlySpan<ValidationFailure> failures) {
			return Result<T>.Fail(new ValidationException([.. failures]));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified validation failures.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified failures.</returns>
		public static Result<T> NotValid<T>(IEnumerable<ValidationFailure> failures) {
			return Result<T>.Fail(new ValidationException(failures));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified validation failures.
		/// </summary>
		/// <param name="failures">One or more validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified failures.</returns>
		public static Result NotValid(params ReadOnlySpan<ValidationFailure> failures) {
			return Result.Fail(new ValidationException([.. failures]));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified validation failures.
		/// </summary>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified failures.</returns>
		public static Result NotValid(IEnumerable<ValidationFailure> failures) {
			return Result.Fail(new ValidationException(failures));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message and validation failures.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <param name="failures">One or more validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified message and failures.</returns>
		public static Result<T> NotValid<T>(string message, params ReadOnlySpan<ValidationFailure> failures) {
			return Result<T>.Fail(new ValidationException(message, [.. failures]));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message and validation failures.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified message and failures.</returns>
		public static Result<T> NotValid<T>(string message, IEnumerable<ValidationFailure> failures) {
			return Result<T>.Fail(new ValidationException(message, failures));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message and validation failures.
		/// </summary>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <param name="failures">One or more validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified message and failures.</returns>
		public static Result NotValid(string message, params ReadOnlySpan<ValidationFailure> failures) {
			return Result.Fail(new ValidationException(message, [.. failures]));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message and validation failures.
		/// </summary>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified message and failures.</returns>
		public static Result NotValid(string message, IEnumerable<ValidationFailure> failures) {
			return Result.Fail(new ValidationException(message, failures));
		}

	}

}