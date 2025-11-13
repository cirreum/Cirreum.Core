namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;

/// <summary>
/// A no-operation publisher that logs messages but does not send them to any external system.
/// </summary>
/// <remarks>
/// This implementation is useful for testing, development environments, or when external
/// message distribution needs to be temporarily disabled without changing application code.
/// </remarks>
public sealed class EmptyTransportPublisher(
	ILogger<EmptyTransportPublisher> logger)
	: IDistributedTransportPublisher {

	/// <summary>
	/// Logs the receipt of a message and returns a completed task without sending the message.
	/// </summary>
	/// <typeparam name="T">The type of distributed message.</typeparam>
	/// <param name="message">The message that would normally be published.</param>
	/// <param name="ct">A cancellation token to observe while performing the operation.</param>
	/// <returns>A completed task representing the asynchronous operation.</returns>
	/// <remarks>
	/// This method logs a warning with the message type and publisher name to provide visibility
	/// that the message was received but not actually published to any external system.
	/// </remarks>
	public Task PublishMessageAsync<T>(T message, CancellationToken ct) where T : DistributedMessage {
		if (logger.IsEnabled(LogLevel.Warning)) {
			logger.LogWarning("{Publisher} received Message {MessageType} - not published",
				nameof(EmptyTransportPublisher), message.GetType().Name);
		}
		return Task.CompletedTask;
	}

}