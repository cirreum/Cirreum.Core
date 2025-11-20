namespace Cirreum.FileSystem;

/// <summary>
/// Defines the interface for MAUI Hybrid file system operations that bridge
/// native platform capabilities with web-based UI.
/// </summary>
public interface IMauiHybridFileSystem {
	/// <summary>
	/// Gets the path to the app's local data directory on the native platform.
	/// </summary>
	/// <returns>The absolute path to the local data directory.</returns>
	string GetLocalDataPath();

	/// <summary>
	/// Gets the path to the app's cache directory on the native platform.
	/// </summary>
	/// <returns>The absolute path to the cache directory.</returns>
	string GetCachePath();

	/// <summary>
	/// Gets the path to the app's temporary directory on the native platform.
	/// </summary>
	/// <returns>The absolute path to the temporary directory.</returns>
	string GetTempPath();

	/// <summary>
	/// Prompts the user to select a file from the native file picker.
	/// </summary>
	/// <param name="fileTypes">Optional file type filters (e.g., ".pdf", ".jpg").</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>The selected file result, or null if cancelled.</returns>
	Task<FilePickerResult?> PickFileAsync(IEnumerable<string>? fileTypes = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Prompts the user to select multiple files from the native file picker.
	/// </summary>
	/// <param name="fileTypes">Optional file type filters (e.g., ".pdf", ".jpg").</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>The collection of selected file results.</returns>
	Task<IEnumerable<FilePickerResult>> PickMultipleFilesAsync(IEnumerable<string>? fileTypes = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Prompts the user to save a file to the native file system.
	/// </summary>
	/// <param name="fileName">The suggested file name.</param>
	/// <param name="data">The file content as a byte array.</param>
	/// <param name="contentType">The MIME type of the content.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>True if the file was saved successfully; otherwise false.</returns>
	Task<bool> SaveFileAsync(string fileName, byte[] data, string contentType = "application/octet-stream", CancellationToken cancellationToken = default);

	/// <summary>
	/// Shares a file using the native platform's share capabilities.
	/// </summary>
	/// <param name="filePath">The path to the file to share.</param>
	/// <param name="title">Optional title for the share dialog.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>True if the share was successful; otherwise false.</returns>
	Task<bool> ShareFileAsync(string filePath, string? title = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens a file with the default native application.
	/// </summary>
	/// <param name="filePath">The path to the file to open.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>True if the file was opened successfully; otherwise false.</returns>
	Task<bool> OpenFileAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a file selected from the native file picker.
/// </summary>
public sealed class FilePickerResult {
	/// <summary>
	/// Gets the file name.
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// Gets the full file path (may not be accessible on all platforms).
	/// </summary>
	public string? FullPath { get; init; }

	/// <summary>
	/// Gets the content type of the file.
	/// </summary>
	public string? ContentType { get; init; }

	/// <summary>
	/// Opens a read stream for the file.
	/// </summary>
	/// <returns>A stream for reading the file content.</returns>
	public required Func<Task<Stream>> OpenReadAsync { get; init; }
}