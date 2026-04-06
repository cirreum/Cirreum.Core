namespace Cirreum.Authorization.Grants.Caching;

using Cirreum.Conductor.Caching;

/// <summary>
/// Default implementation of <see cref="IGrantCacheInvalidator"/>. Delegates to the
/// application's registered <see cref="ICacheableQueryService"/> for tag-based removal.
/// Registered as a singleton.
/// </summary>
public sealed class GrantCacheInvalidator(ICacheableQueryService cacheService)
	: IGrantCacheInvalidator {

	private readonly ICacheableQueryService _cacheService =
		cacheService ?? throw new ArgumentNullException(nameof(cacheService));

	/// <inheritdoc />
	public ValueTask InvalidateCallerAsync(
		string callerId,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(callerId);
		var tag = ReachCacheKeys.CallerTag(callerId);
		return this._cacheService.RemoveByTagAsync(tag, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask InvalidateDomainAsync<TDomain>(
		CancellationToken cancellationToken = default)
		where TDomain : class {

		var domain = GrantDomainCache.GetFor<TDomain>().Namespace;
		var tag = ReachCacheKeys.DomainTag(domain);
		return this._cacheService.RemoveByTagAsync(tag, cancellationToken);
	}
}
