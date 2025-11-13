namespace Cirreum;

/// <summary>
/// Provides a registry for encryption algorithm kind identifiers to prevent conflicts and enable discovery.
/// </summary>
/// <remarks>
/// <para>
/// This static class manages the allocation and registration of single-character algorithm kind 
/// identifiers used by <see cref="IStateContainerEncryption"/> implementations. It ensures that 
/// each encryption algorithm type has a unique identifier and provides human-readable descriptions 
/// for debugging and error messages.
/// </para>
/// <para>
/// <strong>Built-in Algorithms:</strong> The framework reserves identifiers 'a', 'b', and 'c' 
/// for built-in encryption algorithms. Custom implementations should use identifiers 'd' through 'z'.
/// </para>
/// <para>
/// <strong>Registration Strategy:</strong> Algorithm kinds should be registered during application 
/// startup or in static constructors to ensure they are available before any encryption operations occur.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a custom encryption algorithm
/// StateEncryptionKinds.Register('d', "AES-256 Encryption");
/// 
/// // Use in custom encryption implementation
/// public class AesEncryption : IStateContainerEncryption {
///     public char AlgorithmKindId => 'd';
///     // ... implementation
/// }
/// 
/// // Get next available ID for dynamic registration
/// var nextId = StateEncryptionKinds.GetNextAvailableId();
/// StateEncryptionKinds.Register(nextId, "Dynamic Algorithm");
/// </code>
/// </example>
public static class StateEncryptionKinds {

	private static readonly Dictionary<char, string> _registry = [];
	private static readonly HashSet<char> _reserved = [];

	/// <summary>
	/// Algorithm kind identifier for no encryption (plaintext storage).
	/// </summary>
	/// <value>The character 'a'.</value>
	public const char NONE = 'a';

	/// <summary>
	/// Algorithm kind identifier for Base64 obfuscation.
	/// </summary>
	/// <value>The character 'b'.</value>
	public const char BASE64 = 'b';

	/// <summary>
	/// Algorithm kind identifier for XOR obfuscation algorithms.
	/// </summary>
	/// <value>The character 'c'.</value>
	public const char XOR = 'c';

	static StateEncryptionKinds() {
		Register(NONE, "None (No Encryption)");
		Register(BASE64, "Base64 Obfuscation");
		Register(XOR, "XOR Obfuscation");
	}

	/// <summary>
	/// Registers a custom algorithm kind identifier with a human-readable description.
	/// </summary>
	/// <param name="kindId">The single character identifier for the algorithm kind. Must be between 'd' and 'z' for custom algorithms.</param>
	/// <param name="description">A human-readable description of the encryption algorithm type.</param>
	/// <exception cref="InvalidOperationException">Thrown when the specified <paramref name="kindId"/> is already registered.</exception>
	/// <remarks>
	/// <para>
	/// Custom encryption implementations should register their algorithm kind during application 
	/// startup to ensure the identifier is reserved and to provide meaningful descriptions in 
	/// error messages and debugging scenarios.
	/// </para>
	/// <para>
	/// <strong>Identifier Range:</strong> Characters 'a', 'b', and 'c' are reserved for built-in 
	/// algorithms. Custom implementations should use 'd' through 'z'.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Register during application startup
	/// StateEncryptionKinds.Register('d', "AES-256 Encryption");
	/// StateEncryptionKinds.Register('e', "Custom Company Algorithm");
	/// 
	/// // Use in encryption implementation
	/// public class MyCustomEncryption : IStateContainerEncryption {
	///     static MyCustomEncryption() {
	///         StateEncryptionKinds.Register('f', "My Custom Algorithm");
	///     }
	///     public char AlgorithmKindId => 'f';
	/// }
	/// </code>
	/// </example>
	public static void Register(char kindId, string description) {
		if (_reserved.Contains(kindId)) {
			throw new InvalidOperationException($"Algorithm Kind ID '{kindId}' is already registered as: {_registry[kindId]}");
		}
		_registry[kindId] = description;
		_reserved.Add(kindId);
	}

	/// <summary>
	/// Gets the human-readable description for the specified algorithm kind identifier.
	/// </summary>
	/// <param name="kindId">The algorithm kind identifier to look up.</param>
	/// <returns>
	/// The description if the identifier is registered; otherwise, a message indicating 
	/// the algorithm is unknown.
	/// </returns>
	/// <remarks>
	/// This method is useful for generating error messages and debugging information that 
	/// include meaningful algorithm names instead of just character identifiers.
	/// </remarks>
	/// <example>
	/// <code>
	/// var description = StateEncryptionKinds.GetDescription('b');
	/// // Returns: "Base64 Obfuscation"
	/// 
	/// var unknown = StateEncryptionKinds.GetDescription('z');
	/// // Returns: "Unknown algorithm 'z'"
	/// </code>
	/// </example>
	public static string GetDescription(char kindId) {
		return _registry.TryGetValue(kindId, out var description)
			? description
			: $"Unknown algorithm '{kindId}'";
	}

	/// <summary>
	/// Gets all currently registered algorithm kind identifiers.
	/// </summary>
	/// <returns>An enumerable collection of all registered algorithm kind characters.</returns>
	/// <remarks>
	/// This method is useful for diagnostic purposes, error reporting, and determining 
	/// which algorithm kinds are available in the current application configuration.
	/// </remarks>
	/// <example>
	/// <code>
	/// var registeredAlgorithms = StateEncryptionKinds.GetRegisteredAlgorithms();
	/// Console.WriteLine($"Available algorithms: {string.Join(", ", registeredAlgorithms)}");
	/// // Output: "Available algorithms: a, b, c, d"
	/// </code>
	/// </example>
	public static IEnumerable<char> GetRegisteredAlgorithms() => _reserved;

	/// <summary>
	/// Finds the next available algorithm kind identifier for custom implementations.
	/// </summary>
	/// <returns>The next available character identifier between 'd' and 'z'.</returns>
	/// <exception cref="InvalidOperationException">Thrown when all available identifiers (d-z) have been used.</exception>
	/// <remarks>
	/// <para>
	/// This method searches for the first unregistered identifier in the range 'd' through 'z' 
	/// and returns it for use by custom encryption implementations. This is particularly useful 
	/// for dynamic algorithm registration or when you don't want to manually track which 
	/// identifiers are available.
	/// </para>
	/// <para>
	/// <strong>Note:</strong> This method only finds an available identifier; you still need 
	/// to call <see cref="Register"/> to actually reserve it.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Find and register the next available algorithm
	/// var nextId = StateEncryptionKinds.GetNextAvailableId();
	/// StateEncryptionKinds.Register(nextId, "My Dynamic Algorithm");
	/// 
	/// // Use in encryption implementation
	/// public class DynamicEncryption : IStateContainerEncryption {
	///     public char AlgorithmKindId { get; }
	///     
	///     public DynamicEncryption() {
	///         AlgorithmKindId = StateEncryptionKinds.GetNextAvailableId();
	///         StateEncryptionKinds.Register(AlgorithmKindId, "Dynamic Encryption");
	///     }
	/// }
	/// </code>
	/// </example>
	public static char GetNextAvailableId() {
		for (var c = 'd'; c <= 'z'; c++) {
			if (!_reserved.Contains(c)) {
				return c;
			}
		}
		throw new InvalidOperationException("No available algorithm IDs remaining");
	}

}