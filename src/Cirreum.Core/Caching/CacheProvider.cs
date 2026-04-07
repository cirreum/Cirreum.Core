namespace Cirreum.Caching;

/// <summary>
/// Specifies the caching infrastructure used by the Cirreum platform.
/// Configured centrally via <see cref="CacheSettings.Provider"/>.
/// </summary>
public enum CacheProvider {
	/// <summary>
	/// No caching — factories always execute. Safe default.
	/// </summary>
	None,

	/// <summary>
	/// Simple in-memory cache for single-instance scenarios. Not distributed.
	/// Use for: Blazor WASM, development, testing, or single-instance deployments.
	/// </summary>
	InMemory,

	/// <summary>
	/// Distributed cache only (Redis, SQL Server, Cosmos DB). No local L1 tier.
	/// Use for: Azure Functions, serverless, or when you want explicit distributed caching.
	/// </summary>
	Distributed,

	/// <summary>
	/// HybridCache with L1 (in-memory) and L2 (distributed) tiers.
	/// Use for: Multi-instance web apps, Container Apps, App Service with scale-out.
	/// </summary>
	Hybrid
}