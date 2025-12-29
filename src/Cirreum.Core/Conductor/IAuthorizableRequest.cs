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
/// Represents a cacheable query that requires authorization but does not generate audit records.
/// </summary>
/// <typeparam name="TResponse">
/// The type of response returned by the query. Must be immutable for safe caching
/// with instance reuse. Use sealed records with init-only properties.
/// </typeparam>
/// <remarks>
/// <para>
/// Use this interface for frequently executed queries where authorization is required
/// but audit logging would create excessive noise. Ideal for reference data lookups,
/// configuration queries, and other high-frequency read operations.
/// </para>
/// <para>
/// For queries that require both authorization and audit trail capture, use 
/// <see cref="IDomainCacheableQuery{TResponse}"/> instead.
/// </para>
/// </remarks>
public interface IAuthorizableCacheableQuery<TResponse>
	: IAuthorizableRequest<TResponse>,
	ICacheableQuery<TResponse>;

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