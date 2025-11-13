namespace Cirreum.Messaging;

using Cirreum.Messaging.Options;

/// <summary>
/// Configuration options for the distributed message sender, 
/// defining the messaging provider and default destinations for messages.
/// </summary>
public class SenderOptions {

	/// <summary>
	/// The key name of the section in configuration.
	/// </summary>
	public const string ConfigurationName = "Sender";

	/// <summary>
	/// Gets or sets the name of the messaging provider used for sending messages.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Default: <c>"empty"</c> (a no-op provider that logs requests instead of sending actual messages).
	/// </para>
	/// <para>
	/// To enable message distribution, set this to a valid and configured provider instance name 
	/// (e.g., <c>"app-primary"</c> or <c>"app-archiver"</c>) from the <c>Messaging:Providers</c> 
	/// section in appsettings.
	/// </para>
	/// <para>
	/// The provider name should match a registered instance within a messaging provider configuration 
	/// (e.g., under <c>Messaging:Providers:AzureServiceBus</c> or <c>Messaging:Providers:AWS</c> etc.).
	/// </para>
	/// <para>
	/// Example appsettings.json:
	/// <code>
	/// {
	///   "Messaging": {
	///     "Providers": {
	///       "Azure": {
	///			"Instances": {
	///			  "app-primary": {
	///				"Name": "app-messaging-servicebus-1",
	///				"HealthChecks": true
	///           }
	///         }
	///       },
	///       "AWSXXX": {
	///         "app-archiver": {
	///           "Name": "app-messaging-esb-1",
	///           "HealthChecks": false
	///         }
	///       }
	///     },
	///     "Distribution": {
	///       "Sender": {
	///         "InstanceKey": "app-primary",
	///         "QueueName": "app.messages.queue",
	///         "TopicName": "app.messages.topic"
	///       }
	///     }
	///   }
	/// }
	/// </code>
	/// </para>
	/// </remarks>
	public string? InstanceKey { get; set; } = "missing";

	/// <summary>
	/// Gets or sets the name of the queue where messages with <see cref="MessageTarget.Queue"/> are published.
	/// </summary>
	/// <remarks>
	/// This queue is used for messages that are intended to be processed by a single consumer.
	/// Typically used for command-like messages that trigger specific actions or workflows.
	/// </remarks>
	public string QueueName { get; set; } = DistributionOptions.DefaultQueueName;

	/// <summary>
	/// Gets or sets the name of the topic where messages with <see cref="MessageTarget.Topic"/> are broadcast.
	/// </summary>
	/// <remarks>
	/// This topic is used for broadcasting messages to multiple subscribers 
	/// in a publish-subscribe messaging pattern. Typically used for event-like messages
	/// that multiple systems might be interested in.
	/// </remarks>
	public string TopicName { get; set; } = DistributionOptions.DefaultTopicName;

	/// <summary>
	/// Gets or sets the configuration options for background message delivery.
	/// </summary>
	/// <remarks>
	/// <para>
	/// These settings control how messages are delivered to the messaging infrastructure
	/// when background delivery is used, either by default or for specific messages.
	/// </para>
	/// <para>
	/// Individual messages can specify their delivery mode using <see cref="DistributedMessage.UseBackgroundDelivery"/>.
	/// When that property is null, the <see cref="BackgroundDeliveryOptions.UseBackgroundDeliveryByDefault"/> setting
	/// determines the delivery mode.
	/// </para>
	/// <para>
	/// All background delivery settings are required regardless of the default mode, as
	/// any message can opt into background delivery via its individual setting.
	/// </para>
	/// <para>
	/// Use these options to fine-tune background delivery for different scenarios:
	/// <list type="bullet">
	///   <item>High-volume environments can benefit from larger queue capacities and batch sizes</item>
	///   <item>Mission-critical applications may prefer synchronous delivery for stronger consistency</item>
	///   <item>Mixed workloads can be optimized by adjusting batch intervals</item>
	/// </list>
	/// </para>
	/// <para>
	/// Example configuration in appsettings.json:
	/// <code>
	/// "Distribution": {
	///   "Sender": {
	///     "InstanceKey": "app-primary",
	///     "QueueName": "app.messages.queue",
	///     "TopicName": "app.messages.topic",
	///     "BackgroundDelivery": {
	///       "UseBackgroundDeliveryByDefault": false,
	///       "QueueCapacity": 1000,
	///       "BatchCapacity": 10,
	///       "BatchingInterval": "00:00:00.050" // 50ms
	///     }
	///   }
	/// }
	/// </code>
	/// </para>
	/// </remarks>
	public BackgroundDeliveryOptions BackgroundDelivery { get; set; }
		= new BackgroundDeliveryOptions();

}