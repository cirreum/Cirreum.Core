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
/// request marker interfaces instead:
/// <see cref="IAuthorizableRequest"/> or <see cref="IAuthorizableRequest{TResponse}"/>.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="Authorization.Grants.IGrantMutateRequest"/>,
/// <see cref="Authorization.Grants.IGrantLookupRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantSearchRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantMutateSelfRequest"/>, or
/// <see cref="Authorization.Grants.IGrantLookupSelfRequest{TResponse}"/>.
/// </para>
/// <para>
/// By inheriting <see cref="IAuthorizableObject"/>, every request instance may be
/// treated as its own authorizable resource — validators can inspect request shape
/// directly without wrapping.
/// </para>
/// </remarks>
public interface IAuthorizableRequestBase : IBaseRequest, IAuthorizableObject;