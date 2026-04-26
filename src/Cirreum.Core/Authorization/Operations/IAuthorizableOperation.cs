namespace Cirreum.Authorization.Operations;

using Cirreum.Conductor;

/// <summary>
/// A void-returning operation that flows through the authorization pipeline before its
/// handler runs. The minimal "this operation must be authorized" declaration —
/// composes <see cref="IOperation"/> (Conductor routing) with
/// <see cref="IAuthorizableOperationBase"/> (Authorization participation).
/// </summary>
/// <remarks>
/// <para>
/// Use this when an operation needs role-based, claim-based, or constraint-based
/// authorization but does not need owner-scoped grant resolution or self-identity
/// matching. For those, use the grant interfaces:
/// <see cref="IOwnerMutateOperation"/>,
/// <see cref="IOwnerLookupOperation{TResultValue}"/>,
/// <see cref="IOwnerSearchOperation{TResultValue}"/>,
/// <see cref="ISelfMutateOperation"/>, or
/// <see cref="ISelfLookupOperation{TResultValue}"/> — all of which transitively
/// implement <see cref="IAuthorizableOperation"/>.
/// </para>
/// </remarks>
public interface IAuthorizableOperation : IAuthorizableOperationBase, IOperation;

/// <summary>
/// An operation that returns <typeparamref name="TResultValue"/> and flows through the
/// authorization pipeline before its handler runs. Composes
/// <see cref="IOperation{TResultValue}"/> (Conductor routing) with
/// <see cref="IAuthorizableOperationBase"/> (Authorization participation).
/// </summary>
/// <typeparam name="TResultValue">The value produced on success.</typeparam>
/// <remarks>
/// <para>
/// Use this when an operation returns a value and needs role-based, claim-based, or
/// constraint-based authorization but does not need owner-scoped grant resolution or
/// self-identity matching. For those, use the grant interfaces:
/// <see cref="IOwnerMutateOperation{TResultValue}"/>,
/// <see cref="IOwnerLookupOperation{TResultValue}"/>,
/// <see cref="IOwnerSearchOperation{TResultValue}"/>,
/// <see cref="ISelfMutateOperation{TResultValue}"/>, or
/// <see cref="ISelfLookupOperation{TResultValue}"/> — all of which transitively
/// implement <see cref="IAuthorizableOperation{TResultValue}"/>.
/// </para>
/// </remarks>
public interface IAuthorizableOperation<out TResultValue> : IAuthorizableOperationBase, IOperation<TResultValue>;
