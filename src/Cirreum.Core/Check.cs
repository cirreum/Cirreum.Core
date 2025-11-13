namespace Cirreum;

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides a set of runtime validations for inputs.
/// </summary>
[System.Diagnostics.DebuggerNonUserCode]
public static class Check {

	/// <summary>
	/// Verifies that the condition is true and if it fails constructs the specified type of
	/// exception with any arguments provided and throws it.
	/// </summary>
	/// <typeparam name="TException">The type of exception to throw if the condition is false.</typeparam>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <exception cref="Exception">
	/// Thrown as <typeparamref name="TException"/> when the condition is false.
	/// </exception>
	/// <remarks>
	/// <para>
	/// The type of exception thrown is determined by the generic type parameter <typeparamref name="TException"/>.
	/// </para>
	/// </remarks>
	public static void Assert<TException>(
		bool condition,
		string? message = null,
		[CallerArgumentExpression(nameof(condition))] string? paramName = null)
		where TException : Exception, new() {
		if (condition is false) {
			var ci = typeof(TException).GetConstructor([typeof(string)]);
			if (ci != null) {
				var e = (TException)ci.Invoke([message ?? $"Assertion failed: {paramName}"]);
				throw e;
			}
			throw new TException();
		}
	}


	/// <summary>
	/// Used to delay creation of the exception until the condition fails.
	/// </summary>
	/// <returns>An exception to be thrown when the condition fails.</returns>
	public delegate Exception ExceptionBuilder();

	/// <summary>
	/// Verifies that the condition is true and if it fails throws the exception returned
	/// by <paramref name="fnExceptionBuilder"/>.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="fnExceptionBuilder">A function that creates the exception to throw when the condition is false.</param>
	/// <exception cref="Exception">Thrown when the condition is false (type determined by fnExceptionBuilder).</exception>
	public static void Assert(bool condition, ExceptionBuilder fnExceptionBuilder) {
		if (condition is false) {
			throw fnExceptionBuilder();
		}
	}

	/// <summary>
	/// Verifies that the condition is true and if it fails constructs the specified type of
	/// exception with the provided message and inner exception, then throws it.
	/// </summary>
	/// <typeparam name="TException">The type of exception to throw if the condition is false.</typeparam>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="message">The message for the exception.</param>
	/// <param name="innerException">The inner exception to include in the thrown exception.</param>
	/// <exception cref="Exception">
	/// Thrown as <typeparamref name="TException"/> when the condition is false.
	/// </exception>
	/// <remarks>
	/// <para>
	/// The type of exception thrown is determined by the generic type parameter <typeparamref name="TException"/>.
	/// </para>
	/// </remarks>
	public static void Assert<TException>(bool condition, string message, Exception innerException)
		where TException : Exception, new() {

		if (!condition) {

			var ci = typeof(TException).GetConstructor([typeof(string), typeof(Exception)]);
			if (ci != null) {
				var e = (TException)ci.Invoke([message, innerException]);
				throw e;
			}

			throw new TException();

		}

	}

	/// <summary>
	/// Verifies that value is not null and returns the value.
	/// </summary>
	/// <typeparam name="T">The type of the value to check.</typeparam>
	/// <param name="value">The object to evaluate for <see langword="null"/>.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument that was <see langword="null"/>.</param>
	/// <returns>The non-null value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	[return: NotNull]
	public static T NotNull<T>(
		[NotNull] T? value,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null) {
		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}
		return value;
	}

	/// <summary>
	/// Verifies that the string is not null and not empty and returns the string.
	/// </summary>
	/// <param name="value">The string to evaluate.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The non-empty string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is empty.</exception>
	[return: NotNull]
	public static string NotEmpty(
		[NotNull] string? value,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null) {
		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}
		if (value.Length == 0) {
			throw new ArgumentOutOfRangeException(paramName, message);
		}
		return value;
	}

	/// <summary>
	/// Verifies that the string is not null, not empty, and does not consist only of white-space characters.
	/// </summary>
	/// <param name="value">The string to evaluate.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The non-whitespace string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or consists only of whitespace.</exception>
	[return: NotNull]
	public static string NotWhiteSpace(
		[NotNull] string? value,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null) {
		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}
		if (string.IsNullOrWhiteSpace(value)) {
			throw new ArgumentException(message, paramName);
		}
		return value;
	}

	/// <summary>
	/// Verifies that the Guid is not empty.
	/// </summary>
	/// <param name="value">The Guid to evaluate.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The non-empty Guid.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
	public static Guid NotEmpty(
		Guid value,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null) {
		if (value == Guid.Empty) {
			throw new ArgumentOutOfRangeException(paramName, message);
		}

		return value;
	}

	/// <summary>
	/// Verifies that the collection is not null and not empty and returns the collection.
	/// </summary>
	/// <typeparam name="T">The type of the collection.</typeparam>
	/// <param name="value">The collection to evaluate.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The non-empty collection.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is empty.</exception>
	[return: NotNull]
	public static T NotEmpty<T>(
		[NotNull] T? value,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null)
		where T : IEnumerable {

		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}

		// If it's an ICollection, use Count
		if (value is ICollection collection) {
			if (collection.Count > 0) {
				return value; // <-- Early return: we know it's not empty
			}
			throw new ArgumentOutOfRangeException(paramName, message);
		}

		// Otherwise, check enumerator
		var enumerator = value.GetEnumerator();
		if (enumerator.MoveNext()) {
			return value; // <-- Early return: first item exists
		}

		throw new ArgumentOutOfRangeException(paramName, message);
	}


	/// <summary>
	/// Verifies that the array has at least <paramref name="min"/> items, but not more than <paramref name="max"/> items.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="value">The array to evaluate.</param>
	/// <param name="min">The minimum allowed length.</param>
	/// <param name="max">The maximum allowed length.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The array with valid size.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the array length is outside the specified range.</exception>
	[return: NotNull]
	public static T[] ArraySize<T>(
		[NotNull] T[]? value,
		int min,
		int max,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null) {
		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}

		if (value.Length < min || value.Length > max) {
			throw new ArgumentOutOfRangeException(paramName, message ?? $"Array length must be between {min} and {max}");
		}

		return value;
	}

	/// <summary>
	/// Verifies that the two values are equal using the type's Equals method.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="a">The first value to compare.</param>
	/// <param name="b">The second value to compare.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramNameA">The optional name if you want to use something different than that of the first argument.</param>
	/// <param name="paramNameB">The optional name if you want to use something different than that of the second argument.</param>
	/// <exception cref="ArgumentException">Thrown when the values are not equal.</exception>
	public static void IsEqual<T>(
		T a,
		T b,
		string? message = null,
		[CallerArgumentExpression(nameof(a))] string? paramNameA = null,
		[CallerArgumentExpression(nameof(b))] string? paramNameB = null)
		where T : IEquatable<T> {
		if (a.Equals(b) is false) {
			throw new ArgumentException(
				message ?? $"Values must be equal: {paramNameA} != {paramNameB}");
		}
	}

	/// <summary>
	/// Verifies that the two values are not equal using the type's Equals method.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="a">The first value to compare.</param>
	/// <param name="b">The second value to compare.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramNameA">The optional name if you want to use something different than that of the first argument.</param>
	/// <param name="paramNameB">The optional name if you want to use something different than that of the second argument.</param>
	/// <exception cref="ArgumentException">Thrown when the values are equal.</exception>
	public static void NotEqual<T>(
		T a,
		T b,
		string? message = null,
		[CallerArgumentExpression(nameof(a))] string? paramNameA = null,
		[CallerArgumentExpression(nameof(b))] string? paramNameB = null)
		where T : IEquatable<T> {
		if (a.Equals(b) is true) {
			throw new ArgumentException(
				message ?? $"Values must not be equal: {paramNameA} == {paramNameB}");
		}
	}

	/// <summary>
	/// Verifies that the value is greater than or equal to <paramref name="min"/> and less than or equal to <paramref name="max"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value to check.</typeparam>
	/// <param name="value">The value to evaluate.</param>
	/// <param name="min">The minimum allowed value (inclusive).</param>
	/// <param name="max">The maximum allowed value (inclusive).</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The value if it's within the specified range.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the allowed range.</exception>
	[return: NotNull]
	public static T InRange<T>(
		[NotNull] T? value,
		T min,
		T max,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null)
		where T : IComparable<T> {
		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}

		if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0) {
			throw new ArgumentOutOfRangeException(
				paramName,
				message ?? $"Value must be between {min} and {max}");
		}

		return value;
	}


	/// <summary>
	/// Verifies that the provided value can be assigned to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to check assignability to.</typeparam>
	/// <param name="value">The value to check for assignability.</param>
	/// <param name="message">The optional message articulating a friendly explanation.</param>
	/// <param name="paramName">The optional name if you want to use something different than that of the argument.</param>
	/// <returns>The value cast to type T if assignable.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> cannot be assigned to type T.</exception>
	public static T IsAssignable<T>(
		object value,
		string? message = null,
		[CallerArgumentExpression(nameof(value))] string? paramName = null) {
		if (value == null) {
			throw new ArgumentNullException(paramName, message);
		}

		try {
			return (T)IsAssignable(typeof(T), value);
		} catch (Exception ex) {
			throw new ArgumentException(
				message ?? $"Cannot assign {paramName} of type {value.GetType()} to type {typeof(T)}",
				paramName,
				ex);
		}
	}

	/// <summary>
	/// Returns <paramref name="fromValue"/> if <paramref name="fromValue"/> can be assigned to a variable
	/// of type <paramref name="toType"/>; otherwise throws <see cref="ArgumentException"/>.
	/// </summary>
	/// <param name="toType">The target type to check assignability to.</param>
	/// <param name="fromValue">The value to check for assignability.</param>
	/// <returns>The original value if assignable.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="fromValue"/> cannot be assigned to <paramref name="toType"/>.</exception>
	public static object IsAssignable(Type toType, object fromValue) {
		IsAssignable(toType, fromValue.GetType());
		return fromValue;
	}

	/// <summary>
	/// Verifies that <paramref name="fromType"/> can be assigned to a variable of type <paramref name="toType"/>.
	/// </summary>
	/// <param name="toType">The target type to check assignability to.</param>
	/// <param name="fromType">The source type to check for assignability.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="fromType"/> cannot be assigned to <paramref name="toType"/>.</exception>
	public static void IsAssignable(Type toType, Type fromType) {
		if (toType.IsAssignableFrom(fromType) is false) {
			throw new ArgumentException(string.Format("Can not set value of type {0} to a value of type {1}.",
				toType,
				fromType));
		}
	}

}