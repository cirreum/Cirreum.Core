namespace Cirreum;

/// <summary>
/// Represents a handle for managing the state value of an object or system.
/// </summary>
/// <remarks>This interface provides a mechanism to interact with and manipulate state values. Implementations may
/// define specific behaviors for accessing, updating, or observing state.</remarks>
public interface IStateValueHandle {
}

/// <summary>
/// Provides access and modification capabilities for a typed value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// <para>
/// Handles both reading and writing of state values while ensuring proper:
/// </para>
/// <list type="bullet">
///     <item>
///         <description>Type safety for state values</description>
///     </item>
///     <item>
///         <description>State change notifications</description>
///     </item>
///     <item>
///         <description>Asynchronous state updates</description>
///     </item>
/// </list>
/// </remarks>
public interface IStateValueHandle<T> : IStateValueHandle {

	/// <summary>
	/// Gets the current state value.
	/// </summary>
	/// <value>
	/// The current value stored in state.
	/// </value>
	T Value { get; }

	/// <summary>
	/// Updates the state value asynchronously.
	/// </summary>
	/// <param name="newValue">The new value to store in state.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	/// <remarks>
	/// This operation will trigger appropriate state change notifications.
	/// </remarks>
	Task SetValue(T newValue);

	/// <summary>
	/// Resets the underlying value.
	/// </summary>
	/// <param name="defaultValue">The default value.</param>
	/// <remarks>
	/// This method is intended for when you want to
	/// reset the value without notifying subscribers of the change.
	/// </remarks>
	void ResetValue(T defaultValue);

	/// <summary>
	/// Deconstructs the handle into its value and setter components.
	/// </summary>
	/// <param name="value">The current state value.</param>
	/// <param name="setter">An async function that can be used to update the state value.</param>
	/// <remarks>
	/// Enables convenient pattern matching and deconstruction syntax:
	/// <code>
	/// var (currentValue, setValue) = stateHandle;
	/// await setValue(newValue);
	/// 
	/// // Or fire-and-forget for simple updates
	/// var (value, setter) = stateHandle;
	/// _ = setter(value + 1);
	/// </code>
	/// </remarks>
	void Deconstruct(out T value, out Func<T, Task> setter);

}