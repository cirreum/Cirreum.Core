namespace Cirreum.Authorization.Visualization;

using Cirreum.Authorization.Analysis;
using System.Linq;
using System.Text;

/// <summary>
/// Custom formatters of an <see cref="AnalysisReport"/>.
/// </summary>
public static class AnalysisReportFormatters {

	/// <summary>
	/// Generates a Markdown report.
	/// </summary>
	/// <param name="report">The <see cref="AnalysisReport"/> to format.</param>
	/// <returns>The formatted string.</returns>
	public static string ToMarkdown(this AnalysisReport report) {

		var markdownBuilder = new StringBuilder();

		// Overall Status
		markdownBuilder.AppendLine("# Analysis Report");
		markdownBuilder.AppendLine();
		markdownBuilder.AppendLine("## Overall Status");
		markdownBuilder.AppendLine();
		markdownBuilder.AppendLine($"- **Issues Found**: {(report.HasIssues ? "Yes" : "No")}");
		markdownBuilder.AppendLine($"- **Total Issues**: {report.Issues.Count}");
		markdownBuilder.AppendLine();

		// Quick Summary
		markdownBuilder.AppendLine("### Summary");
		markdownBuilder.AppendLine();
		var errorCount = report.Issues.Count(i => i.Severity == IssueSeverity.Error);
		var warningCount = report.Issues.Count(i => i.Severity == IssueSeverity.Warning);
		var infoCount = report.Issues.Count(i => i.Severity == IssueSeverity.Info);

		markdownBuilder.AppendLine($"- 🔴 **Error**: {errorCount}");
		markdownBuilder.AppendLine($"- 🟡 **Warning**: {warningCount}");
		markdownBuilder.AppendLine($"- 🟢 **Info**: {infoCount}");

		// Detailed Issues Grouped by Category then by Severity
		markdownBuilder.AppendLine();
		markdownBuilder.AppendLine("## Detailed Issues");
		foreach (var category in report.AnalyzerCategories.OrderBy(c => c)) {
			markdownBuilder.AppendLine();
			markdownBuilder.AppendLine($"### <span style=\"color: royalblue;\">{category}</span>");
			markdownBuilder.AppendLine();
			var categoryIssues = report.Issues.Where(i => i.Category == category);

			if (!categoryIssues.Any()) {
				markdownBuilder.AppendLine("- No issues found");
				continue;
			}

			// Within each category, group issues by Severity
			foreach (var severityGroup in categoryIssues.GroupBy(issue => issue.Severity)) {
				// Get a color based on severity (red for errors, orange for warnings, green for info, etc.)
				var severityColor = GetSeverityColor(severityGroup.Key);
				markdownBuilder.AppendLine();
				markdownBuilder.AppendLine($"#### Severity: <span style=\"color: {severityColor};\">{severityGroup.Key}</span>");
				markdownBuilder.AppendLine();
				var issueIndex = 1;
				foreach (var issue in severityGroup) {
					markdownBuilder.AppendLine($"- **Issue {issueIndex++}:** {issue.Description}");
					if (issue.RelatedObjects != null && issue.RelatedObjects.Any()) {
						markdownBuilder.AppendLine($"  - **Related Objects**: {string.Join(", ", issue.RelatedObjects)}");
					}
				}
			}

		}

		// Metrics
		if (report.Metrics.Count != 0) {
			markdownBuilder.AppendLine();
			markdownBuilder.AppendLine("## Metrics");
			markdownBuilder.AppendLine();
			foreach (var metric in report.Metrics) {
				markdownBuilder.AppendLine($"- **{metric.Key}**: {metric.Value}");
			}
		}

		// Analysis Insights
		markdownBuilder.AppendLine();
		markdownBuilder.AppendLine("## Analysis Insights");
		markdownBuilder.AppendLine();
		if (report.Issues.Count != 0) {
			var mostFrequentIssueType = report.Issues
				.GroupBy(i => i.Category)
				.OrderByDescending(g => g.Count())
				.First()
				.Key;
			var uniqueIssueTypesCount = report.Issues.Select(i => i.Category).Distinct().Count();
			var severityDistribution = report.Issues
				.GroupBy(i => i.Severity)
				.OrderByDescending(g => g.Count())
				.ToDictionary(g => g.Key, g => g.Count());

			markdownBuilder.AppendLine($"- **Most Frequent Issue Type**: {mostFrequentIssueType}");
			markdownBuilder.AppendLine($"- **Total Unique Issue Types**: {uniqueIssueTypesCount}");
			markdownBuilder.AppendLine("- **Severity Distribution**:");
			foreach (var (severity, count) in severityDistribution) {
				markdownBuilder.AppendLine($"  - {severity}: {count} ({(count * 100.0 / report.Issues.Count):F1}%)");
			}
		} else {
			markdownBuilder.AppendLine("- No issues found.");
		}

		return markdownBuilder.ToString();

		// Local function to map severity to a color.
		static string GetSeverityColor(IssueSeverity severity) {
			return severity switch {
				IssueSeverity.Error => "red",
				IssueSeverity.Warning => "orange",
				IssueSeverity.Info => "limegreen",
				_ => "black"
			};
		}

	}

	/// <summary>
	/// Generates a Text report.
	/// </summary>
	/// <param name="report">The <see cref="AnalysisReport"/> to format.</param>
	/// <returns>The formatted string.</returns>
	public static string ToText(this AnalysisReport report) {
		var textBuilder = new StringBuilder();

		// Report Header
		textBuilder.AppendLine("╔══════════════════════════════════════════╗");
		textBuilder.AppendLine("║           ANALYSIS REPORT                ║");
		textBuilder.AppendLine("╚══════════════════════════════════════════╝");
		textBuilder.AppendLine();

		// Status Overview
		textBuilder.AppendLine("Status Overview:");
		textBuilder.AppendLine("----------------");
		textBuilder.AppendLine($"* Issues Found: {(report.HasIssues ? "Yes" : "No")}");
		textBuilder.AppendLine($"* Total Issues: {report.Issues.Count}");
		textBuilder.AppendLine($"* Has Critical Issues: {report.Issues.Any(i => i.Severity == IssueSeverity.Error)}");
		textBuilder.AppendLine();

		// Detailed Analysis by Category
		textBuilder.AppendLine("Detailed Analysis:");
		textBuilder.AppendLine("------------------");

		// Materialize categories: if AnalyzerCategories is empty, fall back on the categories in Issues.
		var categories = report.AnalyzerCategories.Count != 0
			? report.AnalyzerCategories.OrderBy(c => c).ToList()
			: [.. report.Issues.Select(i => i.Category).Distinct().OrderBy(c => c)];

		foreach (var category in categories) {
			textBuilder.AppendLine($"[{category}]");
			// Materialize issues in this category
			var categoryIssues = report.Issues
				.Where(i => i.Category == category)
				.ToList();

			if (categoryIssues.Count == 0) {
				textBuilder.AppendLine("  * No issues found");
			} else {
				// Group issues by severity, mirroring the Markdown logic.
				foreach (var severityGroup in categoryIssues.GroupBy(i => i.Severity).OrderByDescending(g => g.Key)) {
					textBuilder.AppendLine($"  {severityGroup.Key}:");
					var issueIndex = 1;
					foreach (var issue in severityGroup) {
						textBuilder.AppendLine($"    Issue {issueIndex++}: {issue.Description}");
						if (issue.RelatedObjects?.Any() == true) {
							textBuilder.AppendLine($"      Related: {string.Join(", ", issue.RelatedObjects)}");
						}
					}
				}
			}
			textBuilder.AppendLine();
		}

		// Metrics Summary
		if (report.Metrics.Count != 0) {
			textBuilder.AppendLine("Metrics Summary:");
			textBuilder.AppendLine("----------------");
			foreach (var metric in report.Metrics.OrderBy(m => m.Key)) {
				textBuilder.AppendLine($"{metric.Key}: {metric.Value}");
			}
			textBuilder.AppendLine();
		}

		// Analysis Insights
		textBuilder.AppendLine("Analysis Insights:");
		textBuilder.AppendLine("------------------");
		if (report.Issues.Count != 0) {
			var mostFrequentIssueType = report.Issues
				.GroupBy(i => i.Category)
				.OrderByDescending(g => g.Count())
				.First().Key;
			var uniqueIssueTypesCount = report.Issues.Select(i => i.Category).Distinct().Count();

			textBuilder.AppendLine($"* Most Frequent Issue Type: {mostFrequentIssueType}");
			textBuilder.AppendLine($"* Total Unique Issue Types: {uniqueIssueTypesCount}");
		} else {
			textBuilder.AppendLine("* No issues found in any category");
		}

		return textBuilder.ToString();
	}

	/// <summary>
	/// Generates a Csv report.
	/// </summary>
	/// <param name="report">The <see cref="AnalysisReport"/> to format.</param>
	/// <returns>The formatted string.</returns>
	public static string ToCsv(this AnalysisReport report) {

		var csvBuilder = new StringBuilder();

		// Issues CSV
		csvBuilder.AppendLine("Category,Severity,Description,RelatedObjects");
		foreach (var issue in report.Issues) {
			csvBuilder.AppendLine(
				$"{EscapeCsvField(issue.Category)}," +
				$"{EscapeCsvField(issue.Severity)}," +
				$"{EscapeCsvField(issue.Description)}," +
				$"{EscapeCsvField(issue.RelatedObjects != null ? string.Join("; ", issue.RelatedObjects) : string.Empty)}"
			);
		}

		// Metrics CSV
		if (report.Metrics.Count != 0) {
			csvBuilder.AppendLine();
			csvBuilder.AppendLine("Metric,Value");
			foreach (var metric in report.Metrics) {
				csvBuilder.AppendLine($"{EscapeCsvField(metric.Key)},{EscapeCsvField(metric.Value)}");
			}
		}

		// Analysis Insights CSV
		csvBuilder.AppendLine();
		csvBuilder.AppendLine("Insight,Value");
		if (report.Issues.Count != 0) {
			var mostFrequentIssueType = report.Issues
				.GroupBy(i => i.Category)
				.OrderByDescending(g => g.Count())
				.First()
				.Key;
			var uniqueIssueTypesCount = report.Issues.Select(i => i.Category).Distinct().Count();

			csvBuilder.AppendLine($"{EscapeCsvField("Most Frequent Issue Type")},{EscapeCsvField(mostFrequentIssueType)}");
			csvBuilder.AppendLine($"{EscapeCsvField("Total Unique Issue Types")},{EscapeCsvField(uniqueIssueTypesCount)}");
		} else {
			csvBuilder.AppendLine($"{EscapeCsvField("Analysis Insights")},{EscapeCsvField("No issues found")}");
		}

		return csvBuilder.ToString();
	}

	private static string EscapeCsvField(object value) {
		if (value == null) {
			return string.Empty;
		}

		var stringValue = value.ToString();
		if (string.IsNullOrWhiteSpace(stringValue)) {
			return string.Empty;
		}

		// Escape quotes and wrap in quotes if needed
		if (stringValue.Contains('"') || stringValue.Contains(',') || stringValue.Contains('\n')) {
			stringValue = $"\"{stringValue.Replace("\"", "\"\"")}\"";
		}

		return stringValue;
	}

}