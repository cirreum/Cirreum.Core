namespace Cirreum.Messaging;

using System.Text.Json.Serialization;

/// <summary>
/// Specifies the infrastructure target for message delivery.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageTarget {
	/// <summary>
	/// Deliver to a queue for single-consumer processing.
	/// </summary>
	Queue = 0,

	/// <summary>
	/// Deliver to a topic for multi-subscriber distribution.
	/// </summary>
	Topic = 1
}