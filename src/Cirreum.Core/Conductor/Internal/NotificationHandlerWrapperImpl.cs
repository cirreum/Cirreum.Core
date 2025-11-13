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

		// Resolve handlers for this specific notification type
		var handlers = serviceProvider
			.GetServices<INotificationHandler<TNotification>>()
			.ToList();

		if (handlers.Count == 0) {
			PublisherLogger.NoHandlersRegistered(logger, notificationType.Name);
			return Result.Success;
		}

		// Determine effective strategy (with short-circuit evaluation)
		PublisherStrategy effectiveStrategy;
		if (strategy.HasValue) {
			effectiveStrategy = strategy.Value;
		} else {
			var attributeStrategy = _strategyCache.GetOrAdd(
				notificationType,
				static nt => nt.GetCustomAttribute<PublishingStrategyAttribute>()?.Strategy);
			effectiveStrategy = attributeStrategy ?? defaultStrategy;
		}

		PublisherLogger.Publishing(logger, notificationType.Name, handlers.Count, effectiveStrategy);

		return effectiveStrategy switch {
			PublisherStrategy.Sequential => await publisher.PublishSequentialAsync(typedNotification, handlers, false, cancellationToken),
			PublisherStrategy.FailFast => await publisher.PublishSequentialAsync(typedNotification, handlers, true, cancellationToken),
			PublisherStrategy.Parallel => await publisher.PublishParallelAsync(typedNotification, handlers, cancellationToken),
			PublisherStrategy.FireAndForget => await publisher.PublishFireAndForgetAsync(typedNotification, handlers),
			_ => Result.Fail($"Unknown publisher strategy: {effectiveStrategy}")
		};

	}

}