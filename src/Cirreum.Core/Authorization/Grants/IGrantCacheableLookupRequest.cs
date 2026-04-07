namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;
using Cirreum.Security;

/// <summary>
/// Grant-aware cacheable point-lookup. Composes <see cref="IGrantLookupRequest{TResponse}"/>
/// with the <see cref="IGrantableCacheableLookupBase"/> detection surface and
/// <see cref="ICacheableQuery{TResponse}"/> caching contract. The framework composes
/// the final cache key as
/// <c>owner:{OwnerId}:scope:{CallerAccessScope}:{ScopedCacheKey}</c>, which isolates
/// tenants from each other and cross-tenant operators from tenant-scoped callers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache safety contract.</b> The handler's result MUST depend only on
/// <see cref="IGrantableLookupBase.OwnerId"/>, the discriminators inside
/// <see cref="ScopedCacheKey"/>, and shared state — <b>never</b> on the caller's identity,
/// roles, or per-caller authorization side-effects. Two callers resolving the same
/// <c>OwnerId</c> and <see cref="ScopedCacheKey"/> must always get the same result.
/// </para>
/// <para>
/// <b>Global-scope callers</b> of this interface <b>must</b> supply a non-null <c>OwnerId</c>.
/// Omitting it denies with <see cref="DenyCodes.CacheableReadOwnerIdRequired"/>. This
/// prevents an unbounded "null-owner" cache bucket shared across all operator callers.
/// </para>
/// <para>
/// A <c>tenant:{OwnerId}</c> tag is automatically added to
/// <see cref="ICacheableQuery{TResponse}.CacheTags"/>, enabling bulk invalidation of a
/// tenant's cached entries.
/// </para>
/// </remarks>
/// <typeparam name="TResponse">
/// The type of response returned by the lookup. Must be immutable for safe caching.
/// </typeparam>
public interface IGrantCacheableLookupRequest<TResponse>
	: IGrantLookupRequest<TResponse>, IGrantableCacheableLookupBase, ICacheableQuery<TResponse> {

	/// <summary>
	/// The per-tenant portion of the cache key. Uniqueness is only required
	/// <b>within a single tenant</b> — OwnerId and caller scope are prefixed automatically.
	/// </summary>
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
