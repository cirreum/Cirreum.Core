namespace Cirreum.Messaging;

using Cirreum.Conductor;

/// <summary>
/// Handles distributed messages by publishing them to external systems via a transport publisher.
/// </summary>
/// <typeparam name="TMessage">The type of message to handle, which must implement <see cref="DistributedMessage"/>.</typeparam>
/// <param name="transportPublisher">The <see cref="IDistributedTransportPublisher"/> service.</param>
/// <remarks>
/// <para>
/// This handler intercepts any Conductor notification that extends <see cref="DistributedMessage"/>
/// and automatically forwards it to the configured message transport for external distribution.
/// </para>
/// <para>
/// The handler acts as a bridge between the application's internal notification system (Conductor)
/// and external messaging infrastructure, ensuring consistent delivery of distributed messages
/// based on their configuration settings.
/// </para>
/// </remarks>
public sealed class DistributedMessageHandler<TMessage>(
	IDistributedTransportPublisher transportPublisher
) : INotificationHandler<TMessage>
	where TMessage : notnull, DistributedMessage {

	/// <summary>
	/// Handles the message by publishing it to the external messaging system.
	/// </summary>
	/// <param name="message">The <see cref="DistributedMessage"/> to be distributed externally.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// This method forwards the message to the <see cref="IDistributedTransportPublisher.PublishMessageAsync{T}"/>
	/// method, which will handle delivery based on the message's configuration and destination type.
	/// </remarks>
	public async Task HandleAsync(TMessage message, CancellationToken cancellationToken) {
		await transportPublisher.PublishMessageAsync(message, cancellationToken);
	}
}