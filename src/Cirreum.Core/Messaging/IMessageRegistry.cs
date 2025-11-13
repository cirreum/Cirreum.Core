namespace Cirreum.Messaging;

/// <summary>
/// Provides access to message definitions for use with distributed messaging.
/// </summary>
/// <remarks>
/// <para>
/// This registry is responsible for providing access to message metadata (identifier and version)
/// for all registered <see cref="DistributedMessage"/> implementations. It serves as a central
/// repository of message definitions that can be used at runtime without instantiating message objects.
/// </para>
/// <para>
/// The registry is typically populated during application startup by scanning assemblies for classes
/// that inherit from <see cref="DistributedMessage"/> and are decorated with the 
/// <see cref="MessageDefinitionAttribute"/>. This information is cached to provide efficient lookups
/// during message processing.
/// </para>
/// <para>
/// This registry is essential for operations like:
/// <list type="bullet">
///   <item>Creating message envelopes without instantiating message objects</item>
///   <item>Validating message schemas during development and runtime</item>
///   <item>Supporting administrative tools for message management</item>
///   <item>Enabling schema versioning and compatibility checks</item>
/// </list>
/// </para>
/// </remarks>
public interface IMessageRegistry {
	/// <summary>
	/// Gets the message definition for the specified message type.
	/// </summary>
	/// <typeparam name="T">The type of message to get the definition for.</typeparam>
	/// <returns>The found <see cref="MessageDefinition"/>.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when no definition is found for the specified message type, which typically indicates
	/// the type is not decorated with <see cref="MessageDefinitionAttribute"/> or was not registered
	/// during initialization.
	/// </exception>
	MessageDefinition GetDefinitionFor<T>() where T : DistributedMessage;
	/// <summary>
	/// Gets the message definition for the specified message type.
	/// </summary>
	/// <param name="messageType">The type of message to get the definition for.</param>
	/// <returns>The found <see cref="MessageDefinition"/>.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the specified type does not inherit from <see cref="DistributedMessage"/>.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when no definition is found for the specified message type, which typically indicates
	/// the type is not decorated with <see cref="MessageDefinitionAttribute"/> or was not registered
	/// during initialization.
	/// </exception>
	MessageDefinition GetDefinitionFor(Type messageType);
	/// <summary>
	/// Gets the message definition for the specified message type.
	/// </summary>
	/// <param name="messageTypeFullName">The type full-name of the message to get the definition for.</param>
	/// <returns>The found <see cref="MessageDefinition"/>.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the specified type does not inherit from <see cref="DistributedMessage"/>.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when no definition is found for the specified message type, which typically indicates
	/// the type is not decorated with <see cref="MessageDefinitionAttribute"/> or was not registered
	/// during initialization.
	/// </exception>
	MessageDefinition GetDefinitionFor(string messageTypeFullName);
	/// <summary>
	/// Get all registered message definitions.
	/// </summary>
	/// <returns>A readonly collection of <see cref="MessageDefinition"/>.</returns>
	IReadOnlyCollection<MessageDefinition> GetAll();
}