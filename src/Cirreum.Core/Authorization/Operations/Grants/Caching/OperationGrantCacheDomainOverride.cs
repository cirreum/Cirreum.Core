namespace Cirreum.Authorization.Operations.Grants.Caching;

/// <summary>
/// Per-domain override for <see cref="OperationGrantCacheSettings"/>. Fields that are
/// <see langword="null"/> fall through to the top-level defaults.
/// </summary>
public sealed class OperationGrantCacheDomainOverride {

	/// <summary>
	/// Override the <see cref="OperationGrantCacheSettings.Expiration"/> for this domain.
	/// <see langword="null"/> inherits the global setting.
	/// </summary>
	public TimeSpan? Expiration { get; set; }
}
