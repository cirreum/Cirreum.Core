namespace Cirreum.Authorization.Operations.Grants.Caching;

using Cirreum.Caching;

/// <summary>
/// Default implementation of <see cref="IOperationGrantCacheInvalidator"/>. Delegates to the
/// application's registered <see cref="ICacheService"/> for tag-based removal.
/// Registered as a singleton.
/// </summary>
public sealed class OperationGrantCacheInvalidator(ICacheService cacheService)
	: IOperationGrantCacheInvalidator {

	private readonly ICacheService _cacheService =
		cacheService ?? throw new ArgumentNullException(nameof(cacheService));

	/// <inheritdoc />
	public ValueTask InvalidateCallerAsync(
		string callerId,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(callerId);
		var tag = OperationGrantCacheKeys.CallerTag(callerId);
		return this._cacheService.RemoveByTagAsync(tag, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask InvalidateDomainAsync(
		string domainFeature,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(domainFeature);
		var tag = OperationGrantCacheKeys.DomainTag(domainFeature);
		return this._cacheService.RemoveByTagAsync(tag, cancellationToken);
	}
}
