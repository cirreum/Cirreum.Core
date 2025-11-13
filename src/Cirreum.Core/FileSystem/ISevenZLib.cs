namespace Cirreum.FileSystem;

using System.Threading.Tasks;

/// <summary>
/// Defines a client service that can extract 7z compressed files.
/// </summary>
public interface ISevenZLib : IDisposable {

	/// <summary>
	/// Perform the extraction process.
	/// </summary>
	/// <param name="source">The soure file to extract.</param>
	/// <param name="destination">The directory of where to expand the source contents.</param>
	/// <param name="secondsToWait">The number of seconds to wait for the extraction to occur.</param>
	/// <returns>The exit code of the extraction process.</returns>
	Task<int> ExtractAsync(string source, string destination, int secondsToWait = 30);

	/// <summary>
	/// Perform the extraction process.
	/// </summary>
	/// <param name="source">The soure file to extract.</param>
	/// <param name="destination">The directory of where to expand the source contents.</param>
	/// <param name="secondsToWait">The number of seconds to wait for the extraction to occur.</param>
	/// <returns>The exit code of the extraction process.</returns>
	int Extract(string source, string destination, int secondsToWait = 30);

}