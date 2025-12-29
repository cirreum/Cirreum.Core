namespace Cirreum.Conductor;

using Cirreum.Authorization;

// ===== Domain Requests =====

/// <summary>
/// Represents a domain command that is both auditable and authorizable.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface for commands that modify state and require both authorization
/// checks and audit trail capture. This is the recommended base for most write operations.
/// </para>
/// <para>
/// Implement a corresponding <see cref="AuthorizationValidatorBase{TResource}"/> to define
/// the authorization rules for this command.
/// </para>
/// </remarks>
public interface IDomainCommand : IAuditableRequest, IAuthorizableRequest;

/// <summary>
/// Represents a domain command with a response that is both auditable and authorizable.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
/// <remarks>
/// <para>
/// Use this interface for commands that modify state, return a result, and require both
/// authorization checks and audit trail capture.
/// </para>
/// <para>
/// Implement a corresponding <see cref="AuthorizationValidatorBase{TResource}"/> to define
/// the authorization rules for this command.
/// </para>
/// </remarks>
public interface IDomainCommand<out TResponse> : IAuditableRequest<TResponse>, IAuthorizableRequest<TResponse>;

/// <summary>
/// Represents a domain query that is both auditable and authorizable.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
/// <remarks>
/// <para>
/// Use this interface for queries that retrieve data and require both authorization
/// checks and audit trail capture. Suitable for sensitive read operations where
/// access should be logged.
/// </para>
/// <para>
/// For queries that benefit from caching, consider using <see cref="IDomainCacheableQuery{TResponse}"/> instead.
/// </para>
/// </remarks>
public interface IDomainQuery<TResponse> : IAuditableRequest<TResponse>, IAuthorizableRequest<TResponse>;

/// <summary>
/// Represents a domain query that is auditable, authorizable, and cacheable.
/// </summary>
/// <typeparam name="TResponse">
/// The type of response returned by the query. Must be immutable for safe caching
/// with instance reuse. Use sealed records with init-only properties.
/// </typeparam>
/// <remarks>
/// <para>
/// Use this interface for frequently executed queries where caching improves performance
/// while still maintaining authorization checks and audit trail capture.
/// </para>
/// <para>
/// Implementers must provide a <see cref="ICacheableQuery{TResponse}.CacheKey"/> and may
/// optionally configure <see cref="ICacheableQuery{TResponse}.Cache"/>,
/// <see cref="ICacheableQuery{TResponse}.CacheTags"/>, and
/// <see cref="ICacheableQuery{TResponse}.CacheCategory"/>.
/// </para>
/// </remarks>
public interface IDomainCacheableQuery<TResponse>
	: IAuditableRequest<TResponse>,
	IAuthorizableRequest<TResponse>,
	ICacheableQuery<TResponse>;