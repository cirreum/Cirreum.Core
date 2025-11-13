namespace Cirreum.Exceptions;

using System;

/// <summary>
/// A Conflict exception thrown when an item already exists.
/// </summary>
public class AlreadyExistsException : Exception {

	/// <summary>
	/// A Conflict exception thrown when an item already exists.
	/// </summary>
	public AlreadyExistsException() {
	}

	/// <summary>
	/// A Conflict exception thrown when an item already exists.
	/// </summary>
	/// <param name="message">The message describing the exception.</param>
	public AlreadyExistsException(string message)
		: base(message) {
	}

	/// <summary>
	/// A Conflict exception thrown when an item already exists.
	/// </summary>
	/// <param name="message">The message describing the exception.</param>
	/// <param name="inner">The exception that is the cause of the current exception.</param>
	public AlreadyExistsException(string message, Exception inner)
		: base(message, inner) {
	}

}