namespace Cirreum.Authorization.Operations;

using Cirreum.Conductor;

/// <summary>
/// Marker for an authorized request with no response (void).
/// </summary>
/// <remarks>
/// Implements <see cref="IAuthorizableRequestBase"/> for authorization pipeline participation
/// and <see cref="IRequest"/> for conductor routing.
/// For grant-aware (grant-based) operations, use the grant interfaces:
/// <see cref="Operations.Grants.IGrantMutateRequest"/>,
/// <see cref="Operations.Grants.IGrantLookupRequest{TResponse}"/>,
/// <see cref="Operations.Grants.IGrantSearchRequest{TResponse}"/>,
/// <see cref="Operations.Grants.IGrantMutateSelfRequest"/>, or
/// <see cref="Operations.Grants.IGrantLookupSelfRequest{TResponse}"/>.
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
/// <see cref="Operations.Grants.IGrantMutateRequest{TResponse}"/>,
/// <see cref="Operations.Grants.IGrantLookupRequest{TResponse}"/>,
/// <see cref="Operations.Grants.IGrantSearchRequest{TResponse}"/>,
/// <see cref="Operations.Grants.IGrantMutateSelfRequest{TResponse}"/>, or
/// <see cref="Operations.Grants.IGrantLookupSelfRequest{TResponse}"/>.
/// </remarks>
public interface IAuthorizableRequest<out TResponse> : IAuthorizableRequestBase, IRequest<TResponse>;
