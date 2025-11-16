namespace Cirreum.Conductor;

using Cirreum.Authorization;

// ===== Authorizable Requests =====

/// <summary>
/// Marker interface that represents an authorizable <see cref="IRequest"/> instance.
/// </summary>
public interface IAuthorizableRequest : IRequest, IAuthorizableRequestBase;

/// <summary>
/// Marker interface that represents an authorizable <see cref="IRequest{TResponse}"/> instance.
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IAuthorizableRequest<out TResponse> : IRequest<TResponse>, IAuthorizableRequestBase;

/// <summary>
/// Marker interface to allow requests to be treated as a resource, and participate in
/// the authorization pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Normally you would not implement this interface directly.
/// </para>
/// <para>
/// See <see cref="IAuthorizableRequest"/> and <see cref="IAuthorizableRequest{TResponse}"/>
/// for the interfaces that should be implemented.
/// </para>
/// </remarks>
public interface IAuthorizableRequestBase : IBaseRequest, IAuthorizableResource;