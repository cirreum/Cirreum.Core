namespace Cirreum.State;
/// <summary>
/// Provides batched state change notifications with scoping capabilities.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables efficient state management by allowing multiple state changes 
/// to be batched into a single notification event. This is particularly useful in scenarios where:
/// </para>
/// <list type="bullet">
/// <item><description>Multiple related state properties are updated together</description></item>
/// <item><description>You want to prevent excessive UI re-renders in response frameworks like Blazor</description></item>
/// <item><description>Complex state operations need to be treated as atomic units</description></item>
/// <item><description>Performance optimization is needed when many rapid state changes occur</description></item>
/// </list>
/// <para>
/// The scoping mechanism supports nested operations, ensuring that notifications are only 
/// triggered when the outermost scope completes, similar to transaction boundaries in databases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Batch multiple state changes into a single notification
/// using (myState.CreateNotificationScope())
/// {
///     userState.Name = "John Doe";
///     userState.Email = "john@example.com";
///     userState.LastModified = DateTime.Now;
///     // Single notification sent here when scope disposes
/// }
/// </code>
/// </example>
public interface IScopedNotificationState : IApplicationState {

	/// <summary>
	/// Creates a synchronous scope that batches and delays state change notifications until the scope is disposed.
	/// </summary>
	/// <returns>
	/// An <see cref="IDisposable"/> scope that will trigger batched notifications when disposed.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Multiple state changes within the scope will be consolidated into a single notification event.
	/// Nested scopes are supported - notifications are only sent when the outermost scope is disposed,
	/// ensuring that complex, multi-layered state operations are treated as atomic units.
	/// </para>
	/// <para>
	/// Use this method to batch multiple state changes into a single notification event.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (myState.CreateNotificationScope())
	/// {
	///     // Multiple state changes here
	///     myState.Property1 = newValue1;
	///     myState.Property2 = newValue2;
	///     // Only one notification triggered when using block exits
	/// }
	/// </code>
	/// </example>
	IDisposable CreateNotificationScope();

}