namespace Cirreum.State;

/// <summary>
/// Provides scoped control over state change notifications.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows state implementations to suppress state change notifications
/// while a notification scope is active. It is useful when multiple related state
/// mutations occur together and intermediate notifications should be deferred.
/// </para>
/// <para>
/// Notification scopes may be nested. Implementations should treat the outermost
/// scope boundary as the point at which deferred notification behavior is resolved.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using (myState.CreateNotificationScope())
/// {
///     userState.Name = "John Doe";
///     userState.Email = "john@example.com";
///     userState.LastModified = DateTime.Now;
/// }
/// </code>
/// </example>
public interface IScopedNotificationState : IApplicationState {

	/// <summary>
	/// Creates a notification scope that defers notification behavior until the scope is disposed.
	/// </summary>
	/// <returns>
	/// An <see cref="IDisposable"/> that represents the active notification scope.
	/// </returns>
	/// <remarks>
	/// <para>
	/// While a notification scope is active, state change notifications may be suppressed
	/// or deferred by the implementation.
	/// </para>
	/// <para>
	/// Notification scopes may be nested. Implementations should resolve deferred
	/// notification behavior when the outermost scope completes.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (myState.CreateNotificationScope())
	/// {
	///     myState.Property1 = newValue1;
	///     myState.Property2 = newValue2;
	/// }
	/// </code>
	/// </example>
	IDisposable CreateNotificationScope();
}