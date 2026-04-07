namespace Cirreum.Authorization.Grants;

using Cirreum.Authorization.Grants.Caching;
using Cirreum.Caching;

/// <summary>
/// Core's <see cref="IAccessReachResolver"/> implementation. Composes an
/// app-provided <see cref="IGrantResolver"/> and owns every piece of
/// reach-translation policy so apps never touch <see cref="AccessReach"/> directly.
/// </summary>
/// <remarks>
/// <para>
/// Resolver flow:
/// </para>
/// <list type="number">
///   <item><description>Unauthenticated caller → <see cref="AccessReach.Denied"/>.</description></item>
///   <item><description><see cref="IGrantResolver.ShouldBypassAsync"/> returns <see langword="true"/> → <see cref="AccessReach.Unrestricted"/> (always live, never cached).</description></item>
///   <item><description>No <see cref="AuthorizationContext{TResource}.Permissions"/> declared → <see cref="AccessReach.Denied"/> (misconfig guard).</description></item>
///   <item><description>L1 check: scoped in-memory dictionary keyed by cache key string.</description></item>
///   <item><description>L2 check: <see cref="ICacheService"/> via <c>GetOrCreateAsync</c>.</description></item>
///   <item><description>Cold path: invoke <see cref="IGrantResolver.ResolveGrantsAsync"/> + <see cref="IGrantResolver.ResolveHomeOwnerAsync"/> + merge.</description></item>
/// </list>
/// <para>
/// Self-advertises via <see cref="Handles"/>: checks for
/// <see cref="IGrantedCommandBase"/>, <see cref="IGrantedReadBase"/>, or
/// <see cref="IGrantedListBase"/> on the resource type.
/// </para>
/// </remarks>
sealed class GrantBasedAccessReachResolver(
	IGrantResolver grantResolver,
	ICacheService cacheService,
	CacheSettings rootCacheSettings,
	GrantCacheSettings cacheSettings
) : IAccessReachResolver {

	private readonly IGrantResolver _grantResolver =
		grantResolver ?? throw new ArgumentNullException(nameof(grantResolver));
	private readonly ICacheService _cacheService =
		cacheService ?? throw new ArgumentNullException(nameof(cacheService));
	private readonly CacheSettings _rootCacheSettings =
		rootCacheSettings ?? throw new ArgumentNullException(nameof(rootCacheSettings));
	private readonly GrantCacheSettings _cacheSettings =
		cacheSettings ?? throw new ArgumentNullException(nameof(cacheSettings));

	// L1: scoped memoization — same cache key string as L2 for shared identity
	private readonly Dictionary<string, AccessReach> _scopeCache = [];

	public bool Handles(Type resourceType) {
		ArgumentNullException.ThrowIfNull(resourceType);
		return typeof(IGrantedCommandBase).IsAssignableFrom(resourceType) ||
			   typeof(IGrantedReadBase).IsAssignableFrom(resourceType) ||
			   typeof(IGrantedListBase).IsAssignableFrom(resourceType);
	}

	public async ValueTask<AccessReach> ResolveAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource {

		ArgumentNullException.ThrowIfNull(context);

		if (!context.IsAuthenticated) {
			return AccessReach.Denied;
		}

		// Bypass is always live — never cached. Admin promotion is immediate.
		if (await this._grantResolver.ShouldBypassAsync(context, cancellationToken).ConfigureAwait(false)) {
			return AccessReach.Unrestricted;
		}

		if (context.Permissions.Count == 0) {
			return AccessReach.Denied;
		}

		var domainFeature = context.DomainFeature ?? "unknown";
		var callerId = context.Operation.UserState.Id;
		var cacheKey = ReachCacheKeys.BuildKey(
			this._cacheSettings.Version,
			callerId,
			domainFeature,
			context.Permissions);

		// L1: scoped memoization
		if (this._scopeCache.TryGetValue(cacheKey, out var cached)) {
			return cached;
		}

		// L2: cross-request cache. When provider is None, NoCacheService
		// executes the factory directly — no branching needed.
		var reach = await this.ResolveWithL2CacheAsync(context, cacheKey, callerId, domainFeature, cancellationToken)
			.ConfigureAwait(false);

		this._scopeCache[cacheKey] = reach;
		return reach;
	}

	// L2 cache integration ————————————————————————————————————

	private async ValueTask<AccessReach> ResolveWithL2CacheAsync<TResource>(
		AuthorizationContext<TResource> context,
		string cacheKey,
		string callerId,
		string domainFeature,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource {

		var tags = ReachCacheKeys.BuildTags(callerId, domainFeature);
		var settings = this.BuildEffectiveCacheSettings(domainFeature);

		return await this._cacheService.GetOrCreateAsync(
			cacheKey,
			async ct => await this.ResolveFromGrantResolverAsync(context, ct).ConfigureAwait(false),
			settings,
			tags,
			cancellationToken).ConfigureAwait(false);
	}

	// Cold-path resolution ————————————————————————————————————

	private async ValueTask<AccessReach> ResolveFromGrantResolverAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource {

		var granted = await this._grantResolver
			.ResolveGrantsAsync(context, cancellationToken)
			.ConfigureAwait(false);

		var homeOwner = await this._grantResolver
			.ResolveHomeOwnerAsync(context, cancellationToken)
			.ConfigureAwait(false);

		var combined = Combine(granted.OwnerIds, homeOwner);
		return combined.Count == 0
			? AccessReach.Denied
			: AccessReach.ForOwners(combined, granted.Extensions);
	}

	// Cache configuration helpers —————————————————————————————

	private CacheExpirationSettings BuildEffectiveCacheSettings(string domainFeature) {
		// Cascade: domain override → grant-level default → root CacheSettings default
		var defaults = this._rootCacheSettings.DefaultExpiration;

		var expiration = this._cacheSettings.Expiration ?? defaults.Expiration;
		if (this._cacheSettings.DomainOverrides.TryGetValue(domainFeature, out var ov) &&
			ov.Expiration.HasValue) {
			expiration = ov.Expiration.Value;
		}

		return new CacheExpirationSettings(Expiration: expiration);
	}

	// Owner merge ————————————————————————————————————————————

	private static IReadOnlyList<string> Combine(IReadOnlyList<string> grantedOwners, string? homeOwner) {
		ArgumentNullException.ThrowIfNull(grantedOwners);

		if (string.IsNullOrEmpty(homeOwner)) {
			return grantedOwners;
		}

		// Quick path: home owner already present.
		for (var i = 0; i < grantedOwners.Count; i++) {
			if (string.Equals(grantedOwners[i], homeOwner, StringComparison.Ordinal)) {
				return grantedOwners;
			}
		}

		var merged = new List<string>(grantedOwners.Count + 1);
		merged.AddRange(grantedOwners);
		merged.Add(homeOwner);
		return merged;
	}
}
