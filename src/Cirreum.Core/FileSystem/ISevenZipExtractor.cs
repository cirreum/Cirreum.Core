namespace Cirreum.FileSystem;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a client service that can extract 7z compressed files.
/// </summary>
public interface ISevenZipExtractor : IDisposable {

	/// <summary>
	/// Extracts a 7z compressed file to the specified destination directory.
	/// </summary>
	/// <param name="source">The source file to extract.</param>
	/// <param name="destination">The directory where the contents will be extracted.</param>
	/// <param name="timeout">The maximum time to wait for the extraction to complete. Default is 30 seconds.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation, containing the exit code of the extraction process.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	/// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
	/// <exception cref="TimeoutException">Thrown when the extraction exceeds the specified timeout.</exception>
	Task<int> ExtractAsync(string source, string destination, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Extracts a 7z compressed file to the specified destination directory.
	/// </summary>
	/// <param name="source">The source file to extract.</param>
	/// <param name="destination">The directory where the contents will be extracted.</param>
	/// <param name="timeout">The maximum time to wait for the extraction to complete. Default is 30 seconds.</param>
	/// <returns>The exit code of the extraction process.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	/// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
	/// <exception cref="TimeoutException">Thrown when the extraction exceeds the specified timeout.</exception>
	int Extract(string source, string destination, TimeSpan? timeout = null);
}