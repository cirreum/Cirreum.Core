namespace Cirreum;

using Cirreum.Exceptions;
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

		/// <summary>
		/// Creates a failed result indicating that one or more items with the specified keys were not found.
		/// </summary>
		/// <typeparam name="T">The type of the value that would have been returned if the item was found.</typeparam>
		/// <param name="keys">One or more keys used to identify the items that were not found.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="NotFoundException"/> with the specified keys.</returns>
		public static Result<T> NotFound<T>(params ReadOnlySpan<object> keys) {
			return Result<T>.Fail(new NotFoundException(keys));
		}

		/// <summary>
		/// Creates a failed result indicating that an entity already exists, using the specified error message.
		/// </summary>
		/// <typeparam name="T">The type of the value associated with the result.</typeparam>
		/// <param name="message">The error message describing the reason the entity is considered to already exist. Cannot be null.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing an <see cref="AlreadyExistsException"/> with the specified message.</returns>
		public static Result<T> AlreadyExist<T>(string message) {
			return Result<T>.Fail(new AlreadyExistsException(message));
		}

		/// <summary>
		/// Creates a failed result that represents a bad request error with the specified message.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="message">The error message that describes the reason for the bad request.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="BadRequestException"/> with the specified message.</returns>
		public static Result<T> BadRequest<T>(string message) {
			return Result<T>.Fail(new BadRequestException(message));
		}

		/// <summary>
		/// Creates a failed result that represents a bad request error with the specified message.
		/// </summary>
		/// <param name="message">The error message that describes the reason for the bad request.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="BadRequestException"/> with the specified message.</returns>
		public static Result BadRequest(string message) {
			return Result.Fail(new BadRequestException(message));
		}

		/// <summary>
		/// Creates a failed result indicating a conflict with the current state of the resource.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="message">The error message describing the conflict.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ConflictException"/> with the specified message.</returns>
		public static Result<T> Conflict<T>(string message) {
			return Result<T>.Fail(new ConflictException(message));
		}

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
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified failures.</returns>
		public static Result<T> NotValid<T>(params IEnumerable<ValidationFailure> failures) {
			return Result<T>.Fail(new ValidationException(failures));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified validation failures.
		/// </summary>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified failures.</returns>
		public static Result NotValid(params IEnumerable<ValidationFailure> failures) {
			return Result.Fail(new ValidationException(failures));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message and validation failures.
		/// </summary>
		/// <typeparam name="T">The type of the value that would be returned on success.</typeparam>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result{T}"/> containing a <see cref="ValidationException"/> with the specified message and failures.</returns>
		public static Result<T> NotValid<T>(string message, params IEnumerable<ValidationFailure> failures) {
			return Result<T>.Fail(new ValidationException(message, failures));
		}

		/// <summary>
		/// Creates a failed result indicating that validation failed with the specified message and validation failures.
		/// </summary>
		/// <param name="message">The error message describing the validation failure.</param>
		/// <param name="failures">The collection of validation failures that describe what failed validation.</param>
		/// <returns>A failed <see cref="Result"/> containing a <see cref="ValidationException"/> with the specified message and failures.</returns>
		public static Result NotValid(string message, params IEnumerable<ValidationFailure> failures) {
			return Result.Fail(new ValidationException(message, failures));
		}

	}

}