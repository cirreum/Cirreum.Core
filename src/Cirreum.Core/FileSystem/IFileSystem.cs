namespace Cirreum.FileSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines the interface for a File System implementation.
/// </summary>
public interface IFileSystem {

	/// <summary>
	/// Determines whether the specified file exists.
	/// </summary>
	/// <param name="path">The file to check.</param>
	/// <returns>
	/// <see langword="true"/> if the caller has the required permissions and <paramref name="path"/>
	/// contains the name of an existing file; otherwise, <see langword="false"/>. This method also
	/// returns <see langword="false"/> if <paramref name="path"/> is null, an invalid path, or a zero-length
	/// string. If the caller does not have sufficient permissions to read the specified file, no exception
	/// is thrown and the method returns <see langword="false"/> regardless of the existence of path.
	/// </returns>
	bool FileExists(string path);

	/// <summary>
	/// Determines whether the given path refers to an existing directory on disk.
	/// </summary>
	/// <param name="path">The path to test.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="path"/> refers to an existing directory;
	/// <see langword="false"/> if the directory does not exist or an error occurs when trying
	/// to determine if the specified directory exists.
	/// </returns>
	bool DirectoryExists(string path);

	/// <summary>
	/// Attempts to create a new directory using the specified <paramref name="path"/>.
	/// </summary>
	/// <param name="path">The path of the new directory.</param>
	/// <returns><see langword="true"/> if directory was successfully created; otherwise <see langword="false"/></returns>
	bool EnsureDirectory(string path);

	/// <summary>
	/// Queries the specified path for any files.
	/// </summary>
	/// <param name="path">The path to the directory.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of files in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="includeChildDirectories">Optionally, search subdirectories.</param>
	/// <returns>The collection of file names.</returns>
	string[] GetFiles(string path, string searchPattern, bool includeChildDirectories);


	/// <summary>
	/// Queries the specified paths for any files.
	/// </summary>
	/// <param name="paths">One or more paths to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of files in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of files to return. Zero (0) indicates no limits and is the default.</param>
	/// <returns>An enumerable collection of file paths.</returns>
	IEnumerable<string> QueryFiles(string[] paths, bool includeChildDirectories, string searchPattern, Func<string, bool>? predicate = null, int take = 0);

	/// <summary>
	/// Queries the specified path for any files.
	/// </summary>
	/// <param name="path">The relative or absolute path to the directory to search. This string is not case-sensitive.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of files in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of files to return. Zero (0) indicates no limits and is the default.</param>
	/// <returns>An enumerable collection of file paths.</returns>
	IEnumerable<string> QueryFiles(string path, bool includeChildDirectories, string searchPattern, Func<string, bool>? predicate = null, int take = 0);

	/// <summary>
	/// Queries the specified path for any files.
	/// </summary>
	/// <param name="path">The relative or absolute path to the directory to query. This string is not case-sensitive.</param>
	/// <param name="includeChildDirectories">The option to query subdirectories.</param>
	/// <param name="searchPatterns">
	/// The array of search strings to match against the names of files in path. 
	/// The search patterns can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional predicate, to filter the results.</param>
	/// <param name="take">The optional, limit on the number of files to return.</param>
	/// <returns>An enumerable collection of file paths.</returns>
	/// <remarks>
	/// When multiple search patterns are provided with a take limit, the specific files 
	/// returned may vary between executions depending on the implementation.
	/// </remarks>
	IEnumerable<string> QueryFiles(string path, bool includeChildDirectories, IEnumerable<string> searchPatterns, Func<string, bool>? predicate = null, int take = 0);

	/// <summary>
	/// Asynchronously queries the specified paths for any files.
	/// </summary>
	/// <param name="paths">One or more paths to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of files in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of files to return. Zero (0) indicates no limits and is the default.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An async enumerable collection of file paths that can be awaited in a foreach loop.</returns>
	IAsyncEnumerable<string> QueryFilesAsync(
		string[] paths,
		bool includeChildDirectories,
		string searchPattern,
		Func<string, bool>? predicate = null,
		int take = 0,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Asynchronously queries the specified path for any files.
	/// </summary>
	/// <param name="path">The relative or absolute path to the directory to search. This string is not case-sensitive.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of files in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of files to return. Zero (0) indicates no limits and is the default.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An async enumerable collection of file paths that can be awaited in a foreach loop.</returns>
	IAsyncEnumerable<string> QueryFilesAsync(
		string path,
		bool includeChildDirectories,
		string searchPattern,
		Func<string, bool>? predicate = null,
		int take = 0,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Asynchronously queries the specified path for any files.
	/// </summary>
	/// <param name="path">The relative or absolute path to the directory to query. This string is not case-sensitive.</param>
	/// <param name="includeChildDirectories">The option to query subdirectories.</param>
	/// <param name="searchPatterns">
	/// The array of search strings to match against the names of files in path. 
	/// The search patterns can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional predicate, to filter the results.</param>
	/// <param name="take">The optional, limit on the number of files to return.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An async enumerable collection of file paths that can be awaited in a foreach loop.</returns>
	/// <remarks>
	/// When multiple search patterns are provided with a take limit, the specific files 
	/// returned may vary between executions depending on the implementation.
	/// </remarks>
	IAsyncEnumerable<string> QueryFilesAsync(
		string path,
		bool includeChildDirectories,
		IEnumerable<string> searchPatterns,
		Func<string, bool>? predicate = null,
		int take = 0,
		CancellationToken cancellationToken = default);


	/// <summary>
	/// Queries the specified path for any directories.
	/// </summary>
	/// <param name="paths">One or more paths to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of directories in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of directories to return. Zero (0) indicates no limits and is the default.</param>
	/// <returns>An enumerable collection of directory paths.</returns>
	IEnumerable<string> QueryDirectories(string[] paths, bool includeChildDirectories, string searchPattern, Func<string, bool>? predicate = null, int take = 0);

	/// <summary>
	/// Queries the specified path for any directories.
	/// </summary>
	/// <param name="path">The path to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of directories in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of directories to return. Zero (0) indicates no limits and is the default.</param>
	/// <returns>An enumerable of directory paths.</returns>
	IEnumerable<string> QueryDirectories(string path, bool includeChildDirectories, string searchPattern, Func<string, bool>? predicate = null, int take = 0);

	/// <summary>
	/// Queries the specified path for any directories.
	/// </summary>
	/// <param name="path">The path to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPatterns">
	/// The array of search strings to match against the names of files in path. 
	/// The search patterns can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of directories to return. Zero (0) indicates no limits and is the default.</param>
	/// <returns>An enumerable collection of directory paths.</returns>
	/// <remarks>
	/// When multiple search patterns are provided with a take limit, the specific directories 
	/// returned may vary between executions depending on the implementation.
	/// </remarks>
	IEnumerable<string> QueryDirectories(string path, bool includeChildDirectories, IEnumerable<string> searchPatterns, Func<string, bool>? predicate = null, int take = 0);


	/// <summary>
	/// Asynchronously queries the specified paths for any directories.
	/// </summary>
	/// <param name="paths">One or more paths to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of directories in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of directories to return. Zero (0) indicates no limits and is the default.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An async enumerable collection of directory paths that can be awaited in a foreach loop.</returns>
	IAsyncEnumerable<string> QueryDirectoriesAsync(
		string[] paths,
		bool includeChildDirectories,
		string searchPattern,
		Func<string, bool>? predicate = null,
		int take = 0,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Asynchronously queries the specified path for any directories.
	/// </summary>
	/// <param name="path">The path to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPattern">
	/// The optional search string to match against the names of directories in path. 
	/// This parameter can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of directories to return. Zero (0) indicates no limits and is the default.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An async enumerable collection of directory paths that can be awaited in a foreach loop.</returns>
	IAsyncEnumerable<string> QueryDirectoriesAsync(
		string path,
		bool includeChildDirectories,
		string searchPattern,
		Func<string, bool>? predicate = null,
		int take = 0,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Asynchronously queries the specified path for any directories.
	/// </summary>
	/// <param name="path">The path to query.</param>
	/// <param name="includeChildDirectories">The option to search subdirectories.</param>
	/// <param name="searchPatterns">
	/// The array of search strings to match against the names of directories in path. 
	/// The search patterns can contain a combination of valid literal path and
	/// wildcard (* and ?) characters, but it doesn't support regular expressions.
	/// </param>
	/// <param name="predicate">The optional, predicate, to filter the results.</param>
	/// <param name="take">The optional, number of directories to return. Zero (0) indicates no limits and is the default.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An async enumerable collection of directory paths that can be awaited in a foreach loop.</returns>
	/// <remarks>
	/// When multiple search patterns are provided with a take limit, the specific directories 
	/// returned may vary between executions depending on the implementation.
	/// </remarks>
	IAsyncEnumerable<string> QueryDirectoriesAsync(
		string path,
		bool includeChildDirectories,
		IEnumerable<string> searchPatterns,
		Func<string, bool>? predicate = null,
		int take = 0,
		CancellationToken cancellationToken = default);


	/// <summary>
	/// Extracts a .Zip file to the specified location.
	/// </summary>
	/// <param name="source">The source Zip file (must have a .zip extension).</param>
	/// <param name="destination">The path to a destination directory.</param>
	/// <param name="overwriteFiles"><see langword="true"/> to overwrite existing files in the destination; otherwise <see langword="false"/>.</param>
	void ExtractZipFile(string source, string destination, bool overwriteFiles);

	/// <summary>
	/// Asynchronously extracts a .Zip file to the specified location.
	/// </summary>
	/// <param name="source">The source Zip file (must have a .zip extension).</param>
	/// <param name="destination">The path to a destination directory.</param>
	/// <param name="overwriteFiles"><see langword="true"/> to overwrite existing files in the destination; otherwise <see langword="false"/>.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous extraction operation.</returns>
	Task ExtractZipFileAsync(string source, string destination, bool overwriteFiles, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the specified file if it exists.
	/// </summary>
	/// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
	/// <exception cref="ArgumentException">
	/// path is a zero-length string, contains only white space, or contains one or more
	/// invalid characters as defined by System.IO.Path.InvalidPathChars.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// path is null.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The specified path is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="IOException">
	/// The specified file is in use. -or- There is an open handle on the file, and the
	/// operating system is Windows XP or earlier. This open handle can result from enumerating
	/// directories and files. For more information, see How to: Enumerate Directories
	/// and Files.
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// path is in an invalid format.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- The file is an executable
	/// file that is in use. -or- path is a directory. -or- path specified is a read-only
	/// file.
	/// </exception>
	void DeleteFile(string path);

	/// <summary>
	/// Asynchronously deletes the specified file if it exists.
	/// </summary>
	/// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	/// <exception cref="ArgumentException">
	/// path is a zero-length string, contains only white space, or contains one or more
	/// invalid characters as defined by System.IO.Path.InvalidPathChars.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// path is null.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The specified path is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="IOException">
	/// The specified file is in use. -or- There is an open handle on the file, and the
	/// operating system is Windows XP or earlier. This open handle can result from enumerating
	/// directories and files. For more information, see How to: Enumerate Directories
	/// and Files.
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// path is in an invalid format.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- The file is an executable
	/// file that is in use. -or- path is a directory. -or- path specified is a read-only
	/// file.
	/// </exception>
	Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the specified file if it exists, including support
	/// for retry, for scenarios where the caller is waiting for
	/// the file to become available.
	/// </summary>
	/// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
	/// <exception cref="ArgumentException">
	/// path is a zero-length string, contains only white space, or contains one or more
	/// invalid characters as defined by System.IO.Path.InvalidPathChars.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// path is null.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The specified path is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="IOException">
	/// The specified file is in use. -or- There is an open handle on the file, and the
	/// operating system is Windows XP or earlier. This open handle can result from enumerating
	/// directories and files. For more information, see How to: Enumerate Directories
	/// and Files.
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// path is in an invalid format.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- The file is an executable
	/// file that is in use. -or- path is a directory. -or- path specified is a read-only
	/// file.
	/// </exception>
	void DeleteFileWithRetry(string path);

	/// <summary>
	/// Asynchronously deletes the specified file if it exists, including support
	/// for retry, for scenarios where the caller is waiting for
	/// the file to become available.
	/// </summary>
	/// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous delete operation with retry logic.</returns>
	/// <exception cref="ArgumentException">
	/// path is a zero-length string, contains only white space, or contains one or more
	/// invalid characters as defined by System.IO.Path.InvalidPathChars.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// path is null.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The specified path is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="IOException">
	/// The specified file is in use. -or- There is an open handle on the file, and the
	/// operating system is Windows XP or earlier. This open handle can result from enumerating
	/// directories and files. For more information, see How to: Enumerate Directories
	/// and Files.
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// path is in an invalid format.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- The file is an executable
	/// file that is in use. -or- path is a directory. -or- path specified is a read-only
	/// file.
	/// </exception>
	Task DeleteFileWithRetryAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	///  Deletes the specified directory if it exists and if indicated, any subdirectories and files
	///  in the directory.
	/// </summary>
	/// <param name="path">The path of the directory to delete.</param>
	/// <param name="recursive">
	/// <see langword="true"/> to delete directories, subdirectories, and files in path; 
	/// otherwise, false.
	/// </param>
	/// <exception cref="IOException">
	/// A file with the same name and location specified by path exists. -or- The directory
	/// specified by path is read-only, or recursive is false and path is not an empty
	/// directory. -or- The directory is the application's current working directory.
	/// -or- The directory contains a read-only file. -or- The directory is being used
	/// by another process.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="path"/> is a zero-length string, contains only white space, or
	/// contains one or more invalid characters as defined by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="path"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified <paramref name="path"/> or one of its subdirectories or files, exceed the
	/// system-defined maximum length.
	/// </exception>
	void DeleteDirectory(string path, bool recursive);

	/// <summary>
	///  Asynchronously deletes the specified directory if it exists and if indicated, any subdirectories and files
	///  in the directory.
	/// </summary>
	/// <param name="path">The path of the directory to delete.</param>
	/// <param name="recursive">
	/// <see langword="true"/> to delete directories, subdirectories, and files in path; 
	/// otherwise, false.
	/// </param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	/// <exception cref="IOException">
	/// A file with the same name and location specified by path exists. -or- The directory
	/// specified by path is read-only, or recursive is false and path is not an empty
	/// directory. -or- The directory is the application's current working directory.
	/// -or- The directory contains a read-only file. -or- The directory is being used
	/// by another process.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="path"/> is a zero-length string, contains only white space, or
	/// contains one or more invalid characters as defined by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="path"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified <paramref name="path"/> or one of its subdirectories or files, exceed the
	/// system-defined maximum length.
	/// </exception>
	Task DeleteDirectoryAsync(string path, bool recursive, CancellationToken cancellationToken = default);

	/// <summary>
	///  Deletes any child directories and files, from the specified parent directory, if it exists.
	/// </summary>
	/// <param name="rootPath">The path of the parent directory.</param>
	/// <exception cref="IOException">
	/// A file with the same name and location specified by path exists. -or- The directory
	/// specified by path is read-only, -or- The directory is the application's current working
	/// directory. -or- The directory contains a read-only file. -or- The directory is being used
	/// by another process.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="rootPath"/> is a zero-length string, contains only white space, or
	/// contains one or more invalid characters as defined by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="rootPath"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified <paramref name="rootPath"/> or one of its subdirectories or files, exceed the
	/// system-defined maximum length.
	/// </exception>
	void DeleteChildDirectories(string rootPath);

	/// <summary>
	///  Asynchronously deletes any child directories and files, from the specified parent directory, if it exists.
	/// </summary>
	/// <param name="rootPath">The path of the parent directory.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	/// <exception cref="IOException">
	/// A file with the same name and location specified by path exists. -or- The directory
	/// specified by path is read-only, -or- The directory is the application's current working
	/// directory. -or- The directory contains a read-only file. -or- The directory is being used
	/// by another process.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="rootPath"/> is a zero-length string, contains only white space, or
	/// contains one or more invalid characters as defined by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="rootPath"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified <paramref name="rootPath"/> or one of its subdirectories or files, exceed the
	/// system-defined maximum length.
	/// </exception>
	Task DeleteChildDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default);

	/// <summary>
	/// Moves a specified file to a new location, providing the options to specify a new file name
	/// and to overwrite the destination file if it already exists.
	/// </summary>
	/// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
	/// <param name="destFileName">The new path and name for the file.</param>
	/// <param name="overwrite">
	/// <see langword="true"/> to overwrite the destination file if it already exists;
	/// otherwise, <see langword="false"/>.</param>
	/// <exception cref="IOException">
	/// <paramref name="destFileName"/> exists and overwrite is false. -or- An I/O error has occurred.
	/// </exception>
	/// <exception cref="FileNotFoundException">
	/// <paramref name="sourceFileName"/> was not found.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is null.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// -or- <paramref name="sourceFileName"/> or <paramref name="destFileName"/> specifies a directory.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- <paramref name="destFileName"/>
	/// is read-only. -or- overwrite is true, <paramref name="destFileName"/> exists and is hidden,
	/// but <paramref name="sourceFileName"/> is not hidden.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The path specified in <paramref name="destFileName"/>
	/// is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.
	/// </exception>
	void MoveFile(string sourceFileName, string destFileName, bool overwrite = false);

	/// <summary>
	/// Asynchronously moves a specified file to a new location, providing the options to specify a new file name
	/// and to overwrite the destination file if it already exists.
	/// </summary>
	/// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
	/// <param name="destFileName">The new path and name for the file.</param>
	/// <param name="overwrite">
	/// <see langword="true"/> to overwrite the destination file if it already exists;
	/// otherwise, <see langword="false"/>.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous move operation.</returns>
	/// <exception cref="IOException">
	/// <paramref name="destFileName"/> exists and overwrite is false. -or- An I/O error has occurred.
	/// </exception>
	/// <exception cref="FileNotFoundException">
	/// <paramref name="sourceFileName"/> was not found.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is null.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// -or- <paramref name="sourceFileName"/> or <paramref name="destFileName"/> specifies a directory.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- <paramref name="destFileName"/>
	/// is read-only. -or- overwrite is true, <paramref name="destFileName"/> exists and is hidden,
	/// but <paramref name="sourceFileName"/> is not hidden.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The path specified in <paramref name="destFileName"/>
	/// is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.
	/// </exception>
	Task MoveFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Moves a file or a directory and its contents to a new location.
	/// </summary>
	/// <param name="sourceDirName">The path of the file or directory to move.</param>
	/// <param name="destDirName">
	/// The path to the new location for <paramref name="sourceDirName"/>. If <paramref name="sourceDirName"/>
	/// is a file, then <paramref name="destDirName"/> must also be a file name.
	/// </param>
	/// <remarks>
	/// <para>
	/// This method creates a new directory with the name specified by destDirName and moves the contents of
	/// <paramref name="sourceDirName"/> to the newly created destination directory. If you try to move a
	/// directory to a directory that already exists, an <see cref="IOException"/> will occur. For example,
	/// an exception will occur if you try to move c:\mydir to c:\public, and c:\public already exists.
	/// Alternatively, you could specify "c:\\public\\mydir" as the destDirName parameter, provided that
	/// "mydir" does not exist under "c:\\public", or specify a new directory name such as "c:\\newdir".
	/// </para>
	/// <para>
	/// The <paramref name="sourceDirName"/> and <paramref name="destDirName"/> arguments are permitted to
	/// specify relative or absolute path information. Relative path information is interpreted as relative
	/// to the current working directory. To obtain the current working directory, see 
	/// <see cref="Directory.GetCurrentDirectory"/>.
	/// </para>
	/// <para>
	/// Trailing spaces are removed from the end of the path parameters before moving the directory.
	/// </para>
	/// <para>
	/// If the <paramref name="destDirName"/> directory or any sub-directories where created during the move
	/// operation and an exception occurs, this method will attempt to clean up, by deleting any directories
	/// it created; including any files it may have moved.
	/// </para>
	/// </remarks>
	/// <exception cref="IOException">
	/// An attempt was made to move a directory to a different volume.
	/// -or-
	/// <paramref name="destDirName"/> already exists. See the Note in the Remarks section.
	/// -or-
	/// The <paramref name="sourceDirName"/> and <paramref name="destDirName"/> parameters refer to the same file or directory.
	/// -or-
	/// The directory or a file within it is being used by another process.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="SecurityException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The path specified by <paramref name="sourceDirName"/> is invalid (for example, it is on an unmapped drive).
	/// </exception>
	void MoveDirectory(string sourceDirName, string destDirName);

	/// <summary>
	/// Asynchronously moves a file or a directory and its contents to a new location.
	/// </summary>
	/// <param name="sourceDirName">The path of the file or directory to move.</param>
	/// <param name="destDirName">
	/// The path to the new location for <paramref name="sourceDirName"/>. If <paramref name="sourceDirName"/>
	/// is a file, then <paramref name="destDirName"/> must also be a file name.
	/// </param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous move operation.</returns>
	/// <remarks>
	/// <para>
	/// This method creates a new directory with the name specified by destDirName and moves the contents of
	/// <paramref name="sourceDirName"/> to the newly created destination directory. If you try to move a
	/// directory to a directory that already exists, an <see cref="IOException"/> will occur. For example,
	/// an exception will occur if you try to move c:\mydir to c:\public, and c:\public already exists.
	/// Alternatively, you could specify "c:\\public\\mydir" as the destDirName parameter, provided that
	/// "mydir" does not exist under "c:\\public", or specify a new directory name such as "c:\\newdir".
	/// </para>
	/// <para>
	/// The <paramref name="sourceDirName"/> and <paramref name="destDirName"/> arguments are permitted to
	/// specify relative or absolute path information. Relative path information is interpreted as relative
	/// to the current working directory. To obtain the current working directory, see 
	/// <see cref="Directory.GetCurrentDirectory"/>.
	/// </para>
	/// <para>
	/// Trailing spaces are removed from the end of the path parameters before moving the directory.
	/// </para>
	/// <para>
	/// If the <paramref name="destDirName"/> directory or any sub-directories where created during the move
	/// operation and an exception occurs, this method will attempt to clean up, by deleting any directories
	/// it created; including any files it may have moved.
	/// </para>
	/// </remarks>
	/// <exception cref="IOException">
	/// An attempt was made to move a directory to a different volume.
	/// -or-
	/// <paramref name="destDirName"/> already exists. See the Note in the Remarks section.
	/// -or-
	/// The <paramref name="sourceDirName"/> and <paramref name="destDirName"/> parameters refer to the same file or directory.
	/// -or-
	/// The directory or a file within it is being used by another process.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="SecurityException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The path specified by <paramref name="sourceDirName"/> is invalid (for example, it is on an unmapped drive).
	/// </exception>
	Task MoveDirectoryAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
	/// </summary>
	/// <param name="sourceFileName">The file to copy.</param>
	/// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
	/// <param name="overwrite">
	/// <see langword="true"/> if the destination file can be overwritten; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- <paramref name="destFileName"/>
	/// is read-only. -or- overwrite is true, <paramref name="destFileName"/> exists and is hidden,
	/// but <paramref name="sourceFileName"/> is not hidden.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// -or- <paramref name="sourceFileName"/> or <paramref name="destFileName"/> specifies a directory.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The path specified in <paramref name="destFileName"/>
	/// is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="IOException">
	/// <paramref name="destFileName"/> exists and overwrite is false. -or- An I/O error has occurred.
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.
	/// </exception>
	void CopyFile(string sourceFileName, string destFileName, bool overwrite = false);

	/// <summary>
	/// Asynchronously copies an existing file to a new file. Overwriting a file of the same name is allowed.
	/// </summary>
	/// <param name="sourceFileName">The file to copy.</param>
	/// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
	/// <param name="overwrite">
	/// <see langword="true"/> if the destination file can be overwritten; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous copy operation.</returns>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission. -or- <paramref name="destFileName"/>
	/// is read-only. -or- overwrite is true, <paramref name="destFileName"/> exists and is hidden,
	/// but <paramref name="sourceFileName"/> is not hidden.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// -or- <paramref name="sourceFileName"/> or <paramref name="destFileName"/> specifies a directory.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="DirectoryNotFoundException">
	/// The path specified in <paramref name="destFileName"/>
	/// is invalid (for example, it is on an unmapped drive).
	/// </exception>
	/// <exception cref="IOException">
	/// <paramref name="destFileName"/> exists and overwrite is false. -or- An I/O error has occurred.
	/// </exception>
	/// <exception cref="NotSupportedException">
	/// <paramref name="sourceFileName"/> or <paramref name="destFileName"/> is in an invalid format.
	/// </exception>
	Task CopyFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Copies an existing directory's contents to another directory. Creating the
	/// <paramref name="destDirName"/> if it does not already exist.
	/// </summary>
	/// <param name="sourceDirName">The path of the directory to copy.</param>
	/// <param name="destDirName">The path to the new location.</param>
	/// <param name="copySubDirs"><see langword="true"/> to also copy any sub directories; otherwise false.</param>
	/// <param name="overwrite">
	/// <see langword="true"/> if the destination file can be overwritten; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="SecurityException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite = false);

	/// <summary>
	/// Asynchronously copies an existing directory's contents to another directory. Creating the
	/// <paramref name="destDirName"/> if it does not already exist.
	/// </summary>
	/// <param name="sourceDirName">The path of the directory to copy.</param>
	/// <param name="destDirName">The path to the new location.</param>
	/// <param name="copySubDirs"><see langword="true"/> to also copy any sub directories; otherwise false.</param>
	/// <param name="overwrite">
	/// <see langword="true"/> if the destination file can be overwritten; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous copy operation.</returns>
	/// <exception cref="ArgumentException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is a zero-length
	/// string, contains only white space, or contains one or more invalid characters as defined
	/// by <see cref="Path.InvalidPathChars"/>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is null.
	/// </exception>
	/// <exception cref="PathTooLongException">
	/// The specified path, file name, or both exceed the system-defined maximum length.
	/// </exception>
	/// <exception cref="SecurityException">
	/// The caller does not have the required permission.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// The caller does not have the required permission.
	/// </exception>
	Task CopyDirectoryAsync(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new file, writes the specified string to the file, and then closes
	/// the file. If the target file already exists, it is overwritten.
	/// </summary>
	/// <param name="path">The file to write to.</param>
	/// <param name="contents">The string to write to the file.</param>
	void WriteAllText(string path, string contents);

	/// <summary>
	/// Asynchronously creates a new file, writes the specified string to the file, and
	/// then closes the file. If the target file already exists, it is overwritten.
	/// </summary>
	/// <param name="path">The file to write to.</param>
	/// <param name="contents">The string to write to the file.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens a text file, reads all the text in the file, and then closes the file.
	/// </summary>
	/// <param name="path">The file to open for reading.</param>
	/// <returns>A string containing all the text in the file.</returns>
	string ReadAllText(string path);

	/// <summary>
	/// Asynchronously opens a text file, reads all the text in the file, and then closes
	/// the file.
	/// </summary>
	/// <param name="path">The file to open for reading.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous read operation, which wraps the string containing all text in the file.</returns>
	Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

}