namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;
using Cirreum.Security;

/// <summary>
/// Non-generic sidecar interface for grant-aware cacheable reads. Extends
/// <see cref="IGrantedRead"/> with a <see cref="CallerAccessScope"/> discriminator
/// that the grant evaluator stamps after successful authorization to isolate
/// per-scope cache buckets.
/// </summary>
public interface IGrantedCacheableRead : IGrantedRead {

	/// <summary>
	/// The caller's <see cref="AccessScope"/> at the time authorization ran.
	/// Null before authorization; non-null after a successful grant evaluation.
	/// Used purely as a cache-key discriminator to prevent cross-scope bucket sharing.
	/// </summary>
	AccessScope? CallerAccessScope { get; set; }
}

/// <summary>
/// Grant-aware cacheable point-read. Composes <see cref="IGrantedRead{TDomain, TResponse}"/>
/// with the <see cref="IGrantedCacheableRead"/> sidecar and <see cref="ICacheableQuery{TResponse}"/>
/// caching contract. The framework composes the final cache key as
/// <c>owner:{OwnerId}:scope:{CallerAccessScope}:{ScopedCacheKey}</c>, which isolates
/// tenants from each other and cross-tenant operators from tenant-scoped callers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache safety contract.</b> The handler's result MUST depend only on
/// <see cref="IGrantedRead.OwnerId"/>, the discriminators inside
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
/// <typeparam name="TDomain">The bounded-context domain marker.</typeparam>
/// <typeparam name="TResponse">
/// The type of response returned by the read. Must be immutable for safe caching.
/// </typeparam>
public interface IGrantedCacheableRead<TDomain, TResponse>
	: IGrantedRead<TDomain, TResponse>, IGrantedCacheableRead, ICacheableQuery<TResponse>
	where TDomain : class {

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
