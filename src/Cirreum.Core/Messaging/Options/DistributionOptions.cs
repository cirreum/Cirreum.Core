namespace Cirreum.Messaging;

/// <summary>
/// The options class to configure distributed messaging.
/// </summary>
public class DistributionOptions {

	/// <summary>
	/// The key name of the section.
	/// </summary>
	public const string ConfigurationName = "Distribution";

	/// <summary>
	/// The name of the <see cref="Sender"/>'s configured instance (<see cref="SenderOptions.InstanceKey"/>)
	/// to use for sending messages.
	/// </summary>
	public readonly static string SenderInstanceConfigurationName
		= $"{SenderOptions.ConfigurationName}:{nameof(SenderOptions.InstanceKey)}";

	/// <summary>
	/// An optional default queue name.
	/// </summary>
	public const string DefaultQueueName = "App.DistributedEvents.v1";
	/// <summary>
	/// An optional default topic name.
	/// </summary>
	public const string DefaultTopicName = "App.DistributedNotifications.v1";

	/// <summary>
	/// The configured sender options.
	/// </summary>
	public SenderOptions Sender { get; set; } = new();

}