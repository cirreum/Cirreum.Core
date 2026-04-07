namespace Cirreum.Caching;

/// <summary>
/// Central cache configuration for the Cirreum platform. Bound from the
/// <c>Cirreum:Cache</c> configuration section.
/// </summary>
/// <remarks>
/// <para>
/// This is the single source of truth for cache provider selection and default
/// expiration policy. Subsystems are cache <em>participants</em> — they may override
/// expiration per-category or per-domain but never choose the provider or global
/// defaults independently.
/// </para>
/// <para>
/// When <see cref="Provider"/> is <see cref="CacheProvider.None"/>, the DI container
/// registers <see cref="NoCacheService"/>, which executes every factory directly.
/// Participants that call <see cref="ICacheService.GetOrCreateAsync{TResponse}"/>
/// degrade transparently — no branching required.
/// </para>
/// </remarks>
public sealed class CacheSettings {

	/// <summary>
	/// The configuration section path for binding.
	/// </summary>
	public const string SectionPath = "Cirreum:Cache";

	/// <summary>
	/// The cache provider to use. Defaults to <see cref="CacheProvider.None"/>.
	/// <list type="bullet">
	///   <item><description><see cref="CacheProvider.None"/> — caching disabled; safe default.</description></item>
	///   <item><description><see cref="CacheProvider.InMemory"/> — single-instance in-memory cache (Blazor WASM, development).</description></item>
	///   <item><description><see cref="CacheProvider.Distributed"/> — distributed cache only (Azure Functions, serverless).</description></item>
	///   <item><description><see cref="CacheProvider.Hybrid"/> — L1 (memory) + L2 (distributed) for multi-instance apps.</description></item>
	/// </list>
	/// </summary>
	public CacheProvider Provider { get; set; } = CacheProvider.None;

	/// <summary>
	/// Default expiration policy inherited by all cache participants unless they
	/// specify an override at the category, query, or domain level.
	/// </summary>
	/// <example>
	/// <code>
	/// "DefaultExpiration": {
	///   "Expiration": "00:05:00",
	///   "LocalExpiration": "00:02:00",
	///   "FailureExpiration": "00:00:30"
	/// }
	/// </code>
	/// </example>
	public QueryCacheOverride DefaultExpiration { get; set; } = new();
}
