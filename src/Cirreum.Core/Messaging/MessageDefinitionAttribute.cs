namespace Cirreum.Messaging;

/// <summary>
/// Defines the metadata for a distributed message.
/// </summary>
/// <remarks>
/// <para>
/// This attribute must be applied to all implementations of <see cref="DistributedMessage"/>
/// to provide the stable identifier, version information, and delivery target for the message.
/// </para>
/// <para>
/// The identifier establishes a stable logical identity for the message that remains
/// consistent across all versions, enabling proper routing and handling.
/// </para>
/// <para>
/// The version indicates the schema version of the message, which should be incremented
/// whenever the message structure changes in a way that affects compatibility. This enables
/// consumers to handle different versions appropriately.
/// </para>
/// <para>
/// The target specifies the infrastructure destination (queue or topic) where the message
/// should be delivered.
/// </para>
/// <para>
/// <b>Important:</b> Once a message version is deployed to production, it should never be modified.
/// Instead, create a new message class with an incremented version number while keeping the same identifier.
/// </para>
/// </remarks>
/// <param name="identifier">The stable logical identifier for this message type (e.g., "race.completed").</param>
/// <param name="version">The schema version of the message, which can be numeric or semantic (e.g., "1" or "1.0.0").</param>
/// <param name="target">The infrastructure target specifying whether the message is delivered to a queue or topic.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MessageDefinitionAttribute(string identifier, string version, MessageTarget target) : Attribute {

	/// <summary>
	/// Gets the logical identifier of the message.
	/// </summary>
	/// <value>
	/// A stable identifier that remains consistent across all versions of this message type.
	/// </value>
	/// <remarks>
	/// This identifier provides a stable identity for the message type that persists even
	/// as the schema evolves through different versions, enabling consistent routing and handling
	/// across system boundaries.
	/// </remarks>
	public string Identifier { get; } = identifier;

	/// <summary>
	/// Gets the schema version of the message.
	/// </summary>
	/// <value>
	/// The version number or semantic version string for this message schema.
	/// </value>
	/// <remarks>
	/// The version indicates the schema version of the message class. It should be incremented
	/// from any previous message class that shares the same identifier. This enables consumers
	/// to handle different schema versions appropriately and maintain backward compatibility.
	/// </remarks>
	public string Version { get; } = version;

	/// <summary>
	/// Gets the infrastructure target for message delivery.
	/// </summary>
	/// <value>
	/// A <see cref="MessageTarget"/> value indicating whether the message is delivered to a queue or topic.
	/// </value>
	/// <remarks>
	/// The target determines the delivery infrastructure: <see cref="MessageTarget.Queue"/> for
	/// point-to-point single-consumer processing, or <see cref="MessageTarget.Topic"/> for
	/// publish-subscribe multi-subscriber distribution.
	/// </remarks>
	public MessageTarget Target { get; } = target;
}