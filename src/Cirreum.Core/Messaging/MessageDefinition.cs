namespace Cirreum.Messaging;

/// <summary>
/// Represents the metadata definition of a distributed message type.
/// </summary>
/// <remarks>
/// This record captures the complete schema definition of a distributed message,
/// including its identifier, version, routing target, runtime type information,
/// and property schema. It is typically used for message registration, validation,
/// and schema discovery.
/// </remarks>
/// <param name="Identifier">The stable logical identifier for the message type (e.g., "race.completed").</param>
/// <param name="Version">The schema version of the message (e.g., "1" or "1.0.0").</param>
/// <param name="Target">The infrastructure target specifying whether the message is delivered to a queue or topic.</param>
/// <param name="MessageType">The fully qualified CLR type name of the message (e.g., "MyApp.Messages.RaceCompleted").</param>
/// <param name="Schema">The collection of properties that define the message's data structure.</param>
public record MessageDefinition(
	string Identifier,
	string Version,
	MessageTarget Target,
	string MessageType,
	List<MessageProperty> Schema
);