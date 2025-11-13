namespace Cirreum.Conductor;

using Cirreum.Conductor.Intercepts;

// ===== Requests =====

/// <summary>
/// Marker interface to represent a request with a void response
/// </summary>
public interface IRequest : IBaseRequest { }

/// <summary>
/// Marker interface to represent a request with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<out TResponse> : IBaseRequest { }

/// <summary>
/// Allows for generic type constraints of objects implementing IRequest or IRequest{TResponse}
/// </summary>
public interface IBaseRequest { }


// ===== Commands =====

/// <summary>
/// Represents a command in the CQRS (Command Query Responsibility Segregation) pattern  that is used to mutate
/// application state.
/// </summary>
/// <remarks>
/// Commands are typically used to encapsulate all the information needed to perform an action  or
/// trigger a state change. Implementations of this interface are expected to define the specific behavior of the
/// command.
/// </remarks>
public interface ICommand : IRequest;
/// <summary>
/// Represents a command that produces a response of the specified type.
/// </summary>
/// <typeparam name="TResponse">The type of the response produced by the command.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;


// ===== Queries =====

/// <summary>
/// Represents a query that produces a response of the specified type.
/// </summary>
/// <typeparam name="TResponse">The type of the response produced by the query.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Represents a query that can be cached.
/// </summary>
/// <typeparam name="TResponse">
/// The response type. Must be immutable for safe caching with instance reuse.
/// Use sealed records with init-only properties and mark with [ImmutableObject(true)].
/// </typeparam>
public interface ICacheableQuery<out TResponse> : IQuery<TResponse> {

	/// <summary>
	/// The unique cache key for this query instance.
	/// </summary>
	string CacheKey { get; }

	/// <summary>
	/// Cache expiration settings for this query. Values specified here can be overridden by
	/// configuration at runtime. If not specified, uses global defaults.
	/// </summary>
	QueryCacheSettings Cache => new();

	/// <summary>
	/// Tags for cache invalidation. Cannot be overridden by configuration.
	/// Tags enable bulk invalidation of related cache entries and define the
	/// domain relationships of the cached data.
	/// </summary>
	/// <example>
	/// ["users", $"user:{UserId}"] - Can invalidate all users or specific user
	/// ["orders", "tenant:123"] - Can invalidate by entity type or tenant
	/// </example>
	string[]? CacheTags => null;

	/// <summary>
	/// Cache category for configuration grouping. This allows DevOps to configure
	/// cache settings for groups of related queries without knowing specific query names.
	/// </summary>
	/// <remarks>
	/// Common categories: "users", "orders", "reports", "analytics", "reference-data", "real-time"
	/// If null, only uses default settings or exact query name overrides.
	/// </remarks>
	string? CacheCategory => null;
}