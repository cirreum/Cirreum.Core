namespace Cirreum;

/// <summary>
/// Builder interface for registering state sections with the dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// All state sections are automatically registered as scoped services to ensure proper lifecycle
/// management within the application's service scope.
/// </para>
/// <para>
/// <strong>Built-in Decryption Support:</strong> The framework automatically registers 
/// decryptors for <see cref="BuiltInEncryption.None"/> and <see cref="BuiltInEncryption.Base64Obfuscation"/> 
/// to support migration from these common encryption schemes. Other built-in algorithms 
/// (such as <see cref="BuiltInEncryption.XorObfuscation"/>) and custom encryption implementations 
/// must be explicitly registered using <see cref="RegisterDecryptor"/> if migration 
/// support is needed.
/// </para>
/// </remarks>
public interface IStateBuilder {

	/// <summary>
	/// Registers an application state implementation with its corresponding interface.
	/// </summary>
	/// <typeparam name="TInterface">The interface type that consumers will use to access the state section</typeparam>
	/// <typeparam name="TImplementation">The concrete implementation of the state section</typeparam>
	/// <returns>The state builder instance for method chaining</returns>
	/// <remarks>
	/// The state section will be registered as a singleton service in the dependency injection container.
	/// This ensures proper lifecycle management and allows the state to be resolved within the
	/// application's service scope.
	/// </remarks>
	/// <example>
	/// <code>
	/// builder.AddClientState(state => {
	///     state.RegisterState&lt;IMyState, MyState&gt;();
	/// });
	/// </code>
	/// </example>
	IStateBuilder RegisterState<TInterface, TImplementation>()
		where TInterface : class, IApplicationState
		where TImplementation : class, TInterface;

	/// <summary>
	/// Registers an application state implementation directly without an interface.
	/// </summary>
	/// <typeparam name="TImplementation">The concrete state section type to register</typeparam>
	/// <returns>The state builder instance for method chaining</returns>
	/// <remarks>
	/// The state will be registered as a singleton service in the dependency injection container.
	/// Use this overload when you don't need interface abstraction for your state.
	/// </remarks>
	/// <example>
	/// <code>
	/// builder.AddClientState(state => {
	///     state.RegisterState&lt;AppSettings&gt;();
	/// });
	/// </code>
	/// </example>
	IStateBuilder RegisterState<TImplementation>()
		where TImplementation : class, IApplicationState;

	/// <summary>
	/// Registers an encryption implementation for state containers.
	/// </summary>
	/// <param name="encryptor">The encryption implementation to use for state value protection</param>
	/// <returns>The state builder instance for method chaining</returns>
	/// <remarks>
	/// <para>
	/// This configures the encryption strategy used by state containers to protect stored values.
	/// The encryption implementation will be registered as a singleton and automatically injected
	/// into state containers that support value encryption. Additionally, the encryptor is registered
	/// as a keyed service using its <see cref="IStateContainerEncryption.AlgorithmId"/> for
	/// decryption support during data migration scenarios.
	/// </para>
	/// <para>
	/// <strong>Encryption Scope:</strong> This encryption applies only to <c>StateContainer</c> 
	/// implementations (such as <see cref="ISessionState"/>, <see cref="ILocalState"/>, and 
	/// <see cref="IMemoryState"/>) that provide persistent or temporary storage capabilities.
	/// Application-specific state that directly implement <see cref="IApplicationState"/>,
	/// <see cref="IScopedNotificationState"/>, <see cref="IStateContainer"/>
	/// or <see cref="IPersistableStateContainer"/> are not affected by this encryption
	/// setting and remain unencrypted by default.
	/// </para>
	/// <para>
	/// If no encryption is registered, state containers will use no encryption by default.
	/// Only one encryption implementation can be registered as the primary encryptor - subsequent calls
	/// will replace the previous registration, though all previously registered encryptors remain
	/// available as keyed services for decryption purposes.
	/// </para>
	/// <para>
	/// <strong>Security Notice:</strong> The built-in encryption implementations provide obfuscation 
	/// and light protection against casual inspection, but are not cryptographically secure. 
	/// Do not use these methods to protect truly sensitive data such as passwords, personal 
	/// information, or confidential business data.
	/// </para>
	/// <para>
	/// <strong>Data Migration Warning:</strong> Changing encryption strategies will render 
	/// existing encrypted data unreadable unless appropriate decryptors are registered using
	/// <see cref="RegisterDecryptor"/>. Ensure you have appropriate data migration procedures 
	/// in place before modifying encryption settings in production environments.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// builder.AddClientState(state => {
	///     // This encryption applies to ISessionState, ILocalState, IMemoryState
	///     state.RegisterEncryptor(BuiltInEncryption.Base64Obfuscation);
	///     
	///     // These remain unencrypted by default
	///     state.RegisterState&lt;IAppDataState, AppDataState&gt;();
	///     state.RegisterState&lt;INavMenuState, NavMenuState&gt;();
	/// });
	/// </code>
	/// </example>
	IStateBuilder RegisterEncryptor(IStateContainerEncryption encryptor);

	/// <summary>
	/// Registers a decryption implementation to support migration from a previous encryption scheme.
	/// </summary>
	/// <param name="previousEncryption">The encryption implementation that was previously used to encrypt data</param>
	/// <returns>The state builder instance for method chaining</returns>
	/// <remarks>
	/// <para>
	/// Use this method to register decryption support for legacy encryption schemes when migrating
	/// to a new encryption strategy. This enables the framework to automatically decrypt existing
	/// data that was encrypted with previous algorithms while using the new encryption for all
	/// new data writes. The decryptor is registered as a keyed service using its 
	/// <see cref="IStateContainerEncryption.AlgorithmId"/>.
	/// </para>
	/// <para>
	/// <strong>Migration Strategy:</strong> Register your new encryption with <see cref="RegisterEncryptor"/>
	/// and register previous encryption schemes with this method. Data will migrate transparently
	/// as it is accessed - old data gets decrypted with legacy algorithms, new data gets encrypted
	/// with the current algorithm.
	/// </para>
	/// <para>
	/// <strong>Built-in Support:</strong> <see cref="BuiltInEncryption.None"/> and 
	/// <see cref="BuiltInEncryption.Base64Obfuscation"/> are automatically registered as decryptors
	/// and do not need explicit registration. Only parameterized algorithms (like 
	/// <see cref="BuiltInEncryption.XorObfuscation"/>) and custom implementations require explicit
	/// registration. Built-in algorithms that are already registered (None and Base64) will be
	/// silently ignored to prevent duplicate registrations.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// builder.AddClientState(state => {
	///     // Set current encryption for new data
	///     state.RegisterEncryptor(BuiltInEncryption.XorObfuscation(123));
	///     
	///     // Register migration support for old data
	///     state.RegisterDecryptor(BuiltInEncryption.XorObfuscation(42))  // Previous XOR key
	///          .RegisterDecryptor(myOldCustomEncryption);                // Custom algorithm
	///     
	///     // Note: None and Base64 are automatically supported - no registration needed
	/// });
	/// </code>
	/// </example>
	IStateBuilder RegisterDecryptor(IStateContainerEncryption previousEncryption);

}