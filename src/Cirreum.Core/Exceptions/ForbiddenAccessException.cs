namespace Cirreum.Exceptions;
/// <summary>
/// Exception thrown when an operation fails authorization.
/// </summary>
/// <param name="message">The reason for the exception.</param>
public class ForbiddenAccessException(string message) : Exception(message);