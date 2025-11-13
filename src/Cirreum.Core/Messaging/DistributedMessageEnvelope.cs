namespace Cirreum.Messaging;

/// <summary>
/// Defines a message envelope that wraps a serialized distributed message with metadata for delivery.
/// </summary>
public record DistributedMessageEnvelope {

	/// <summary>
	/// Initializes a new instance of the <see cref="DistributedMessageEnvelope"/> class.
	/// This constructor is provided for serialization frameworks.
	/// </summary>
	public DistributedMessageEnvelope() {
		this.SerializedMessage = string.Empty;
		this.MessageIdentifier = string.Empty;
		this.MessageVersion = string.Empty;
		this.MessageType = string.Empty;
		this.ProducerId = string.Empty;
	}

	/// <summary>
	/// Internal constructor used by the Create method.
	/// </summary>
	private DistributedMessageEnvelope(
		string serializedMessage,
		string messageDefinition,
		string messageVersion,
		string messageType,
		string producerId) {
		this.SerializedMessage = serializedMessage;
		this.MessageIdentifier = messageDefinition;
		this.MessageVersion = messageVersion;
		this.ProducerId = producerId;
		this.MessageType = messageType;
	}

	/// <summary>
	/// Creates a new message envelope from a distributed message instance.
	/// Uses <c>System.Text.Json</c> for serialization.
	/// </summary>
	/// <typeparam name="TMessage">The type of message being wrapped.</typeparam>
	/// <param name="message">The message payload to be distributed.</param>
	/// <param name="definition">The message definition</param>
	/// <param name="producerId">The ID of the producer creating this message.</param>
	/// <returns>A new envelope containing the serialized message and its metadata.</returns>
	public static DistributedMessageEnvelope Create<TMessage>(
		TMessage message,
		MessageDefinition definition,
		string producerId)
		where TMessage : DistributedMessage =>
		CreateWithSerializer(
			message,
			definition,
			producerId,
			(m) => System.Text.Json.JsonSerializer.Serialize(m));


	/// <summary>
	/// Creates a new message envelope from a distributed message instance using a custom serializer.
	/// </summary>
	/// <typeparam name="TMessage">The type of message being wrapped.</typeparam>
	/// <param name="definition">The message definition</param>
	/// <param name="message">The message payload to be distributed.</param>
	/// <param name="serializer">Function that performs the serialization.</param>
	/// <param name="producerId">The ID of the producer creating this message.</param>
	/// <returns>A new envelope containing the serialized message and its metadata.</returns>
	public static DistributedMessageEnvelope CreateWithSerializer<TMessage>(
		TMessage message,
		MessageDefinition definition,
		string producerId,
		Func<TMessage, string> serializer)
		where TMessage : DistributedMessage {
		return new DistributedMessageEnvelope(
			serializer(message),
			definition.Identifier,
			definition.Version,
			typeof(TMessage).FullName ?? typeof(TMessage).Name,
			producerId);
	}

	/// <summary>
	/// Deserializes a JSON string into a <see cref="DistributedMessageEnvelope"/>.
	/// </summary>
	/// <param name="json">The JSON representation of the envelope.</param>
	/// <returns>The deserialized <see cref="DistributedMessageEnvelope"/>.</returns>
	public static DistributedMessageEnvelope FromJson(string json) {
		return System.Text.Json.JsonSerializer.Deserialize<DistributedMessageEnvelope>(json)
			?? throw new InvalidOperationException("Unable to deserialize envelope from JSON.");
	}

	/// <summary>
	/// Deserializes a JSON string into a <see cref="DistributedMessageEnvelope"/> with custom JSON options.
	/// </summary>
	/// <param name="json">The JSON representation of the envelope.</param>
	/// <param name="options">Custom JSON serializer options.</param>
	/// <returns>The deserialized <see cref="DistributedMessageEnvelope"/>.</returns>
	public static DistributedMessageEnvelope FromJson(
		string json,
		System.Text.Json.JsonSerializerOptions options) {
		return System.Text.Json.JsonSerializer.Deserialize<DistributedMessageEnvelope>(json, options)
			?? throw new InvalidOperationException("Unable to deserialize envelope from JSON.");
	}

	/// <summary>
	/// Gets the serialized JSON string of the message payload.
	/// </summary>
	public string SerializedMessage { get; init; }

	/// <summary>
	/// Gets the message definition.
	/// </summary>
	public string MessageIdentifier { get; init; }

	/// <summary>
	/// Gets the Version of the message.
	/// </summary>
	public string MessageVersion { get; init; }

	/// <summary>
	/// Gets the Type of the message.
	/// This can be used for automatic type resolution during deserialization.
	/// </summary>
	public string MessageType { get; init; }

	/// <summary>
	/// Gets the ID of the producer that created this message.
	/// This can be used for routing, filtering, or auditing purposes.
	/// </summary>
	public string ProducerId { get; init; }

	/// <summary>
	/// Deserializes the message payload using the stored .NET type information.
	/// </summary>
	/// <returns>The deserialized message instance.</returns>
	public object DeserializeMessage() {
		var type =
			Type.GetType(this.MessageType)
			?? throw new InvalidOperationException($"Could not resolve type '{this.MessageType}'.");

		return System.Text.Json.JsonSerializer.Deserialize(this.SerializedMessage, type)
			?? throw new InvalidOperationException("Unable to deserialize.");
	}

	/// <summary>
	/// Deserializes the message payload using the stored .NET type information and a custom deserializer.
	/// </summary>
	/// <param name="deserializer">Function that performs the deserialization given a type and JSON string.</param>
	/// <returns>The deserialized message instance.</returns>
	public object DeserializeMessage(Func<Type, string, object> deserializer) {
		var type =
			Type.GetType(this.MessageType) ??
			throw new InvalidOperationException($"Could not resolve type '{this.MessageType}'.");

		return deserializer(type, this.SerializedMessage);
	}

	/// <summary>
	/// Deserializes the message payload to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the message to.</typeparam>
	/// <returns>The deserialized message instance.</returns>
	public T DeserializeMessage<T>() =>
		System.Text.Json.JsonSerializer.Deserialize<T>(this.SerializedMessage)
		?? throw new InvalidOperationException("Unable to deserialize.");

	/// <summary>
	/// Deserializes the message payload to the specified type using a custom deserializer.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the message to.</typeparam>
	/// <param name="deserializer">Function that performs the deserialization.</param>
	/// <returns>The deserialized message instance.</returns>
	public T DeserializeMessage<T>(Func<string, T> deserializer) {
		return deserializer(this.SerializedMessage);
	}

}