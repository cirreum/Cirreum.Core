namespace Cirreum.Conductor;

using Cirreum.Authorization;
using Cirreum.Security;

/// <summary>
/// Non-generic marker for owner-scoped cacheable queries. Carries the caller's
/// <see cref="AccessScope"/>, which the owner-scope gate stamps during authorization
/// so that the composed cache key can isolate tenant callers from cross-tenant
/// operator callers in the underlying cache store.
/// </summary>
/// <remarks>
/// Implementers should not set <see cref="CallerAccessScope"/> manually — it is
/// stamped by the framework after a successful owner-scope evaluation. It exists
/// as a settable property only so the evaluator can enrich it.
/// </remarks>
public interface IAuthorizableOwnerScopedCacheableQuery {

	/// <summary>
	/// The caller's <see cref="AccessScope"/> at the time authorization ran.
	/// Null before authorization; non-null after a successful owner-scope evaluation.
	/// Used purely as a cache-key discriminator to prevent cross-scope bucket sharing.
	/// </summary>
	AccessScope? CallerAccessScope { get; set; }
}

/// <summary>
/// Owner-scoped authorized read operation (query) whose result can be cached safely per tenant.
/// OwnerId and the caller's access scope are automatically composed into the cache key,
/// so a query written for one tenant cannot accidentally return cached data belonging
/// to another tenant — and a cross-tenant operator's result cannot leak into a tenant's view.
/// </summary>
/// <typeparam name="TResponse">
/// The type of response returned by the query. Must be immutable for safe caching
/// with instance reuse. Use sealed records with init-only properties.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Cache safety contract.</b> The handler's result MUST depend only on
/// <see cref="IAuthorizableOwnerScopedResource.OwnerId"/>, the discriminators inside
/// <see cref="ScopedCacheKey"/>, and shared state — <b>never</b> on the caller's identity,
/// roles, or per-caller authorization side-effects. Two callers resolving the same
/// <c>OwnerId</c> and <see cref="ScopedCacheKey"/> must always get the same result.
/// If your handler varies output based on caller identity (e.g., redacting PII, showing
/// soft-deleted rows to admins), use <see cref="IAuthorizableOwnerScopedQuery{TResponse}"/>
/// (non-cacheable) instead.
/// </para>
/// <para>
/// <b>Cache key composition.</b> Implementers supply <see cref="ScopedCacheKey"/> — a key
/// that only has to be unique <b>within a single tenant</b>. The framework composes the
/// final <see cref="ICacheableQuery{TResponse}.CacheKey"/> as
/// <c>owner:{OwnerId}:scope:{CallerAccessScope}:{ScopedCacheKey}</c>, which isolates
/// tenants from each other AND isolates cross-tenant operator callers from tenant-scoped
/// callers in the cache store.
/// </para>
/// <para>
/// <b>Global-scope callers.</b> Unlike <see cref="IAuthorizableOwnerScopedQuery{TResponse}"/>,
/// Global-scope callers of this interface <b>must</b> supply a non-null <c>OwnerId</c> —
/// the same rule that applies to owner-scoped writes. Omitting it denies with
/// <see cref="DenyCodes.CacheableReadOwnerIdRequired"/>. This prevents an unbounded
/// "null-owner" cache bucket shared across all operator callers.
/// </para>
/// <para>
/// A <c>tenant:{OwnerId}</c> tag is automatically added to
/// <see cref="ICacheableQuery{TResponse}.CacheTags"/>, enabling bulk invalidation of a
/// tenant's cached entries via <c>ICacheableQueryService.RemoveByTagAsync($"tenant:{ownerId}")</c>
/// — useful for tenant-offboarding or tenant-wide invalidation flows.
/// </para>
/// <para>
/// <b>Do not</b> include <see cref="IAuthorizableOwnerScopedResource.OwnerId"/> in
/// <see cref="ScopedCacheKey"/> — it is prefixed automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record GetTenantDashboardQuery : IAuthorizableOwnerScopedCacheableQuery&lt;DashboardView&gt; {
///     public string? OwnerId { get; set; }
///     public AccessScope? CallerAccessScope { get; set; }
///     public DateOnly AsOfDate { get; init; }
///     public DashboardSection Section { get; init; }
///
///     // Unique WITHIN a tenant — do not include OwnerId. The framework prefixes it.
///     public string ScopedCacheKey =&gt; $"dashboard:{Section}:{AsOfDate:yyyy-MM-dd}";
///
///     // Optional extra tags. "tenant:{OwnerId}" is added automatically.
///     public string[]? ScopedCacheTags =&gt; ["dashboard", $"section:{Section}"];
/// }
/// </code>
/// </example>
public interface IAuthorizableOwnerScopedCacheableQuery<TResponse>
	: IAuthorizableOwnerScopedCacheableQuery, IAuthorizableOwnerScopedQuery<TResponse>, ICacheableQuery<TResponse> {

	/// <summary>
	/// The per-tenant portion of the cache key. Uniqueness is only required
	/// <b>within a single tenant</b> — OwnerId and caller scope are prefixed automatically.
	/// </summary>
	/// <remarks>
	/// Compose this from the query's discriminating inputs (e.g., date range, status,
	/// entity id). Do not include OwnerId.
	/// </remarks>
	string ScopedCacheKey { get; }

	/// <summary>
	/// Optional tags specific to this query, on top of the automatically-added
	/// <c>tenant:{OwnerId}</c> tag. Used for targeted cache invalidation.
	/// </summary>
	string[]? ScopedCacheTags => null;

	/// <inheritdoc />
	string ICacheableQuery<TResponse>.CacheKey =>
		$"owner:{this.OwnerId}:scope:{this.CallerAccessScope}:{this.ScopedCacheKey}";

	/// <inheritdoc />
	string[]? ICacheableQuery<TResponse>.CacheTags {
		get {
			var tenantTag = $"tenant:{this.OwnerId}";
			var extra = this.ScopedCacheTags;
			if (extra is null || extra.Length == 0) {
				return [tenantTag];
			}
			return [tenantTag, .. extra];
		}
	}
}
