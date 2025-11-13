namespace Cirreum;

using System.Runtime.Versioning;

public static class EnvironmentExtensions {

	/// <summary>
	/// Gets the environment variable or default.
	/// </summary>
	/// <param name="environment">The environment.</param>
	/// <param name="name">The name.</param>
	/// <param name="defaultValue">The default value.</param>
	/// <returns></returns>
	[UnsupportedOSPlatform("browser")]
	public static string GetEnvironmentVariableOrDefault(this IEnvironment environment, string name, string defaultValue) {
		return environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ?? defaultValue;
	}

	/// <summary>
	/// Gets the number of effective cores taking into account SKU/environment restrictions.
	/// </summary>
	[UnsupportedOSPlatform("browser")]
#pragma warning disable IDE0060 // Remove unused parameter
	public static int GetEffectiveCoresCount(this IEnvironment environment) {
		return Environment.ProcessorCount;
	}
#pragma warning restore IDE0060 // Remove unused parameter

	/// <summary>
	/// Checks the Environment variable 'DOTNET_RUNNING_IN_CONTAINER' equals 'true';'
	/// </summary>
	[UnsupportedOSPlatform("browser")]
#pragma warning disable IDE0060 // Remove unused parameter
	public static bool IsRunningInContainer(this IEnvironment environment) {
		return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
	}
#pragma warning restore IDE0060 // Remove unused parameter

}