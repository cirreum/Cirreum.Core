namespace Cirreum.Conductor.Intercepts;

using Cirreum.Caching;
using Cirreum.Conductor;
using Cirreum.Conductor.Configuration;
using Microsoft.Extensions.Logging;

sealed class QueryCaching<TRequest, TResponse>
  : IIntercept<TRequest, TResponse>
	where TRequest : ICacheableQuery<TResponse> {

	private readonly ICacheService _cache;
	private readonly ConductorSettings _conductorSettings;
	private readonly CacheSettings _cacheSettings;
	private readonly CacheKeyContext _cacheKeyContext;
	private readonly ILogger<QueryCaching<TRequest, TResponse>> _logger;

	public QueryCaching(
		ICacheService cache,
		ConductorSettings conductorSettings,
		CacheSettings cacheSettings,
		CacheKeyContext cacheKeyContext,
		ILogger<QueryCaching<TRequest, TResponse>> logger) {
		this._cache = cache;
		this._conductorSettings = conductorSettings;
		this._cacheSettings = cacheSettings;
		this._cacheKeyContext = cacheKeyContext;
		this._logger = logger;

		// Check once at construction time instead of every request
		if (cacheSettings.Provider == CacheProvider.Hybrid &&
			cache is NoCacheService) {
			logger.LogWarning(
				"CacheProvider is set to 'Hybrid' yet the service is not registered. " +
				"Did you forget to add a hybrid caching implementation?");
		}
		if (cacheSettings.Provider == CacheProvider.Distributed &&
			cache is NoCacheService) {
			logger.LogWarning(
				"CacheProvider is set to 'Distributed' yet the service is not registered. " +
				"Did you forget to add a distributed caching implementation?");
		}
	}

	public async Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken) {

		var cacheKey = this.ComposeCacheKey(context.Request.CacheKey);
		var cacheTags = this.ComposeCacheTags(context.Request.CacheTags);

		if (this._logger.IsEnabled(LogLevel.Debug)) {
			this._logger.LogDebug(
				"Processing cacheable query: {QueryType} (CacheKey: {CacheKey})",
				context.RequestType,
				cacheKey);
		}

		var effectiveSettings = this.BuildEffectiveSettings(context.Request, context.RequestType);

		// Get from Cache, or Read from real Handler and store in Cache.
		// Telemetry (hit/miss, duration) is handled by the InstrumentedCacheService decorator.
		var result = await this._cache.GetOrCreateAsync(
			cacheKey,
			async (ct) => await next(context, ct), // actual handler that reads data
			effectiveSettings,
			cacheTags,
			cancellationToken);

		if (this._logger.IsEnabled(LogLevel.Debug)) {
			this._logger.LogDebug(
				"Query {QueryType} completed: Status={Status}",
				context.RequestType,
				result.IsSuccess ? "Success" : "Failed");
		}

		return result;
	}

	private CacheExpirationSettings BuildEffectiveSettings(TRequest request, string queryTypeName) {
		var querySettings = request.CacheExpiration;
		var cacheOptions = this._conductorSettings.Cache;

		var expiration = querySettings.Expiration;
		var localExpiration = querySettings.LocalExpiration;
		var failureExpiration = querySettings.FailureExpiration;

		// Apply exact query-specific overrides (highest priority)
		if (cacheOptions.QueryOverrides.TryGetValue(queryTypeName, out var queryOverrides)) {
			expiration = queryOverrides.Expiration ?? expiration;
			localExpiration = queryOverrides.LocalExpiration ?? localExpiration;
			failureExpiration = queryOverrides.FailureExpiration ?? failureExpiration;

			if (this._logger.IsEnabled(LogLevel.Debug)) {
				this._logger.LogDebug("Applied exact override for {QueryType}", queryTypeName);
			}
		}

		// Apply global defaults from central CacheSettings for any remaining nulls
		var defaults = this._cacheSettings.DefaultExpiration;
		expiration ??= defaults.Expiration;
		localExpiration ??= defaults.LocalExpiration;
		failureExpiration ??= defaults.FailureExpiration;

		return new CacheExpirationSettings(
			Expiration: expiration,
			LocalExpiration: localExpiration,
			FailureExpiration: failureExpiration
		);
	}

	private string ComposeCacheKey(string baseKey) =>
		this._cacheKeyContext.KeyPrefix is not null
			? $"{this._cacheKeyContext.KeyPrefix}:{baseKey}"
			: baseKey;

	private string[]? ComposeCacheTags(string[]? baseTags) {
		var extra = this._cacheKeyContext.ExtraTags;
		if (extra is null) {
			return baseTags;
		}
		return baseTags is null or { Length: 0 }
			? extra
			: [.. extra, .. baseTags];
	}

}
