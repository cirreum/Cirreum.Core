namespace Cirreum;

using System.Text;

/// <summary>
/// Provides built-in encryption implementations for state value protection.
/// </summary>
/// <remarks>
/// <para>
/// This class offers several pre-built encryption strategies ranging from no encryption
/// to light obfuscation suitable for preventing casual inspection of stored state values.
/// These implementations are designed for convenience and basic protection rather than
/// high-security scenarios.
/// </para>
/// <para>
/// For production applications with sensitive data, consider implementing a custom
/// <see cref="IStateContainerEncryption"/> with stronger cryptographic methods and proper key management.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // None encryption (default)
/// service.AddClientState(state => state
///		.AddEncryptor(BuiltInEncryption.None));
/// 
/// // Light obfuscation to prevent casual viewing
/// service.AddClientState(state => state
///		.AddEncryptor(BuiltInEncryption.Base64Obfuscation));
/// 
/// // XOR obfuscation with custom key
/// service.AddClientState(state => state
///		.AddEncryptor(BuiltInEncryption.XorObfuscation(123)));
///
/// // Example use with custom encryption
/// service.AddClientState(state => state
///		.AddEncryptor(MyCustomEncryption)
///		.AddDecryptor(MyCustomEncryption));
/// </code>
/// </example>
public static class BuiltInEncryption {

	/// <summary>
	/// Gets an encryption implementation that performs no encryption or obfuscation.
	/// </summary>
	/// <value>
	/// An <see cref="IStateContainerEncryption"/> instance that returns values unchanged.
	/// </value>
	/// <remarks>
	/// This is the default behavior when no encryption is specified. Values are stored
	/// and retrieved in their original form without any transformation.
	/// </remarks>
	public static IStateContainerEncryption None => new NoEncryption();

	/// <summary>
	/// Gets an encryption implementation that uses Base64 encoding for light obfuscation.
	/// </summary>
	/// <value>
	/// An <see cref="IStateContainerEncryption"/> instance that encodes values using Base64.
	/// </value>
	/// <remarks>
	/// <para>
	/// This provides basic obfuscation to prevent casual inspection of stored values
	/// in browser storage or other plain-text storage mechanisms. Base64 encoding is
	/// easily reversible and should not be considered secure encryption.
	/// </para>
	/// <para>
	/// Suitable for hiding configuration values, user preferences, or other non-sensitive
	/// data from casual viewing while maintaining good performance.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var encryption = BuiltInEncryption.Base64Obfuscation;
	/// var encrypted = encryption.Encrypt("Hello World"); // "SGVsbG8gV29ybGQ="
	/// var decrypted = encryption.Decrypt(encrypted);     // "Hello World"
	/// </code>
	/// </example>
	public static IStateContainerEncryption Base64Obfuscation => new Base64Encryption();

	/// <summary>
	/// Gets an encryption implementation that uses XOR obfuscation with a specified key.
	/// </summary>
	/// <param name="key">The XOR key to use for encryption. Default is 42.</param>
	/// <returns>
	/// An <see cref="IStateContainerEncryption"/> instance that performs XOR obfuscation.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This provides lightweight obfuscation using XOR operations combined with positional
	/// variation (key + position) to avoid simple pattern recognition. The result is
	/// Base64 encoded for safe storage in text-based formats.
	/// </para>
	/// <para>
	/// While more secure than Base64 alone, XOR obfuscation is still easily broken
	/// with sufficient analysis and should not be used for protecting truly sensitive data.
	/// It is suitable for preventing casual inspection and simple tampering.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var encryption = BuiltInEncryption.XorObfuscation(123);
	/// var encrypted = encryption.Encrypt("sensitive data");
	/// var decrypted = encryption.Decrypt(encrypted); // "sensitive data"
	/// 
	/// // Different keys produce different results
	/// var encryption2 = BuiltInEncryption.XorObfuscation(456);
	/// var different = encryption2.Encrypt("sensitive data"); // Different output
	/// </code>
	/// </example>
	public static IStateContainerEncryption XorObfuscation(byte key = 42) => new XorEncryption(key);

	/// <summary>
	/// No-operation encryption implementation that leaves values unchanged.
	/// </summary>
	private class NoEncryption : IStateContainerEncryption {
		/// <inheritdoc/>
		public char AlgorithmKindId => StateEncryptionKinds.NONE;
		/// <inheritdoc/>
		public string AlgorithmId => $"{this.AlgorithmKindId}";
		/// <inheritdoc/>
		public string Encrypt(string plaintext) => plaintext;
		/// <inheritdoc/>
		public string Decrypt(string ciphertext) => ciphertext;
	}

	/// <summary>
	/// Base64 encoding implementation for basic value obfuscation.
	/// </summary>
	private class Base64Encryption : IStateContainerEncryption {
		/// <inheritdoc/>
		public char AlgorithmKindId => StateEncryptionKinds.BASE64;
		/// <inheritdoc/>
		public string AlgorithmId => $"{this.AlgorithmKindId}";
		/// <inheritdoc/>
		public string Encrypt(string plaintext)
			=> Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
		/// <inheritdoc/>
		public string Decrypt(string ciphertext)
			=> Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
	}

	/// <summary>
	/// XOR obfuscation implementation with positional key variation.
	/// </summary>
	/// <param name="key">The base XOR key used for obfuscation.</param>
	private class XorEncryption(byte key) : IStateContainerEncryption {
		/// <inheritdoc/>
		public char AlgorithmKindId => StateEncryptionKinds.XOR;
		/// <inheritdoc/>
		public string AlgorithmId => $"{this.AlgorithmKindId}{IStateContainerEncryption.KindKeySeparator}{key}";
		/// <inheritdoc/>
		public string Encrypt(string plaintext) {
			var bytes = Encoding.UTF8.GetBytes(plaintext);
			for (var i = 0; i < bytes.Length; i++) {
				bytes[i] ^= (byte)(key + i);
			}
			return Convert.ToBase64String(bytes);
		}
		/// <inheritdoc/>
		public string Decrypt(string ciphertext) {
			var bytes = Convert.FromBase64String(ciphertext);
			for (var i = 0; i < bytes.Length; i++) {
				bytes[i] ^= (byte)(key + i);
			}
			return Encoding.UTF8.GetString(bytes);
		}
	}

}