namespace Cirreum.Authorization.Operations;

using Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Grant-aware mutation with no response. Composes foundation <see cref="IAuthorizableOperation"/>
/// with the <see cref="IGrantableMutateBase"/> detection surface. Developers implement this
/// single interface for void granted mutations.
/// </summary>
public interface IOwnerMutateOperation
	: IAuthorizableOperation, IGrantableMutateBase;

/// <summary>
/// Grant-aware mutation with a response. Composes foundation <see cref="IAuthorizableOperation{TResponse}"/>
/// with the <see cref="IGrantableMutateBase"/> detection surface. Developers implement this
/// single interface for granted mutations that return a value.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the mutation.</typeparam>
public interface IOwnerMutateOperation<out TResponse>
	: IAuthorizableOperation<TResponse>, IGrantableMutateBase;
