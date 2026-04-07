namespace Cirreum.Conductor.Intercepts;

using Cirreum.Caching;
using Cirreum.Conductor;
using Cirreum.Conductor.Configuration;
using Cirreum.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

sealed class QueryCaching<TRequest, TResponse>
  : IIntercept<TRequest, TResponse>
	where TRequest : ICacheableQuery<TResponse> {

	private static readonly Meter _meter = new(CirreumTelemetry.Meters.ConductorCache, CirreumTelemetry.Version);
	private static readonly Histogram<double> _cacheOperationDuration = _meter.CreateHistogram<double>(
		ConductorTelemetry.CacheDurationMetric,
		unit: "ms",
		description: "Cache operation duration");

	private readonly ICacheService _cache;
	private readonly ConductorSettings _conductorSettings;
	private readonly CacheSettings _cacheSettings;
	private readonly ILogger<QueryCaching<TRequest, TResponse>> _logger;

	public QueryCaching(
		ICacheService cache,
		ConductorSettings conductorSettings,
		CacheSettings cacheSettings,
		ILogger<QueryCaching<TRequest, TResponse>> logger) {
		this._cache = cache;
		this._conductorSettings = conductorSettings;
		this._cacheSettings = cacheSettings;
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

		if (this._logger.IsEnabled(LogLevel.Debug)) {
			this._logger.LogDebug(
				"Processing cacheable query: {QueryType} (CacheKey: {CacheKey})",
				context.RequestType,
				context.Request.CacheKey);
		}

		var effectiveSettings = this.BuildEffectiveSettings(context.Request, context.RequestType);
		var tags = context.Request.CacheTags;

		var startTime = Timing.Start();

		// Get from Cache, or Read from real Handler and store in Cache
		var result = await this._cache.GetOrCreateAsync(
			context.Request.CacheKey,
			async (ct) => await next(context, ct), // actual handler that reads data
			effectiveSettings,
			tags,
			cancellationToken);

		var elapsed = Timing.GetElapsedMilliseconds(startTime);

		var telemetryTags = new TagList {
			{ ConductorTelemetry.QueryNameTag, context.RequestType },
			{ ConductorTelemetry.QueryStatusTag, result.IsSuccess ? "success" : "failure" }
		};

		_cacheOperationDuration.Record(elapsed, telemetryTags);

		if (this._logger.IsEnabled(LogLevel.Debug)) {
			this._logger.LogDebug(
				"Query {QueryType} completed: Status={Status}, Duration={Duration}ms",
				context.RequestType,
				result.IsSuccess ? "Success" : "Failed",
				Math.Round(elapsed, 2));
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

}
