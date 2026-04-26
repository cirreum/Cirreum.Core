namespace Cirreum.Conductor;

using Cirreum.Caching;

/// <summary>
/// Non-generic marker for cacheable operations. Allows runtime detection of cacheability
/// without knowing the result type (e.g., in the grant evaluator's lookup branch).
/// </summary>
public interface ICacheableOperation;

/// <summary>
/// An operation whose result is cached by the framework's caching intercept. The cached
/// value is keyed by <see cref="CacheKey"/>, scoped by <see cref="CacheTags"/> for bulk
/// invalidation, and shared across all callers — caching is per-operation, not per-caller.
/// </summary>
/// <typeparam name="TResultValue">
/// The result value. Must be **immutable** for safe cache instance reuse — the same
/// instance is handed to every caller of a cache hit. Use sealed records with init-only
/// properties; mark with <c>[ImmutableObject(true)]</c> when contributors might miss the
/// constraint.
/// </typeparam>
/// <remarks>
/// <para>
/// Because the cached value is shared, <see cref="CacheKey"/> must capture every input
/// that affects the result — including any owner/tenant scope. Two callers resolving the
/// same key must always be safe to receive the same value. Do <em>not</em> mix
/// <see cref="ICacheableOperation{TResultValue}"/> with per-caller authorization decisions
/// (e.g., ACL filtering) — that turns shared cache entries into leaks.
/// </para>
/// <para>
/// For owner-scoped cacheable lookups in a multi-tenant grant pipeline, use
/// <see cref="Authorization.Operations.IOwnerCacheableLookupOperation{TResultValue}"/>,
/// which automatically composes <c>OwnerId</c> and authentication boundary into the
/// effective key and adds a <c>owner:{OwnerId}</c> invalidation tag.
/// </para>
/// </remarks>
public interface ICacheableOperation<out TResultValue> : ICacheableOperation, IOperation<TResultValue> {

	/// <summary>
	/// Unique cache key for this operation instance. Must capture every input that affects
	/// the result; entries with the same key are treated as identical regardless of caller.
	/// </summary>
	string CacheKey { get; }

	/// <summary>
	/// Expiration policy for this operation's cache entries. Values specified here are
	/// code-level defaults; configuration can override (per-operation type, category-wide).
	/// Defaults to <see cref="CacheSettings.DefaultExpiration"/> when unspecified.
	/// </summary>
	CacheExpirationSettings CacheExpiration => new();

	/// <summary>
	/// Tags for bulk cache invalidation. Cannot be overridden by configuration. Tags
	/// declare the domain relationships of the cached value so that related entries
	/// can be evicted together (e.g., on entity update).
	/// </summary>
	/// <example>
	/// <c>["users", $"user:{UserId}"]</c> — invalidate all users or one specific user.<br/>
	/// <c>["orders", "tenant:123"]</c> — invalidate by entity type or by tenant.
	/// </example>
	string[]? CacheTags => null;

}
