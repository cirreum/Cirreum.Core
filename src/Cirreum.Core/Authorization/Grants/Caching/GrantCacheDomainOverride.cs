namespace Cirreum.Authorization.Grants.Caching;

/// <summary>
/// Per-domain override for <see cref="GrantCacheSettings"/>. Fields that are
/// <see langword="null"/> fall through to the top-level defaults.
/// </summary>
public sealed class GrantCacheDomainOverride {

	/// <summary>
	/// Override the <see cref="GrantCacheSettings.Enabled"/> flag for this domain.
	/// <see langword="null"/> inherits the global setting.
	/// </summary>
	public bool? Enabled { get; set; }

	/// <summary>
	/// Override the <see cref="GrantCacheSettings.Expiration"/> for this domain.
	/// <see langword="null"/> inherits the global setting.
	/// </summary>
	public TimeSpan? Expiration { get; set; }
}
