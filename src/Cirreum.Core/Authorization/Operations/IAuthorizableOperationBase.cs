namespace Cirreum.Authorization.Operations;

using Cirreum.Conductor;

/// <summary>
/// The single pipeline discriminator for authorization. Any object that participates
/// in the authorization pipeline — commands, queries, cacheable queries, and their
/// owner-scoped variants — inherits from this interface.
/// </summary>
/// <remarks>
/// <para>
/// Normally you would not implement this interface directly. Implement one of the
/// operational marker interfaces instead:
/// <see cref="IAuthorizableOperation"/> or <see cref="IAuthorizableOperation{TResponse}"/>.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="IOwnerMutateOperation"/>,
/// <see cref="IOwnerLookupOperation{TResponse}"/>,
/// <see cref="IOwnerSearchOperation{TResponse}"/>,
/// <see cref="ISelfMutateOperation"/>, or
/// <see cref="ISelfLookupOperation{TResponse}"/>.
/// </para>
/// <para>
/// By inheriting <see cref="IAuthorizableObject"/>, every instance may be
/// treated as its own authorizable object — authorizers can inspect shape
/// directly without wrapping.
/// </para>
/// </remarks>
public interface IAuthorizableOperationBase : IBaseOperation, IAuthorizableObject;