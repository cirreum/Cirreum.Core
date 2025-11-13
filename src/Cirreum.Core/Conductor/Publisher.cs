namespace Cirreum.Conductor;

using Cirreum.Conductor.Internal;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Default publisher that sends notifications to all registered handlers.
/// Supports parallel, sequential and fire-and-forget publishing.
/// </summary>
public sealed class Publisher(
	IServiceProvider serviceProvider,
	PublisherStrategy defaultStrategy,
	ILogger<Publisher> logger
) : IPublisher {

	private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> _wrapperCache = new();

	public Task<Result> PublishAsync<TNotification>(
		TNotification notification,
		PublisherStrategy? strategy = null,
		CancellationToken cancellationToken = default)
		where TNotification : INotification {

		ArgumentNullException.ThrowIfNull(notification);

		var notificationType = notification.GetType(); // Runtime type is correct here!

		var wrapper = _wrapperCache.GetOrAdd(notificationType, static nt => {
			var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(nt);
			return (NotificationHandlerWrapper)(Activator.CreateInstance(wrapperType)
				?? throw new InvalidOperationException($"Could not create wrapper for {nt.Name}"));
		});

		return wrapper.Handle(
			this,
			logger,
			notification,
			serviceProvider,
			strategy,
			defaultStrategy,
			cancellationToken);

	}

	internal async Task<Result> PublishSequentialAsync<TNotification>(
		TNotification notification,
		List<INotificationHandler<TNotification>> handlers,
		bool stopOnFailure,
		CancellationToken cancellationToken)
		where TNotification : INotification {

		var failures = new List<(Type HandlerType, Exception Error)>();

		foreach (var handler in handlers) {
			var handlerType = handler.GetType();
			try {
				await handler.HandleAsync(notification, cancellationToken);
			} catch (Exception ex) {
				PublisherLogger.HandlerThrewException(logger, handlerType, ex);
				failures.Add((handlerType, ex));

				if (stopOnFailure) {
					break;
				}
			}
		}

		if (failures.Count == 0) {
			return Result.Success;
		}

		var message = $"One or more notification handlers failed: {string.Join(", ", failures.Select(f => f.HandlerType.Name))}";
		return Result.Fail(new AggregateException(message, failures.Select(f => f.Error)));
	}

	internal async Task<Result> PublishParallelAsync<TNotification>(
		TNotification notification,
		List<INotificationHandler<TNotification>> handlers,
		CancellationToken cancellationToken)
		where TNotification : INotification {

		var tasks = handlers
			.Select(h => this.InvokeHandlerAsync(h, notification, cancellationToken))
			.ToArray();

		var results = await Task
			.WhenAll(tasks)
			.ConfigureAwait(false);

		var failures = results
			.Where(r => !r.IsSuccess)
			.Select(r => r.Error!)
			.ToList();

		if (failures.Count == 0) {
			return Result.Success;
		}

		var message = $"One or more notification handlers failed: {string.Join(", ", failures.Select(e => e.GetType().Name))}";
		return Result.Fail(new AggregateException(message, failures));
	}

	internal Task<Result> PublishFireAndForgetAsync<TNotification>(
		TNotification notification,
		List<INotificationHandler<TNotification>> handlers)
		where TNotification : INotification {

		_ = Task.Run(async () => {
			foreach (var handler in handlers) {
				try {
					await handler.HandleAsync(notification, CancellationToken.None);
				} catch (Exception ex) {
					var handlerType = handler.GetType();
					PublisherLogger.HandlerFailedFireAndForget(logger, handlerType, ex);
				}
			}
		}, CancellationToken.None);

		return Task.FromResult(Result.Success);
	}

	private async Task<(bool IsSuccess, Exception? Error)> InvokeHandlerAsync<TNotification>(
		INotificationHandler<TNotification> handler,
		TNotification notification,
		CancellationToken cancellationToken)
		where TNotification : INotification {

		var handlerType = handler.GetType();

		try {
			await handler.HandleAsync(notification, cancellationToken);
			return (true, null);
		} catch (Exception ex) {
			PublisherLogger.HandlerThrewException(logger, handlerType, ex);
			return (false, ex);
		}
	}

}