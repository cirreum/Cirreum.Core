namespace Cirreum.Authorization.Grants.Caching;

using Cirreum.Caching;

/// <summary>
/// Default implementation of <see cref="IGrantCacheInvalidator"/>. Delegates to the
/// application's registered <see cref="ICacheService"/> for tag-based removal.
/// Registered as a singleton.
/// </summary>
public sealed class GrantCacheInvalidator(ICacheService cacheService)
	: IGrantCacheInvalidator {

	private readonly ICacheService _cacheService =
		cacheService ?? throw new ArgumentNullException(nameof(cacheService));

	/// <inheritdoc />
	public ValueTask InvalidateCallerAsync(
		string callerId,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(callerId);
		var tag = GrantCacheKeys.CallerTag(callerId);
		return this._cacheService.RemoveByTagAsync(tag, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask InvalidateDomainAsync(
		string domainFeature,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(domainFeature);
		var tag = GrantCacheKeys.DomainTag(domainFeature);
		return this._cacheService.RemoveByTagAsync(tag, cancellationToken);
	}
}
