namespace Cirreum;

/// <summary>
/// Defines an encryption implementation for protecting state container values.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the contract for encrypting and decrypting state values stored 
/// in state containers. Implementations can range from simple obfuscation to more complex 
/// encryption schemes, depending on security requirements.
/// </para>
/// <para>
/// <strong>Algorithm Identification:</strong> Each encryption implementation must provide 
/// both a kind identifier (<see cref="AlgorithmKindId"/>) and a full algorithm identifier 
/// (<see cref="AlgorithmId"/>). The kind identifier is a single character that categorizes 
/// the encryption type, while the full identifier may include additional parameters for 
/// algorithms that require configuration (such as encryption public keys).
/// </para>
/// <para>
/// <strong>Suffix Format:</strong> The framework automatically appends the 
/// <see cref="AlgorithmId"/> to encrypted values as a suffix to enable automatic 
/// algorithm detection during decryption and migration scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple algorithm (no parameters)
/// public class Base64Encryption : IStateContainerEncryption {
///     public char AlgorithmKindId => 'b';
///     public string AlgorithmId => "b";           // Same as kind
///     // ... encryption implementation
/// }
/// 
/// // Parameterized algorithm (with key)
/// public class XorEncryption : IStateContainerEncryption {
///     private readonly byte _key;
///     public char AlgorithmKindId => 'c';
///     public string AlgorithmId => $"{AlgorithmKindId}{this.KindKeySeparator}{_key}";   // Kind + separator + key
///     // ... encryption implementation
/// }
/// </code>
/// </example>
public interface IStateContainerEncryption {

	/// <summary>
	/// Represents the character used to separate the algorithm kind from parameters in algorithm identifiers.
	/// </summary>
	/// <value>The Unicode character 'ζ' (Greek letter zeta).</value>
	/// <remarks>
	/// <para>
	/// This separator is used in <see cref="AlgorithmId"/> to distinguish between the algorithm 
	/// kind and any additional parameters. For example, an XOR encryption with key 123 might 
	/// have an algorithm ID of "cζ123" where 'c' is the kind, 'ζ' is the separator, and '123' 
	/// is the key parameter.
	/// </para>
	/// <para>
	/// The zeta character was chosen because it is unlikely to appear in typical encryption 
	/// output or parameter values, ensuring reliable parsing.
	/// </para>
	/// </remarks>
	public const char KindKeySeparator = '\u03B6';

	/// <summary>
	/// Gets the complete algorithm identifier including any parameters.
	/// </summary>
	/// <value>
	/// A string that uniquely identifies this encryption implementation and its configuration.
	/// </value>
	/// <remarks>
	/// <para>
	/// For simple algorithms without parameters, this is typically the same as 
	/// <see cref="AlgorithmKindId"/>. For parameterized algorithms, this includes the 
	/// kind identifier, <see cref="KindKeySeparator"/>, and the parameter value.
	/// </para>
	/// <para>
	/// This identifier is used as the suffix for encrypted values and as the key for 
	/// dependency injection registration to support automatic decryption and migration.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Simple algorithm
	/// "b"        // Base64 obfuscation
	/// 
	/// // Parameterized algorithms  
	/// "cζ42"     // XOR with key 42
	/// "cζ123"    // XOR with key 123
	/// "dζmykey"  // Custom algorithm with string parameter
	/// </code>
	/// </example>
	string AlgorithmId { get; }

	/// <summary>
	/// Gets the single-character identifier that categorizes this encryption algorithm.
	/// </summary>
	/// <value>A unique character that identifies the encryption algorithm type.</value>
	/// <remarks>
	/// <para>
	/// This identifier categorizes the encryption algorithm without including specific 
	/// configuration parameters. Multiple instances of the same algorithm type (with 
	/// different parameters) will share the same kind identifier but have different 
	/// <see cref="AlgorithmId"/> values.
	/// </para>
	/// <para>
	/// Algorithm kind identifiers should be registered with <see cref="StateEncryptionKinds"/> 
	/// to prevent conflicts and provide human-readable descriptions.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// 'a' // None (no encryption)
	/// 'b' // Base64 obfuscation  
	/// 'c' // XOR obfuscation (regardless of key)
	/// 'd' // Custom algorithm type
	/// </code>
	/// </example>
	char AlgorithmKindId { get; }

	/// <summary>
	/// Encrypts the specified plaintext value.
	/// </summary>
	/// <param name="plaintext">The plaintext value to encrypt.</param>
	/// <returns>The encrypted value, without the algorithm identifier suffix.</returns>
	/// <remarks>
	/// <para>
	/// This method should return only the encrypted content. The framework automatically 
	/// appends the <see cref="AlgorithmId"/> as a suffix to enable algorithm detection 
	/// during decryption.
	/// </para>
	/// <para>
	/// Implementations should ensure that the encrypted output does not contain characters 
	/// that could interfere with suffix parsing, particularly the <see cref="KindKeySeparator"/>.
	/// </para>
	/// </remarks>
	string Encrypt(string plaintext);

	/// <summary>
	/// Decrypts the specified ciphertext value.
	/// </summary>
	/// <param name="ciphertext">The encrypted value to decrypt, without the algorithm identifier suffix.</param>
	/// <returns>The decrypted plaintext value.</returns>
	/// <remarks>
	/// <para>
	/// This method receives only the encrypted content portion, with the algorithm 
	/// identifier suffix already removed by the framework. The implementation should 
	/// decrypt the content using the same algorithm and parameters that were used 
	/// for encryption.
	/// </para>
	/// <para>
	/// If decryption fails, implementations should throw descriptive exceptions to 
	/// aid in debugging migration and configuration issues.
	/// </para>
	/// </remarks>
	string Decrypt(string ciphertext);

}