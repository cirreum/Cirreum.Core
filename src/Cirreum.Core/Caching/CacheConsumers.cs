namespace Cirreum.Caching;

/// <summary>
/// Well-known keyed-service keys for <see cref="ICacheService"/> consumers.
/// Each key maps to an <see cref="InstrumentedCacheService"/> instance whose
/// <c>consumer</c> tag is pre-set to the same value, so cache metrics can be
/// sliced by subsystem at zero runtime cost.
/// </summary>
public static class CacheConsumers {

	/// <summary>Conductor query-caching intercept.</summary>
	public const string QueryCaching = "query-caching";

	/// <summary>Authorization grant resolution (L2 cross-request cache).</summary>
	public const string GrantResolution = "grant-resolution";
}
