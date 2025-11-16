namespace Cirreum.Conductor.Intercepts;

using Cirreum.Conductor;
using Cirreum.Conductor.Caching;
using Cirreum.Conductor.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class QueryCaching<TRequest, TResponse>(
	ICacheableQueryService cache,
	ConductorSettings conductorSettings,
	ILogger<QueryCaching<TRequest, TResponse>> logger
) : IIntercept<TRequest, TResponse>
	where TRequest : ICacheableQuery<TResponse> {

	private static readonly Meter _meter = new(ConductorTelemetry.CacheMeterName);

	private static readonly Histogram<double> _cacheDuration = _meter.CreateHistogram<double>(
		ConductorTelemetry.CacheDurationMetric,
		unit: "ms",
		description: "Cache operation duration");

	public async ValueTask<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken) {

		// If HybridCache provider but service is still NoCacheQueryService, log warning
		if (conductorSettings.Cache.Provider == CacheProvider.HybridCache &&
			cache is NoCacheQueryService) {
			logger.LogWarning(
				"CacheProvider is set to 'HybridCache' yet the service is not registered. " +
				"Did you forget to call AddConductorHybridCache()?");
		}

		var queryTypeName = typeof(TRequest).Name;
		var category = context.Request.CacheCategory ?? "uncategorized";

		if (logger.IsEnabled(LogLevel.Debug)) {
			logger.LogDebug(
				"Processing cacheable query: {QueryType} (Category: {Category}, CacheKey: {CacheKey})",
				queryTypeName,
				category,
				context.Request.CacheKey);
		}

		var effectiveSettings = this.BuildEffectiveSettings(context.Request, queryTypeName);
		var tags = context.Request.CacheTags;

		var startTime = Stopwatch.GetTimestamp();

		var result = await cache.GetOrCreateAsync(
			context.Request.CacheKey,
			async ct => await next(ct),
			effectiveSettings,
			tags,
			cancellationToken);

		var elapsed = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

		var telemetryTags = new TagList {
			{ ConductorTelemetry.QueryNameTag, queryTypeName },
			{ ConductorTelemetry.QueryCategoryTag, category },
			{ ConductorTelemetry.QueryStatusTag, result.IsSuccess ? "success" : "failure" }
		};

		_cacheDuration.Record(elapsed, telemetryTags);

		if (logger.IsEnabled(LogLevel.Debug)) {
			logger.LogDebug(
				"Query {QueryType} completed: Status={Status}, Category={Category}, Duration={Duration}ms",
				queryTypeName,
				result.IsSuccess ? "Success" : "Failed",
				category,
				elapsed);
		}

		return result;
	}

	private QueryCacheSettings BuildEffectiveSettings(TRequest request, string queryTypeName) {
		var querySettings = request.Cache;
		var cacheOptions = conductorSettings.Cache;

		var expiration = querySettings.Expiration;
		var localExpiration = querySettings.LocalExpiration;
		var failureExpiration = querySettings.FailureExpiration;

		// Apply category-based overrides if category is specified
		if (request.CacheCategory is not null &&
			cacheOptions.CategoryOverrides.TryGetValue(request.CacheCategory, out var categoryOverrides)) {
			expiration = categoryOverrides.Expiration ?? expiration;
			localExpiration = categoryOverrides.LocalExpiration ?? localExpiration;
			failureExpiration = categoryOverrides.FailureExpiration ?? failureExpiration;

			if (logger.IsEnabled(LogLevel.Debug)) {
				logger.LogDebug("Applied category override '{Category}' for {QueryType}",
					request.CacheCategory, queryTypeName);
			}
		}

		// Apply exact query-specific overrides (highest priority)
		if (cacheOptions.QueryOverrides.TryGetValue(queryTypeName, out var queryOverrides)) {
			expiration = queryOverrides.Expiration ?? expiration;
			localExpiration = queryOverrides.LocalExpiration ?? localExpiration;
			failureExpiration = queryOverrides.FailureExpiration ?? failureExpiration;

			if (logger.IsEnabled(LogLevel.Debug)) {
				logger.LogDebug("Applied exact override for {QueryType}", queryTypeName);
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