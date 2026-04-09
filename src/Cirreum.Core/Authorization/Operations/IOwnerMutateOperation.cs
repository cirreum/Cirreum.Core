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
/// Grant-aware mutation with a response. Composes foundation <see cref="IAuthorizableOperation{TResultValue}"/>
/// with the <see cref="IGrantableMutateBase"/> detection surface. Developers implement this
/// single interface for granted mutations that return a value.
/// </summary>
/// <typeparam name="TResultValue">The type of response returned by the mutation.</typeparam>
public interface IOwnerMutateOperation<out TResultValue>
	: IAuthorizableOperation<TResultValue>, IGrantableMutateBase;
