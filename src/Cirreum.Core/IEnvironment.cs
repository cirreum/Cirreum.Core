namespace Cirreum;

using System.Runtime.Versioning;

/// <summary>
/// Provides an accessor abstraction to an Environment.
/// </summary>
public interface IEnvironment {

	/// <summary>
	/// Returns the value of an environment variable for the current <see cref="IEnvironment"/>.
	/// </summary>
	/// <param name="name">The environment variable name.</param>
	/// <param name="target"></param>
	/// <returns>The value of the environment variable specified by <paramref name="name"/>, or <see langword="null"/> if the environment variable is not found.</returns>
	[UnsupportedOSPlatform("browser")]
	string? GetEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);

	/// <summary>
	/// Creates, modifies, or deletes an environment variable stored in the current <see cref="IEnvironment"/>
	/// </summary>
	/// <param name="name">The environment variable name.</param>
	/// <param name="value">The value to assign to the variable.</param>
	/// <param name="target"></param>
	[UnsupportedOSPlatform("browser")]
	void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);

	/// <summary>
	/// Gets the Environment User Id.
	/// </summary>
	string UserId { get; }

	/// <summary>
	/// Gets the Environment User Name.
	/// </summary>
	string UserName { get; }

	/// <summary>
	/// Gets the User Domain Name
	/// </summary>
	[UnsupportedOSPlatform("browser")]
	string UserDomainName { get; }

	/// <summary>
	/// Gets the Environment Machine Name
	/// </summary>
	[UnsupportedOSPlatform("browser")]
	string MachineName { get; }

	/// <summary>
	/// Gets the Current Directory
	/// </summary>
	[UnsupportedOSPlatform("browser")]
	string CurrentDirectory { get; }

}