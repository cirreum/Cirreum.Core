namespace Cirreum.Conductor.Configuration;

/// <summary>
/// Cache provider types for Conductor queries.
/// </summary>
public enum CacheProvider {
	/// <summary>
	/// No caching - queries always execute.
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