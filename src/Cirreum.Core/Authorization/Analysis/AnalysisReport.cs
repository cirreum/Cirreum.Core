namespace Cirreum.Authorization.Analysis;

/// <summary>
/// Represents a complete analysis report containing issues, metrics, and analyzed categories.
/// </summary>
public record AnalysisReport {

	/// <summary>
	/// private constructor, use <see cref="ForCategory(string, List{AnalysisIssue}?, Dictionary{string, object}?)"/>
	/// or <see cref="Combine(List{AnalysisReport})"/>
	/// </summary>
	private AnalysisReport() {

	}

	/// <summary>
	/// Gets whether the analysis found any issues.
	/// </summary>
	public bool HasIssues => this.Issues.Count > 0;

	/// <summary>
	/// Gets the list of issues found during analysis.
	/// </summary>
	public List<AnalysisIssue> Issues { get; init; } = [];

	/// <summary>
	/// Gets additional metrics collected during analysis.
	/// </summary>
	public Dictionary<string, object> Metrics { get; init; } = [];

	/// <summary>
	/// Gets the set of analyzer categories that were run.
	/// </summary>
	public HashSet<string> AnalyzerCategories { get; init; } = [];

	/// <summary>
	/// Creates a new AnalysisReport for a single analyzer category.
	/// </summary>
	public static AnalysisReport ForCategory(
		string category,
		List<AnalysisIssue>? issues = null,
		Dictionary<string, object>? metrics = null) {
		return new AnalysisReport {
			Issues = issues ?? [],
			Metrics = metrics ?? [],
			AnalyzerCategories = [category]
		};
	}

	/// <summary>
	/// Combines multiple analysis reports into a single report.
	/// </summary>
	public static AnalysisReport Combine(List<AnalysisReport> reports) {
		return new AnalysisReport {
			Issues = [.. reports.SelectMany(r => r.Issues)],
			Metrics = reports
				.SelectMany(r => r.Metrics)
				.GroupBy(kvp => kvp.Key)
				.ToDictionary(g => g.Key, g => g.Last().Value),
			AnalyzerCategories = [.. reports.SelectMany(r => r.AnalyzerCategories)]
		};
	}

}