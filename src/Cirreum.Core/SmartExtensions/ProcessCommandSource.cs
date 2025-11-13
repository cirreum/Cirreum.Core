namespace Cirreum.SmartExtensions;

using System.Diagnostics;

/// <summary>
/// A SmartFormat Extension to support using the Process object during string formatting.
/// </summary>
public class ProcessCommandSource : CommandSource<string> {

	/// <summary>
	/// The command prefix Key used to identify this command source
	/// in a format string.
	/// </summary>
	private const string cmdKey = "prc";

	/// <summary>
	/// Constructor.
	/// </summary>
	public ProcessCommandSource() :
		base(cmdKey) {

	}

	/// <inheritdoc/>
	protected override string? ResolveValue(string command) {

		var process = Process.GetCurrentProcess();

		return (command.ToLowerInvariant()) switch {
			"id" => Environment.ProcessId.ToString(),
			"name" => process.ProcessName,
			"machine" => process.MachineName,
			"session" => process.SessionId.ToString(),
			"file" => GetProcessFileName(process),
			"directory" => GetProcessWorkingDirectory(process),
			_ => default,
		};

	}

	private static string GetProcessFileName(Process process) {
		try {
			return process.StartInfo.FileName;
		} catch (InvalidOperationException) {
			// Alternative approaches when StartInfo is not available
			return System.Reflection.Assembly.GetEntryAssembly()?.Location
				   ?? AppDomain.CurrentDomain.BaseDirectory;
		}
	}

	private static string GetProcessWorkingDirectory(Process process) {
		try {
			return process.StartInfo.WorkingDirectory;
		} catch (InvalidOperationException) {
			// Alternative approaches when StartInfo is not available
			return Environment.CurrentDirectory;
		}
	}

}