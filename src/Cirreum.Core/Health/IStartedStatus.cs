namespace Cirreum.Health;
/// <summary>
/// Defines a service that indicates if the application
/// has successfully started. See: <see cref="StartupCompleted"/>
/// </summary>
/// <remarks>
/// <para>
/// A health check service can implement this service to determine if the application has started.
/// </para>
/// <para>
/// The is used by the server runtime, and should not be used by user application code.
/// </para>
/// </remarks>
public interface IStartedStatus {
	/// <summary>
	/// True if the application has succesfully started.
	/// </summary>
	/// <remarks>
	/// This value only indicates if the application has started
	/// and not whether its "ready".
	/// </remarks>
	bool StartupCompleted { get; set; }
}