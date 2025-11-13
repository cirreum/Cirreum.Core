namespace Cirreum;

/// <summary>
/// Provides a key-value storage abstraction for state management with automatic change notifications.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IStateContainer"/> extends <see cref="IScopedNotificationState"/> to provide a generic, 
/// dictionary-like storage mechanism for maintaining typed values with keys. This is useful when:
/// </para>
/// <list type="bullet">
/// <item><description>You need dynamic key-value storage rather than fixed properties</description></item>
/// <item><description>You want to store heterogeneous data types in a single state section</description></item>
/// <item><description>You need a flexible storage mechanism that can grow at runtime</description></item>
/// <item><description>You want built-in state persistence across different storage backends (memory, localStorage, sessionStorage)</description></item>
/// </list>
/// <para>
/// Use <see cref="IScopedNotificationState"/> directly when you need strongly-typed properties and a fixed schema.
/// Use <see cref="IStateContainer"/> when you need flexible, dynamic key-value storage with the same notification capabilities.
/// </para>
/// </remarks>
public interface IStateContainer : IScopedNotificationState {

	/// <summary>
	/// Gets or creates an <see cref="IStateValueHandle{T}"/> for the specified type with an initial value.
	/// Uses the type name as the key.
	/// </summary>
	/// <typeparam name="T">The type of value to store.</typeparam>
	/// <param name="initialValue">The initial value to use if creating a new handle.</param>
	/// <returns>A handle for getting and setting the value.</returns>
	IStateValueHandle<T> GetOrCreate<T>(T initialValue) where T : notnull;

	/// <summary>
	/// Gets or creates an <see cref="IStateValueHandle{T}"/> for the specified key with an initial value.
	/// </summary>
	/// <typeparam name="T">The type of value to store.</typeparam>
	/// <param name="key">The unique key used to identify the stored value.</param>
	/// <param name="initialValue">The initial value to use if creating a new handle.</param>
	/// <returns>A handle for getting and setting the value.</returns>
	IStateValueHandle<T> GetOrCreate<T>(string key, T initialValue) where T : notnull;

	/// <summary>
	/// Gets the stored value for the specified type, or returns the default value if the stored value is null.
	/// Uses the type name as the key.
	/// </summary>
	/// <typeparam name="T">The type of value to retrieve.</typeparam>
	/// <param name="defaultValue">The value to return if the stored value is null.</param>
	/// <returns>The stored value if not null; otherwise, the specified default value.</returns>
	/// <remarks>
	/// This method will throw an exception if the key is not found, if there's a type mismatch, 
	/// or if both the stored value and default value are null. Ensure the property is initialized 
	/// with <see cref="GetOrCreate{T}(string, T)"/> or <see cref="GetOrCreate{T}(T)"/> before calling this method.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the property key is not found (call GetOrCreate() first to initialize), 
	/// when the existing handle is not of the expected type, 
	/// or when both the stored value and default value are null.
	/// </exception>
	T Get<T>(T defaultValue) where T : notnull;

	/// <summary>
	/// Gets the stored value for the specified key, or returns the default value if the stored value is null.
	/// </summary>
	/// <typeparam name="T">The type of value to retrieve.</typeparam>
	/// <param name="key">The unique key used to identify the stored value.</param>
	/// <param name="defaultValue">The value to return if the stored value is null.</param>
	/// <returns>The stored value if not null; otherwise, the specified default value.</returns>
	/// <remarks>
	/// This method will throw an exception if the key is not found, if there's a type mismatch, 
	/// or if both the stored value and default value are null. Ensure the property is initialized 
	/// with <see cref="GetOrCreate{T}(string, T)"/> or <see cref="GetOrCreate{T}(T)"/> before calling this method.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the property key is not found (call GetOrCreate() first to initialize), 
	/// when the existing handle is not of the expected type, 
	/// or when both the stored value and default value are null.
	/// </exception>
	T Get<T>(string key, T defaultValue) where T : notnull;

	/// <summary>
	/// Removes the stored value for the specified type if it exists.
	/// Uses the type name as the key.
	/// </summary>
	/// <typeparam name="T">The type of value to remove</typeparam>
	void Remove<T>();

	/// <summary>
	/// Removes the stored value for the specified key if it exists.
	/// </summary>
	/// <param name="key">The unique key identifying the value to remove</param>
	void Remove(string key);

	/// <summary>
	/// Removes all stored values for the specified keys if they exist.
	/// </summary>
	/// <param name="keys">The collection of keys identifying the values to remove</param>
	void Remove(params IEnumerable<string> keys);

	/// <summary>
	/// Resets a state value to the specified default value if the key exists.
	/// </summary>
	/// <typeparam name="T">The type of the state value being reset.</typeparam>
	/// <param name="key">The unique key identifying the state value to reset.</param>
	/// <param name="defaultValue">The default value to reset to.</param>
	/// <returns>A completed task (operation is synchronous).</returns>
	/// <remarks>
	/// <para>
	/// This method only resets existing state values. If no state value exists for the 
	/// specified key, the operation completes successfully without any action.
	/// </para>
	/// <para>
	/// The method first attempts a strongly-typed reset if the handle matches type T. 
	/// If there's a type mismatch, it falls back to a non-generic reset, but only if 
	/// the defaultValue is not null.
	/// </para>
	/// <para>
	/// Note: If defaultValue is null and there's a type mismatch, no reset operation 
	/// will be performed on the existing handle.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Reset existing values
	/// await stateContainer.Reset&lt;string&gt;("User.Name", "Anonymous");
	/// await stateContainer.Reset&lt;int&gt;("User.Age", 0);
	/// 
	/// // If key doesn't exist, nothing happens (no error)
	/// await stateContainer.Reset&lt;string&gt;("NonExistent.Key", "Default");
	/// </code>
	/// </example>
	Task Reset<T>(string key, T defaultValue);

	/// <summary>
	/// Sets the container to its initial state, clearing all stored values.
	/// </summary>
	void Initialize();

}