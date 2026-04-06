namespace Cirreum.Conductor;

/// <summary>
/// Marker for an authorized read operation (query).
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
public interface IAuthorizableQuery<out TResponse> : IAuthorizableRequestBase, IRequest<TResponse>;

/// <summary>
/// Marker for an authorized, cacheable read operation.
/// </summary>
/// <typeparam name="TResponse">
/// The type of response returned by the query. Must be immutable for safe caching
/// with instance reuse. Use sealed records with init-only properties.
/// </typeparam>
public interface IAuthorizableCacheableQuery<TResponse>
	: IAuthorizableQuery<TResponse>, ICacheableQuery<TResponse>;