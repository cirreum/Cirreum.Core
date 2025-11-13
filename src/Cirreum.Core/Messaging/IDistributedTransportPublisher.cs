namespace Cirreum.Messaging;

/// <summary>
/// Defines a publisher for distributing messages using a messaging system such
/// as Azure ServiceBus, AWS SNS/SQS, RabbitMQ, etc.
/// </summary>
/// <remarks>
/// This interface defines a service for sending messages outside of the application. It handles the
/// transport-level concerns of message distribution, abstracting away the underlying message delivery
/// service provider.
/// </remarks>
public interface IDistributedTransportPublisher {
	/// <summary>
	/// Sends a distributed message asynchronously.
	/// </summary>
	/// <typeparam name="T">The type of the message, which must inherit from <see cref="DistributedMessage"/>.</typeparam>
	/// <param name="message">The message to send.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task PublishMessageAsync<T>(T message, CancellationToken cancellationToken) where T : DistributedMessage;
}