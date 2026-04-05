namespace Cirreum.Conductor;

/// <summary>
/// Marker for an authorized write operation (command) with no response.
/// </summary>
public interface IAuthorizableCommand : IAuthorizableRequestBase, IRequest;

/// <summary>
/// Marker for an authorized write operation (command) with a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public interface IAuthorizableCommand<out TResponse> : IAuthorizableRequestBase, IRequest<TResponse>;
