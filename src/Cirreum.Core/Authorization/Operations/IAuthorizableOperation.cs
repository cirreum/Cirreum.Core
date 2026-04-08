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
/// <see cref="IOwnerLookupOperation{TResponse}"/>,
/// <see cref="IOwnerSearchOperation{TResponse}"/>,
/// <see cref="ISelfMutateOperation"/>, or
/// <see cref="ISelfLookupOperation{TResponse}"/>.
/// </remarks>
public interface IAuthorizableOperation : IAuthorizableOperationBase, IOperation;

/// <summary>
/// Marker for an authorized request with a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
/// <remarks>
/// Implements <see cref="IAuthorizableOperationBase"/> for authorization pipeline participation
/// and <see cref="IOperation{TResponse}"/> for conductor routing.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="IOwnerMutateOperation{TResponse}"/>,
/// <see cref="IOwnerLookupOperation{TResponse}"/>,
/// <see cref="IOwnerSearchOperation{TResponse}"/>,
/// <see cref="ISelfMutateOperation{TResponse}"/>, or
/// <see cref="ISelfLookupOperation{TResponse}"/>.
/// </remarks>
public interface IAuthorizableOperation<out TResponse> : IAuthorizableOperationBase, IOperation<TResponse>;