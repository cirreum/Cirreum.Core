namespace Cirreum.Authorization.Grants;

using Cirreum.Authorization.Grants.Caching;
using Cirreum.Caching;
using Cirreum.Diagnostics;

/// <summary>
/// Core's <see cref="IAccessGrantFactory"/> implementation. Composes an
/// app-provided <see cref="IGrantResolver"/> and owns every piece of
/// grant-translation policy so apps never touch <see cref="AccessGrant"/> directly.
/// </summary>
/// <remarks>
/// <para>
/// Factory flow:
/// </para>
/// <list type="number">
///   <item><description>Unauthenticated caller → <see cref="AccessGrant.Denied"/>.</description></item>
///   <item><description><see cref="IGrantResolver.ShouldBypassAsync"/> returns <see langword="true"/> → <see cref="AccessGrant.Unrestricted"/> (always live, never cached).</description></item>
///   <item><description>No <see cref="AuthorizationContext{TResource}.Permissions"/> declared → <see cref="AccessGrant.Denied"/> (misconfig guard).</description></item>
///   <item><description>L1 check: scoped in-memory dictionary keyed by cache key string.</description></item>
///   <item><description>L2 check: <see cref="ICacheService"/> via <c>GetOrCreateAsync</c>.</description></item>
///   <item><description>Cold path: invoke <see cref="IGrantResolver.ResolveGrantsAsync"/> + <see cref="IGrantResolver.ResolveHomeOwnerAsync"/> + merge.</description></item>
/// </list>
/// </remarks>
sealed class AccessGrantFactory(
	IGrantResolver grantResolver,
	ICacheService cacheService,
	CacheSettings rootCacheSettings,
	GrantCacheSettings cacheSettings
) : IAccessGrantFactory {

	private readonly IGrantResolver _grantResolver =
		grantResolver ?? throw new ArgumentNullException(nameof(grantResolver));
	private readonly ICacheService _cacheService =
		cacheService ?? throw new ArgumentNullException(nameof(cacheService));
	private readonly CacheSettings _rootCacheSettings =
		rootCacheSettings ?? throw new ArgumentNullException(nameof(rootCacheSettings));
	private readonly GrantCacheSettings _cacheSettings =
		cacheSettings ?? throw new ArgumentNullException(nameof(cacheSettings));

	// L1: scoped memoization — same cache key string as L2 for shared identity
	private readonly Dictionary<string, AccessGrant> _scopeCache = [];

	public async ValueTask<AccessGrant> CreateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource {

		ArgumentNullException.ThrowIfNull(context);

		var resourceType = typeof(TResource).Name;

		if (!context.IsAuthenticated) {
			AuthorizationTelemetry.RecordGrantResolution(
				domain: null, resourceType, AuthorizationTelemetry.GrantLevelDeniedEarly);
			return AccessGrant.Denied;
		}

		// Bypass is always live — never cached. Admin promotion is immediate.
		if (await this._grantResolver.ShouldBypassAsync(context, cancellationToken).ConfigureAwait(false)) {
			AuthorizationTelemetry.RecordGrantResolution(
				domain: null, resourceType, AuthorizationTelemetry.GrantLevelBypass);
			return AccessGrant.Unrestricted;
		}

		var domainFeature = context.DomainFeature ?? "unknown";

		if (context.Permissions.Count == 0) {
			AuthorizationTelemetry.RecordGrantResolution(
				domainFeature, resourceType, AuthorizationTelemetry.GrantLevelDeniedEarly);
			return AccessGrant.Denied;
		}

		var callerId = context.Operation.UserState.Id;
		var cacheKey = GrantCacheKeys.BuildKey(
			this._cacheSettings.Version,
			callerId,
			domainFeature,
			context.Permissions);

		// L1: scoped memoization
		if (this._scopeCache.TryGetValue(cacheKey, out var cached)) {
			AuthorizationTelemetry.RecordGrantResolution(
				domainFeature, resourceType, AuthorizationTelemetry.GrantLevelL1Hit);
			return cached;
		}

		// L2: cross-request cache. When provider is None, NoCacheService
		// executes the factory directly — no branching needed.
		var l2Start = Timing.Start();
		var grant = await this.CreateWithL2CacheAsync(context, cacheKey, callerId, domainFeature, cancellationToken)
			.ConfigureAwait(false);
		AuthorizationTelemetry.RecordGrantResolution(
			domainFeature, resourceType, AuthorizationTelemetry.GrantLevelL2,
			durationMs: Timing.GetElapsedMilliseconds(l2Start));

		this._scopeCache[cacheKey] = grant;
		return grant;
	}

	// L2 cache integration ————————————————————————————————————

	private async ValueTask<AccessGrant> CreateWithL2CacheAsync<TResource>(
		AuthorizationContext<TResource> context,
		string cacheKey,
		string callerId,
		string domainFeature,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource {

		var tags = GrantCacheKeys.BuildTags(callerId, domainFeature);
		var settings = this.BuildEffectiveCacheSettings(domainFeature);

		return await this._cacheService.GetOrCreateAsync(
			cacheKey,
			async ct => await this.CreateFromGrantResolverAsync(context, ct).ConfigureAwait(false),
			settings,
			tags,
			cancellationToken).ConfigureAwait(false);
	}

	// Cold-path resolution ————————————————————————————————————

	private async ValueTask<AccessGrant> CreateFromGrantResolverAsync<TResource>(
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
			? AccessGrant.Denied
			: AccessGrant.ForOwners(combined, granted.Extensions);
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
