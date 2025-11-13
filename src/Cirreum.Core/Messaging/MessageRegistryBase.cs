namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>
/// An abstract default implementation of the <see cref="IMessageRegistry"/> service.
/// </summary>
/// <param name="logger">The <see cref="ILogger"/> to use.</param>
/// <remarks>
/// <para>
/// In a consuming application, define a class that inherits from this class and
/// during startup call <see cref="DefaultInitializationAsync"/>.
/// </para>
/// </remarks>
public abstract class MessageRegistryBase(
	ILogger logger
) : IMessageRegistry {

	protected internal static readonly ConcurrentDictionary<string, MessageDefinition> _messages = new();

	protected ILogger _logger = logger;
	protected bool _initialized;

	/// <summary>
	/// Initializes the registry by scanning all assemblies for concrete types that inherit from
	/// <see cref="DistributedMessage"/>.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/>.</returns>
	protected ValueTask DefaultInitializationAsync() {

		if (this._initialized) {
			this._logger.LogWarning("InitializeAsync was called more than once. Ignoring subsequent calls.");
			return ValueTask.CompletedTask; // Prevent reinitialization
		}
		this._initialized = true;

		var scannedMessages = MessageScanner.ScanAssemblies(this._logger);

		foreach (var messageDefinition in scannedMessages) {
			_messages.TryAdd(messageDefinition.MessageType, messageDefinition);
		}

		return ValueTask.CompletedTask;

	}

	/// <inheritdoc/>
	public MessageDefinition GetDefinitionFor<T>() where T : DistributedMessage =>
		this.GetDefinitionFor(typeof(T));

	/// <inheritdoc/>
	public MessageDefinition GetDefinitionFor(Type messageType) =>
		this.GetDefinitionFor(messageType.FullName!);

	/// <inheritdoc/>
	public MessageDefinition GetDefinitionFor(string messageTypeFullName) {
		if (_messages.TryGetValue(messageTypeFullName, out var definition)) {
			return definition;
		}

		throw new InvalidOperationException(
			$"No definition found for {messageTypeFullName}. " +
			$"Make sure it has a [{nameof(MessageDefinitionAttribute)}] attribute applied.");
	}

	/// <inheritdoc/>
	public IReadOnlyCollection<MessageDefinition> GetAll() {
		return _messages.Values.ToList().AsReadOnly();
	}

}