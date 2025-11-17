namespace Cirreum.Conductor.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Concrete wrapper implementation for typed notifications.
/// </summary>
internal sealed class NotificationHandlerWrapperImpl<TNotification>
	: NotificationHandlerWrapper
	where TNotification : INotification {

	private static readonly ConcurrentDictionary<Type, PublisherStrategy?> _strategyCache = new();
	private static readonly string notificationTypeName = typeof(TNotification).Name;

	public override async Task<Result> Handle(
		Publisher publisher,
		ILogger logger,
		INotification notification,
		IServiceProvider serviceProvider,
		PublisherStrategy? strategy,
		PublisherStrategy defaultStrategy,
		CancellationToken cancellationToken) {

		var typedNotification = (TNotification)notification;
		var notificationType = typeof(TNotification);

		// ----- 0. START TIMING & ACTIVITY -----
		var activity = NotificationTelemetry.StartActivity(notificationTypeName);
		var startTimestamp = Timing.Start();

		try {
			// ----- 1. CHECK CANCELLATION -----
			cancellationToken.ThrowIfCancellationRequested();

			// ----- 2. RESOLVE HANDLERS -----
			var handlers = serviceProvider
				.GetServices<INotificationHandler<TNotification>>()
				.ToList();

			if (handlers.Count == 0) {
				PublisherLogger.NoHandlersRegistered(logger, notificationTypeName);
				NotificationTelemetry.RecordNoHandlers(notificationTypeName);
				return Result.Success;
			}

			// ----- 3. DETERMINE STRATEGY -----
			PublisherStrategy effectiveStrategy;
			if (strategy.HasValue) {
				effectiveStrategy = strategy.Value;
			} else {
				var attributeStrategy = _strategyCache.GetOrAdd(
					notificationType,
					static nt => nt.GetCustomAttribute<PublishingStrategyAttribute>()?.Strategy);
				effectiveStrategy = attributeStrategy ?? defaultStrategy;
			}

			PublisherLogger.Publishing(logger, notificationTypeName, handlers.Count, effectiveStrategy);

			// ----- 4. CHECK CANCELLATION AGAIN -----
			cancellationToken.ThrowIfCancellationRequested();

			// ----- 5. PUBLISH -----
			var result = effectiveStrategy switch {
				PublisherStrategy.Sequential =>
					await publisher.PublishSequentialAsync(typedNotification, handlers, false, cancellationToken),
				PublisherStrategy.FailFast =>
					await publisher.PublishSequentialAsync(typedNotification, handlers, true, cancellationToken),
				PublisherStrategy.Parallel =>
					await publisher.PublishParallelAsync(typedNotification, handlers, cancellationToken),
				PublisherStrategy.FireAndForget =>
					await publisher.PublishFireAndForgetAsync(typedNotification, handlers),
				_ => Result.Fail(
					new InvalidOperationException($"Unknown publisher strategy: {effectiveStrategy}"))
			};

			// ----- 6. RECORD TELEMETRY -----
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			if (result.IsSuccess) {
				NotificationTelemetry.SetActivitySuccess(activity);
				NotificationTelemetry.RecordSuccess(
					notificationTypeName,
					effectiveStrategy,
					handlers.Count,
					elapsed);
			} else {
				NotificationTelemetry.SetActivityError(activity, result.Error);
				NotificationTelemetry.RecordFailure(
					notificationTypeName,
					effectiveStrategy,
					handlers.Count,
					elapsed,
					result.Error);
			}

			return result;

		} catch (OperationCanceledException oce) {
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			NotificationTelemetry.SetActivityCanceled(activity, oce);
			NotificationTelemetry.RecordCanceled(notificationTypeName, elapsed, oce);

			throw;

		} catch (Exception ex) {
			var elapsed = Timing.GetElapsedMilliseconds(startTimestamp);

			NotificationTelemetry.SetActivityError(activity, ex);
			NotificationTelemetry.RecordFailure(
				notificationTypeName,
				strategy ?? defaultStrategy,
				0,
				elapsed,
				ex);

			return Result.Fail(ex);

		} finally {
			NotificationTelemetry.StopActivity(activity);
		}
	}
}