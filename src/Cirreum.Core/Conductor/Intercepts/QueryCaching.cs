namespace Cirreum.Conductor.Intercepts;

using Cirreum.Conductor;
using Cirreum.Conductor.Caching;
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

	private readonly ICacheableQueryService _cache;
	private readonly ConductorSettings _conductorSettings;
	private readonly ILogger<QueryCaching<TRequest, TResponse>> _logger;

	public QueryCaching(
		ICacheableQueryService cache,
		ConductorSettings conductorSettings,
		ILogger<QueryCaching<TRequest, TResponse>> logger) {
		_cache = cache;
		_conductorSettings = conductorSettings;
		_logger = logger;

		// Check once at construction time instead of every request
		if (conductorSettings.Cache.Provider == CacheProvider.Hybrid &&
			cache is NoCacheQueryService) {
			logger.LogWarning(
				"CacheProvider is set to 'Hybrid' yet the service is not registered. " +
				"Did you forget to add a hybrid caching implementation?");
		}
		if (conductorSettings.Cache.Provider == CacheProvider.Distributed &&
			cache is NoCacheQueryService) {
			logger.LogWarning(
				"CacheProvider is set to 'Distributed' yet the service is not registered. " +
				"Did you forget to add a distributed caching implementation?");
		}
	}

	public async Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken) {

		var category = context.Request.CacheCategory ?? "uncategorized";

		if (_logger.IsEnabled(LogLevel.Debug)) {
			_logger.LogDebug(
				"Processing cacheable query: {QueryType} (Category: {Category}, CacheKey: {CacheKey})",
				context.RequestType,
				category,
				context.Request.CacheKey);
		}

		var effectiveSettings = this.BuildEffectiveSettings(context.Request, context.RequestType);
		var tags = context.Request.CacheTags;

		var startTime = Timing.Start();

		// Get from Cache, or Read from real Handler and store in Cache
		var result = await _cache.GetOrCreateAsync(
			context.Request.CacheKey,
			async (ct) => await next(context, ct), // actual handler that reads data
			effectiveSettings,
			tags,
			cancellationToken);

		var elapsed = Timing.GetElapsedMilliseconds(startTime);

		var telemetryTags = new TagList {
			{ ConductorTelemetry.QueryNameTag, context.RequestType },
			{ ConductorTelemetry.QueryCategoryTag, category },
			{ ConductorTelemetry.QueryStatusTag, result.IsSuccess ? "success" : "failure" }
		};

		_cacheOperationDuration.Record(elapsed, telemetryTags);

		if (_logger.IsEnabled(LogLevel.Debug)) {
			_logger.LogDebug(
				"Query {QueryType} completed: Status={Status}, Category={Category}, Duration={Duration}ms",
				context.RequestType,
				result.IsSuccess ? "Success" : "Failed",
				category,
				Math.Round(elapsed, 2));
		}

		return result;
	}

	private QueryCacheSettings BuildEffectiveSettings(TRequest request, string queryTypeName) {
		var querySettings = request.Cache;
		var cacheOptions = _conductorSettings.Cache;

		var expiration = querySettings.Expiration;
		var localExpiration = querySettings.LocalExpiration;
		var failureExpiration = querySettings.FailureExpiration;

		// Apply category-based overrides if category is specified
		if (request.CacheCategory is not null &&
			cacheOptions.CategoryOverrides.TryGetValue(request.CacheCategory, out var categoryOverrides)) {
			expiration = categoryOverrides.Expiration ?? expiration;
			localExpiration = categoryOverrides.LocalExpiration ?? localExpiration;
			failureExpiration = categoryOverrides.FailureExpiration ?? failureExpiration;

			if (_logger.IsEnabled(LogLevel.Debug)) {
				_logger.LogDebug("Applied category override '{Category}' for {QueryType}",
					request.CacheCategory, queryTypeName);
			}
		}

		// Apply exact query-specific overrides (highest priority)
		if (cacheOptions.QueryOverrides.TryGetValue(queryTypeName, out var queryOverrides)) {
			expiration = queryOverrides.Expiration ?? expiration;
			localExpiration = queryOverrides.LocalExpiration ?? localExpiration;
			failureExpiration = queryOverrides.FailureExpiration ?? failureExpiration;

			if (_logger.IsEnabled(LogLevel.Debug)) {
				_logger.LogDebug("Applied exact override for {QueryType}", queryTypeName);
			}
		}

		// Apply global defaults for any remaining nulls
		expiration ??= cacheOptions.DefaultExpiration;
		localExpiration ??= cacheOptions.DefaultLocalExpiration;
		failureExpiration ??= cacheOptions.DefaultFailureExpiration;

		return new QueryCacheSettings(
			Expiration: expiration,
			LocalExpiration: localExpiration,
			FailureExpiration: failureExpiration
		);
	}

}