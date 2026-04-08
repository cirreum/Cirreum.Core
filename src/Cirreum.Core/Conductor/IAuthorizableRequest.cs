namespace Cirreum.Conductor;

/// <summary>
/// Marker for an authorized request with no response (void).
/// </summary>
/// <remarks>
/// Implements <see cref="IAuthorizableRequestBase"/> for authorization pipeline participation
/// and <see cref="IRequest"/> for conductor routing.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="Authorization.Grants.IGrantMutateRequest"/>,
/// <see cref="Authorization.Grants.IGrantLookupRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantSearchRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantMutateSelfRequest"/>, or
/// <see cref="Authorization.Grants.IGrantLookupSelfRequest{TResponse}"/>.
/// </remarks>
public interface IAuthorizableRequest : IAuthorizableRequestBase, IRequest;

/// <summary>
/// Marker for an authorized request with a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the request.</typeparam>
/// <remarks>
/// Implements <see cref="IAuthorizableRequestBase"/> for authorization pipeline participation
/// and <see cref="IRequest{TResponse}"/> for conductor routing.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="Authorization.Grants.IGrantMutateRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantLookupRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantSearchRequest{TResponse}"/>,
/// <see cref="Authorization.Grants.IGrantMutateSelfRequest{TResponse}"/>, or
/// <see cref="Authorization.Grants.IGrantLookupSelfRequest{TResponse}"/>.
/// </remarks>
public interface IAuthorizableRequest<out TResponse> : IAuthorizableRequestBase, IRequest<TResponse>;
