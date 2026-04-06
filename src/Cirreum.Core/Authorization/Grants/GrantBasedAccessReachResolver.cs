namespace Cirreum.Authorization.Grants;

using Cirreum.Authorization.Grants.Caching;
using Cirreum.Conductor;
using Cirreum.Conductor.Caching;

/// <summary>
/// Core's generic <see cref="IAccessReachResolver"/> implementation. Composes an
/// app-provided <see cref="IGrantResolver{TDomain}"/> and owns every piece of
/// reach-translation policy so apps never touch <see cref="AccessReach"/> directly.
/// </summary>
/// <remarks>
/// <para>
/// Resolver flow:
/// </para>
/// <list type="number">
///   <item><description>Unauthenticated caller → <see cref="AccessReach.Denied"/>.</description></item>
///   <item><description><see cref="IGrantResolver{TDomain}.ShouldBypassAsync"/> returns <see langword="true"/> → <see cref="AccessReach.Unrestricted"/> (always live, never cached).</description></item>
///   <item><description>No <see cref="AuthorizationContext{TResource}.RequiredPermissions"/> declared → <see cref="AccessReach.Denied"/> (misconfig guard).</description></item>
///   <item><description>L1 check: scoped in-memory dictionary keyed by cache key string.</description></item>
///   <item><description>L2 check: <see cref="ICacheableQueryService"/> via <c>GetOrCreateAsync</c>.</description></item>
///   <item><description>Cold path: invoke <see cref="IGrantResolver{TDomain}.ResolveGrantsAsync"/> + <see cref="IGrantResolver{TDomain}.ResolveHomeOwnerAsync"/> + merge.</description></item>
/// </list>
/// <para>
/// Self-advertises via <see cref="Handles"/>: inspects the resource type's generic interfaces
/// to find <c>IGrantedCommand&lt;TDomain&gt;</c>, <c>IGrantedRead&lt;TDomain, _&gt;</c>,
/// <c>IGrantedList&lt;TDomain, _&gt;</c>, or <c>IGrantedCacheableRead&lt;TDomain, _&gt;</c>.
/// Core's <see cref="AccessReachResolverSelector"/> enforces 1:1 — if two resolvers claim the
/// same resource type, the selector fails fast.
/// </para>
/// </remarks>
/// <typeparam name="TDomain">
/// The bounded-context domain marker used as the first type argument in the Granted
/// interfaces (e.g., <c>IIssueOperation</c>).
/// </typeparam>
sealed class GrantBasedAccessReachResolver<TDomain>(
	IGrantResolver<TDomain> grantResolver,
	ICacheableQueryService cacheService,
	GrantCacheSettings cacheSettings
) : IAccessReachResolver
	where TDomain : class {

	private static readonly Type[] GrantedOpenGenerics = [
		typeof(IGrantedCommand<>),
		typeof(IGrantedCommand<,>),
		typeof(IGrantedRead<,>),
		typeof(IGrantedList<,>),
		typeof(IGrantedCacheableRead<,>),
	];

	private static readonly string DomainNamespace = GrantDomainCache.GetFor<TDomain>().Namespace;

	private readonly IGrantResolver<TDomain> _grantResolver =
		grantResolver ?? throw new ArgumentNullException(nameof(grantResolver));
	private readonly ICacheableQueryService _cacheService =
		cacheService ?? throw new ArgumentNullException(nameof(cacheService));
	private readonly GrantCacheSettings _cacheSettings =
		cacheSettings ?? throw new ArgumentNullException(nameof(cacheSettings));

	// L1: scoped memoization — same cache key string as L2 for shared identity
	private readonly Dictionary<string, AccessReach> _scopeCache = [];

	public bool Handles(Type resourceType) {
		ArgumentNullException.ThrowIfNull(resourceType);

		foreach (var iface in resourceType.GetInterfaces()) {
			if (!iface.IsGenericType) {
				continue;
			}
			var def = iface.GetGenericTypeDefinition();
			for (var i = 0; i < GrantedOpenGenerics.Length; i++) {
				if (def == GrantedOpenGenerics[i] && iface.GetGenericArguments()[0] == typeof(TDomain)) {
					return true;
				}
			}
		}
		return false;
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

		if (context.RequiredPermissions.Count == 0) {
			return AccessReach.Denied;
		}

		var callerId = context.Operation.UserState.Id;
		var cacheKey = ReachCacheKeys.BuildKey(
			this._cacheSettings.Version,
			callerId,
			DomainNamespace,
			context.RequiredPermissions);

		// L1: scoped memoization
		if (this._scopeCache.TryGetValue(cacheKey, out var cached)) {
			return cached;
		}

		// L2: cross-request cache (or direct resolution if caching disabled)
		var reach = this.IsCacheEnabled()
			? await this.ResolveWithL2CacheAsync(context, cacheKey, callerId, cancellationToken)
				.ConfigureAwait(false)
			: await this.ResolveFromGrantResolverAsync(context, cancellationToken)
				.ConfigureAwait(false);

		this._scopeCache[cacheKey] = reach;
		return reach;
	}

	// L2 cache integration ————————————————————————————————————

	private async ValueTask<AccessReach> ResolveWithL2CacheAsync<TResource>(
		AuthorizationContext<TResource> context,
		string cacheKey,
		string callerId,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource {

		var tags = ReachCacheKeys.BuildTags(callerId, DomainNamespace);
		var settings = this.BuildEffectiveCacheSettings();

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

	private bool IsCacheEnabled() {
		if (!this._cacheSettings.Enabled) {
			return false;
		}
		if (this._cacheSettings.DomainOverrides.TryGetValue(DomainNamespace, out var ov)) {
			return ov.Enabled ?? this._cacheSettings.Enabled;
		}
		return true;
	}

	private CacheExpirationSettings BuildEffectiveCacheSettings() {
		var expiration = this._cacheSettings.Expiration;
		if (this._cacheSettings.DomainOverrides.TryGetValue(DomainNamespace, out var ov) &&
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
