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
public class NotFoundException(
	params ReadOnlySpan<object?> keys
) : Exception(GetMessage(keys)) {
	static string GetMessage(ReadOnlySpan<object?> keys) => keys.Length switch {
		0 => "Item was not found.",
		1 => $"Item {keys[0]} was not found.",
		2 => $"Items ({keys[0]} and {keys[1]}) were not found.",
		_ => $"Items ({keys.ToArray().Humanize()}) were not found."
	};
}