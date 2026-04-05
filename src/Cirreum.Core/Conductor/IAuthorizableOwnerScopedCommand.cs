namespace Cirreum.Conductor;

using Cirreum.Authorization;

/// <summary>
/// Owner-scoped authorized write operation (command) with no response.
/// OwnerId rules are enforced based on the caller's <see cref="Security.AccessScope"/>.
/// </summary>
public interface IAuthorizableOwnerScopedCommand
	: IAuthorizableOwnerScopedResource, IAuthorizableCommand;

/// <summary>
/// Owner-scoped authorized write operation (command) with a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public interface IAuthorizableOwnerScopedCommand<out TResponse>
	: IAuthorizableOwnerScopedResource, IAuthorizableCommand<TResponse>;
