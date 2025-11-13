namespace Cirreum.FileSystem;

/// <summary>
/// Represents the type of a filesystem path.
/// </summary>
public enum PathType {
	/// <summary>
	/// The path does not exist on the filesystem.
	/// </summary>
	NotFound,

	/// <summary>
	/// The path represents a directory.
	/// </summary>
	Directory,

	/// <summary>
	/// The path represents a file.
	/// </summary>
	File
}