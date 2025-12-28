namespace Cirreum.Authorization.Analysis;

/// <summary>
/// Configuration options for authorization analysis.
/// </summary>
public record AnalysisOptions {
	/// <summary>
	/// Maximum recommended role hierarchy depth.
	/// </summary>
	public int MaxHierarchyDepth { get; init; } = 10;

	/// <summary>
	/// Whether to include informational issues in the analysis.
	/// </summary>
	public bool IncludeInfoIssues { get; init; } = true;

	/// <summary>
	/// Analyzer categories to exclude from analysis.
	/// </summary>
	public HashSet<string> ExcludedCategories { get; init; } = [];

	/// <summary>
	/// Gets the default analysis options.
	/// </summary>
	public static AnalysisOptions Default { get; } = new();
}