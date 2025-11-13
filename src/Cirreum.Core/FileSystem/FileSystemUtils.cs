namespace Cirreum.FileSystem;

public static class FileSystemUtils {

	private static readonly string defaultSearchPattern = "*";

	/// <summary>
	/// Validates and normalizes search patterns, ensuring consistent behavior.
	/// </summary>
	/// <param name="patterns">Single pattern or enumerable of patterns</param>
	/// <returns>Normalized collection of search patterns</returns>
	public static IEnumerable<string> NormalizeSearchPatterns(params string?[] patterns) {

		if (patterns == null || patterns.Length == 0) {
			return [defaultSearchPattern];
		}

		return patterns
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Select(p => p ?? defaultSearchPattern) // More explicit null handling
			.DefaultIfEmpty(defaultSearchPattern);

	}

}