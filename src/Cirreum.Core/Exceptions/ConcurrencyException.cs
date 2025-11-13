namespace Cirreum.Exceptions;

using System;

/// <summary>
/// An exception thrown when an entity being updated is stale.
/// </summary>
public class ConcurrencyException : Exception {

	/// <summary>
	/// Default constructor.
	/// </summary>
	public ConcurrencyException() {
	}

	/// <summary>
	/// Construct a new instance using the specified message.
	/// </summary>
	/// <param name="entityId">The id of the entity that had the error.</param>
	public ConcurrencyException(string entityId)
		: base($"Item '{entityId}' has changed and could not be modified.") {
	}

}