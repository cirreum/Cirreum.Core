namespace Cirreum.Exceptions;

using Humanizer;
using System;

/// <summary>
/// The application NotFound exception.
/// </summary>
/// <remarks>
/// Constructs a new instance of the exception.
/// </remarks>
/// <param name="keys"></param>
public class NotFoundException(params object[] keys) : Exception(GetMessage(keys)) {

	static string GetMessage(object[] keys) {

		if (keys.Length == 0) {
			return "Item was not found.";
		}

		if (keys.Length == 1) {
			return $"Item {keys[0]} was not found.";
		}

		return $"Items ({keys.Humanize()}) were not found.";

	}

}