namespace Cirreum.Authorization.Operations;

using Cirreum.Conductor;

/// <summary>
/// Marker for an authorized request with no response (void).
/// </summary>
/// <remarks>
/// Implements <see cref="IAuthorizableOperationBase"/> for authorization pipeline participation
/// and <see cref="IOperation"/> for conductor routing.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="IOwnerMutateOperation"/>,
/// <see cref="IOwnerLookupOperation{TResultValue}"/>,
/// <see cref="IOwnerSearchOperation{TResultValue}"/>,
/// <see cref="ISelfMutateOperation"/>, or
/// <see cref="ISelfLookupOperation{TResultValue}"/>.
/// </remarks>
public interface IAuthorizableOperation : IAuthorizableOperationBase, IOperation;

/// <summary>
/// Marker for an authorized request with a response.
/// </summary>
/// <typeparam name="TResultValue">The type of response returned by the request.</typeparam>
/// <remarks>
/// Implements <see cref="IAuthorizableOperationBase"/> for authorization pipeline participation
/// and <see cref="IOperation{TResultValue}"/> for conductor routing.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="IOwnerMutateOperation{TResultValue}"/>,
/// <see cref="IOwnerLookupOperation{TResultValue}"/>,
/// <see cref="IOwnerSearchOperation{TResultValue}"/>,
/// <see cref="ISelfMutateOperation{TResultValue}"/>, or
/// <see cref="ISelfLookupOperation{TResultValue}"/>.
/// </remarks>
public interface IAuthorizableOperation<out TResultValue> : IAuthorizableOperationBase, IOperation<TResultValue>;