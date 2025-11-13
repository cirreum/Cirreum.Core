namespace Cirreum.FileSystem;

public interface IWasmFileSystem {
	/// <summary>
	/// Initialize the JS Interop
	/// </summary>
	/// <returns>An awaitable <see cref="ValueTask"/></returns>
	ValueTask InitializeAsync();
	/// <summary>
	/// Trigger a file download
	/// </summary>
	/// <param name="data"></param>
	/// <param name="fileName"></param>
	/// <param name="contentType"></param>
	Task DownloadFileAsync(byte[] data, string fileName, string contentType = "application/octet-stream");
}