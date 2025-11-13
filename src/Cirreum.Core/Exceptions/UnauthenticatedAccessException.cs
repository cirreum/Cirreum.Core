namespace Cirreum.Exceptions;
/// <summary>
/// Exception thrown when an operation fails due to an unauthenticated user.
/// </summary>
/// <param name="message">The reason for the exception.</param>
public class UnauthenticatedAccessException(string message) : Exception(message);