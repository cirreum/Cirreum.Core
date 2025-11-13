namespace Cirreum;

using System.Runtime.Versioning;

/// <summary>
/// A implementation of <see cref="IEnvironment"/> backed
/// by the .NET <c>System</c> <see cref="Environment"/> object.
/// </summary>
public sealed class SystemEnvironment : IEnvironment {

	private static readonly Lazy<SystemEnvironment> _instance = new Lazy<SystemEnvironment>(CreateInstance);

	private SystemEnvironment() {
	}

	public static SystemEnvironment Instance => _instance.Value;

	private static SystemEnvironment CreateInstance() {
		return new SystemEnvironment();
	}

	/// <summary>
	/// Retrieves the value of an environment variable from the current process or from
	/// the Windows operating system registry key for the current user or local machine.
	/// </summary>
	/// <param name="name">The name of an environment variable.</param>
	/// <param name="target">One of the <see cref="EnvironmentVariableTarget"/> values.</param>
	/// <returns>
	/// The value of the environment variable specified by the variable and target parameters, or
	/// null if the environment variable is not found.</returns>
	/// <remarks>
	/// Only <see cref="EnvironmentVariableTarget.Process"/>
	/// is supported on .NET running on Unix-based systems.
	/// </remarks>
	/// <exception cref="ArgumentNullException">variable is null.</exception>
	/// <exception cref="ArgumentException">target is not a valid <see cref="EnvironmentVariableTarget"/> value.</exception>
	/// <exception cref="System.Security.SecurityException">The caller does not have the required permission to perform this operation.</exception>
	[UnsupportedOSPlatform("browser")]
	public string? GetEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) {
		return Environment.GetEnvironmentVariable(name, target);
	}

	[UnsupportedOSPlatform("browser")]
	public void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) {
		Environment.SetEnvironmentVariable(name, value, target);
	}

	public string UserId { get; } = Environment.UserName;

	public string UserName { get; } = Environment.UserName;

	[UnsupportedOSPlatform("browser")]
	public string UserDomainName { get; } = Environment.UserDomainName;

	[UnsupportedOSPlatform("browser")]
	public string MachineName { get; } = Environment.MachineName;

	[UnsupportedOSPlatform("browser")]
	public string CurrentDirectory { get; } = Environment.CurrentDirectory;

}