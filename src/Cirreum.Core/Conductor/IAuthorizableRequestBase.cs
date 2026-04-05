namespace Cirreum.Conductor;

using Cirreum.Authorization;

/// <summary>
/// The single pipeline discriminator for authorization. Any request that participates
/// in the authorization pipeline — commands, queries, cacheable queries, and their
/// owner-scoped variants — inherits from this interface.
/// </summary>
/// <remarks>
/// <para>
/// Normally you would not implement this interface directly. Implement one of the
/// CQRS-facing marker interfaces instead:
/// <see cref="IAuthorizableCommand"/>, <see cref="IAuthorizableCommand{TResponse}"/>,
/// <see cref="IAuthorizableQuery{TResponse}"/>,
/// <see cref="IAuthorizableCacheableQuery{TResponse}"/>,
/// <see cref="IAuthorizableOwnerScopedCommand"/>,
/// <see cref="IAuthorizableOwnerScopedCommand{TResponse}"/>, or
/// <see cref="IAuthorizableOwnerScopedQuery{TResponse}"/>.
/// </para>
/// <para>
/// By inheriting <see cref="IAuthorizableResource"/>, every request instance may be
/// treated as its own authorizable resource — validators can inspect request shape
/// directly without wrapping.
/// </para>
/// </remarks>
public interface IAuthorizableRequestBase : IBaseRequest, IAuthorizableResource;
