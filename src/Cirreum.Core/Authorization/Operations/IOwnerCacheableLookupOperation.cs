namespace Cirreum.Authorization.Operations;

using Cirreum.Authorization.Operations.Grants;
using Cirreum.Conductor;

/// <summary>
/// Grant-aware cacheable point-lookup. Composes <see cref="IOwnerLookupOperation{TResultValue}"/>
/// with <see cref="ICacheableQuery{TResultValue}"/> caching contract. The framework automatically
/// composes the final cache key as <c>{owner}:{boundary}:{CacheKey}</c> and adds a
/// <c>tenant:{OwnerId}</c> tag via <see cref="Caching.CacheKeyContext"/>, which isolates
/// tenants from each other and cross-tenant operators from tenant-scoped callers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache safety contract.</b> The handler's result MUST depend only on
/// <see cref="IGrantableLookupBase.OwnerId"/>, the discriminators inside
/// <see cref="ICacheableQuery{TResultValue}.CacheKey"/>, and shared state — <b>never</b> on the
/// caller's identity, roles, or per-caller authorization side-effects. Two callers resolving
/// the same <c>OwnerId</c> and <see cref="ICacheableQuery{TResultValue}.CacheKey"/> must always
/// get the same result.
/// </para>
/// <para>
/// <b>Global-scope callers</b> (those with <see cref="Security.AuthenticationBoundary.Global"/>)
/// <b>must</b> supply a non-null <c>OwnerId</c>.
/// Omitting it denies with <see cref="DenyCodes.CacheableReadOwnerIdRequired"/>. This
/// prevents an unbounded "null-owner" cache bucket shared across all operator callers.
/// </para>
/// <para>
/// A <c>tenant:{OwnerId}</c> tag is automatically added to
/// <see cref="ICacheableQuery{TResultValue}.CacheTags"/>, enabling bulk invalidation of a
/// tenant's cached entries.
/// </para>
/// </remarks>
/// <typeparam name="TResultValue">
/// The type of response returned by the lookup. Must be immutable for safe caching.
/// </typeparam>
public interface IOwnerCacheableLookupOperation<TResultValue>
	: IOwnerLookupOperation<TResultValue>, ICacheableQuery<TResultValue>;