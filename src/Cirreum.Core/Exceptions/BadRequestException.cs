namespace Cirreum.Exceptions;

using System;

/// <summary>
/// An exception thrown when the request is malformed or otherwise unable to be parsed or processed.
/// </summary>
public class BadRequestException : Exception {

	/// <summary>
	/// Default constructor.
	/// </summary>
	public BadRequestException() {
	}

	/// <summary>
	/// Construct a new instance using the specified message.
	/// </summary>
	/// <param name="message">The message describing the exception.</param>
	public BadRequestException(string message)
		: base(message) {
	}

}