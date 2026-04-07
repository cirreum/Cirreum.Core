namespace Cirreum.Authorization.Grants.Caching;

/// <summary>
/// Configuration for the built-in reach cache. Bound from
/// <c>Cirreum:Authorization:Grants:Cache</c> in application settings.
/// </summary>
/// <remarks>
/// <para>
/// The reach cache stores computed <see cref="AccessReach"/> results per caller and
/// permission set. It operates at two levels: L1 (scoped in-memory dictionary per DI
/// scope) and L2 (cross-request via <c>ICacheService</c>).
/// </para>
/// <para>
/// Settings can be overridden per domain via <see cref="DomainOverrides"/>, keyed by
/// the domain feature name (e.g., <c>"issues"</c>).
/// </para>
/// </remarks>
public sealed class GrantCacheSettings {

	/// <summary>
	/// The configuration section path for binding.
	/// </summary>
	public const string SectionPath = "Cirreum:Authorization:Grants:Cache";

	/// <summary>
	/// Cache key version. Changing this value effectively invalidates all existing cache
	/// entries without requiring an explicit purge — entries with the old version simply
	/// miss. Useful for schema evolution or emergency cache busting via environment
	/// variables in Azure Container Apps.
	/// </summary>
	public int Version { get; set; } = 1;

	/// <summary>
	/// Absolute expiration for L2 reach cache entries. If <see langword="null"/>,
	/// inherits from <see cref="Cirreum.Caching.CacheSettings.DefaultExpiration"/>.
	/// </summary>
	public TimeSpan? Expiration { get; set; }

	/// <summary>
	/// Per-domain overrides keyed by the domain feature name
	/// (e.g., <c>"issues"</c>, <c>"admin"</c>). Overrides are merged with the
	/// top-level settings; <see langword="null"/> fields fall through to the default.
	/// </summary>
	public Dictionary<string, GrantCacheDomainOverride> DomainOverrides { get; set; } = [];
}
