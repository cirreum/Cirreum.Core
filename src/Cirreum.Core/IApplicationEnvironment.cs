namespace Cirreum;

/// <summary>
/// Gets the name of the current application environment.
/// </summary>
/// <remarks>The environment name typically indicates the runtime context, such as "Development", "Staging", or
/// "Production". This value can be used to configure application behavior based on the environment.</remarks>
public interface IApplicationEnvironment {
	/// <summary>
	/// Gets the name of the application.
	/// </summary>
	string ApplicationName { get; }
	/// <summary>
	/// Gets the name of the current application environment.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The environment name typically indicates the runtime context, such as "Development", "Staging", or
	/// "Production". This value can be used to configure application behavior based on the environment.
	/// </para>
	/// </remarks>
	string EnvironmentName { get; }
}