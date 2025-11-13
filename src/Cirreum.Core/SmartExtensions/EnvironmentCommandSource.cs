namespace Cirreum.SmartExtensions;

/// <summary>
/// A custom <see cref="CommandSource{T}"/>, that resolve <see cref="Environment"/> properties.
/// </summary>
/// <remarks>
/// <para>
/// <code>
///	"newline" => Environment.NewLine,
///	"commandline" => Environment.CommandLine,
///	"machinename" => Environment.MachineName,
///	"username" => Environment.UserName,
///	"currentdirectory" => Environment.CurrentDirectory,
///	"hasshutdownstarted" => Environment.HasShutdownStarted.ToString(),
///	"is64bitoperatingsystem" => Environment.Is64BitOperatingSystem.ToString(),
///	"is64bitprocess" => Environment.Is64BitProcess.ToString(),
///	"osversion" => Environment.OSVersion.ToString(),
///	"processid" => Environment.ProcessId.ToString(),
///	"processorcount" => Environment.ProcessorCount.ToString(),
///	"version" => Environment.Version.ToString(),
///	"userdomainname" => Environment.UserDomainName.ToString(),
///	_ => Environment.ExpandEnvironmentVariables($"%{command}%")
/// </code>
/// </para>
/// </remarks>
public class EnvironmentCommandSource : CommandSource<string> {

	/// <summary>
	/// The command prefix Key used to identify this command source
	/// in a format string.
	/// </summary>
	private const string cmdKey = "env";

	/// <summary>
	/// Constructor.
	/// </summary>
	public EnvironmentCommandSource() :
		base(cmdKey) {

	}

	/// <inheritdoc/>
	protected override string ResolveValue(string command) {

		return (command.ToLowerInvariant()) switch {
			"newline" => Environment.NewLine,
			"commandline" => Environment.CommandLine,
			"machinename" => Environment.MachineName,
			"username" => Environment.UserName,
			"currentdirectory" => Environment.CurrentDirectory,
			"hasshutdownstarted" => Environment.HasShutdownStarted.ToString(),
			"is64bitoperatingsystem" => Environment.Is64BitOperatingSystem.ToString(),
			"is64bitprocess" => Environment.Is64BitProcess.ToString(),
			"osversion" => Environment.OSVersion.ToString(),
			"processid" => Environment.ProcessId.ToString(),
			"processpath" => Environment.ProcessPath ?? "",
			"processorcount" => Environment.ProcessorCount.ToString(),
			"version" => Environment.Version.ToString(),
			"userdomainname" => Environment.UserDomainName.ToString(),
			_ => Environment.ExpandEnvironmentVariables($"%{command}%"),
		};

	}

}