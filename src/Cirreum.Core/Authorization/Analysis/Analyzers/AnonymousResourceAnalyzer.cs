namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Visualization;

/// <summary>
/// Analyzes anonymous resources and detects security gaps.
/// Identifies truly anonymous resources (don't require authorization) and
/// resources that require authorization but are missing an authorizer.
/// </summary>
public class AnonymousResourceAnalyzer : IAuthorizationAnalyzer {

	public const string AnalyzerCategory = "Anonymous Resources";

	private static readonly string[] SuspiciousWords = [
		"delete", "remove", "admin", "update", "modify", "create", "getall",
		"grant", "revoke", "permission", "role", "user", "account",
		"password", "secret", "key", "token", "auth", "logout", "signout"
	];

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		// Get all anonymous resources
		var anonymousResources = AuthorizationRuleProvider.Instance.GetAnonymousResources();

		var suspiciousAnonymous = anonymousResources
			.Where(r => IsPotentiallySecuritySensitive(r.ResourceType.Name))
			.ToList();

		// Metrics
		metrics[$"{MetricCategories.AnonymousResources}AnonymousResourceCount"] = anonymousResources.Count;
		metrics[$"{MetricCategories.AnonymousResources}SuspiciousResourceCount"] = suspiciousAnonymous.Count;

		// Report anonymous resources for review (informational)
		if (anonymousResources.Count > 0) {
			const int maxTypesToInclude = 5;
			var description = anonymousResources.Count > maxTypesToInclude
				? $"Found {anonymousResources.Count} anonymous resources (don't require authorization). Showing first {maxTypesToInclude}."
				: $"Found {anonymousResources.Count} anonymous resources (don't require authorization).";

			var typeSample = anonymousResources
				.Take(maxTypesToInclude)
				.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)
				.ToList();

			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: description,
				RelatedTypeNames: typeSample));
		}

		// Flag potentially suspicious anonymous resources
		if (suspiciousAnonymous.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {suspiciousAnonymous.Count} anonymous resources with sensitive-sounding names (Delete, Admin, Update, etc.)",
				RelatedTypeNames: [.. suspiciousAnonymous.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)]));
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);

	}

	private static bool IsPotentiallySecuritySensitive(string typeName) {
		var lowerName = typeName.ToLowerInvariant();
		return SuspiciousWords.Any(word => lowerName.Contains(word));
	}

}
