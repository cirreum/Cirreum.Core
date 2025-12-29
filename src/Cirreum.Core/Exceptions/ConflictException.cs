namespace Cirreum.Exceptions;

/// <summary>
/// The application Conflict exception.
/// </summary>
/// <remarks>
/// Constructs a new instance of the exception.
/// </remarks>
/// <param name="message">The message describing the conflict.</param>
public class ConflictException(string message) : Exception(message);