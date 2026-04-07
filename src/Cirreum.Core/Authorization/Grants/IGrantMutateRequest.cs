namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Grant-aware mutation with no response. Composes foundation <see cref="IAuthorizableRequest"/>
/// with the <see cref="IGrantableMutateBase"/> detection surface. Developers implement this
/// single interface for void granted mutations.
/// </summary>
public interface IGrantMutateRequest
	: IAuthorizableRequest, IGrantableMutateBase;

/// <summary>
/// Grant-aware mutation with a response. Composes foundation <see cref="IAuthorizableRequest{TResponse}"/>
/// with the <see cref="IGrantableMutateBase"/> detection surface. Developers implement this
/// single interface for granted mutations that return a value.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the mutation.</typeparam>
public interface IGrantMutateRequest<out TResponse>
	: IAuthorizableRequest<TResponse>, IGrantableMutateBase;
