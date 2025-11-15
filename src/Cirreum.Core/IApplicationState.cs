namespace Cirreum;

/// <summary>
/// Base marker interface for all application state types.
/// </summary>
/// <remarks>
/// <para>
/// This interface serves as the foundational contract for all state-related types in the application.
/// It acts as a marker interface with no behavioral requirements, allowing the type system to 
/// distinguish state objects from other application components.
/// </para>
/// <para>
/// Implementing this interface enables:
/// </para>
/// <list type="bullet">
/// <item><description>Type-safe identification of state objects</description></item>
/// <item><description>Generic constraints that ensure only state types are used in state management APIs</description></item>
/// <item><description>Consistent categorization of all application state regardless of implementation</description></item>
/// <item><description>Future extensibility for cross-cutting state concerns (logging, validation, etc.)</description></item>
/// </list>
/// <para>
/// This interface is typically not implemented directly. Instead, implement one of its derived interfaces
/// such as <see cref="IScopedNotificationState"/>, <see cref="IStateContainer"/>, or 
/// <see cref="IPersistableStateContainer"/> which provide specific state management capabilities.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple state class
/// public class UserPreferences : IApplicationState 
/// {
///     public string Theme { get; set; } = "Light";
///     public bool NotificationsEnabled { get; set; } = true;
/// }
/// 
/// // State with notification capabilities
/// public class ShoppingCart : IScopedNotificationState 
/// {
///     // Inherits from IApplicationState through IScopedNotificationState
///     public List&lt;CartItem&gt; Items { get; set; } = new();
///     
///     public IDisposable CreateNotificationScope() { /* implementation */ }
///     public IAsyncDisposable CreateNotificationScopeAsync() { /* implementation */ }
/// }
/// 
/// // Generic constraint usage
/// public void ProcessState&lt;T&gt;(T state) where T : IApplicationState
/// {
///     // Only state types can be passed to this method
/// }
/// </code>
/// </example>
public interface IApplicationState;