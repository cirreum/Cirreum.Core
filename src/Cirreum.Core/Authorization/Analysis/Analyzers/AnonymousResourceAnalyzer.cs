namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Modeling;

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

	#region Issue Definitions

	private static class Issues {

		public static IssueDefinition AnonymousResourcesFound(int count, int maxShown) {
			var description = count > maxShown
				? $"Found {count} anonymous resources (don't require authorization). Showing first {maxShown}."
				: $"Found {count} anonymous resource(s) (don't require authorization).";

			var recommendation = count == 1
				? "Review this resource to confirm it should be publicly accessible without authentication."
				: "Review these resources to confirm they should be publicly accessible without authentication.";

			return new(description, recommendation);
		}

		public static IssueDefinition SuspiciousAnonymousResources(int count) {
			var recommendation = count == 1
				? "This operation has a name suggesting it may need protection. Review it and add authorization if it performs sensitive actions."
				: "These operations have names suggesting they may need protection (Delete, Admin, Update, etc.). Review each one and add authorization if they perform sensitive actions.";

			return new(
				$"Found {count} anonymous resource(s) with sensitive-sounding names (Delete, Admin, Update, etc.)",
				recommendation);
		}

	}

	#endregion

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		// Get all anonymous resources
		var anonymousResources = AuthorizationModel.Instance.GetAnonymousResources();

		var suspiciousAnonymous = anonymousResources
			.Where(r => IsPotentiallySecuritySensitive(r.ResourceType.Name))
			.ToList();

		// Metrics
		metrics[$"{MetricCategories.AnonymousResources}AnonymousResourceCount"] = anonymousResources.Count;
		metrics[$"{MetricCategories.AnonymousResources}SuspiciousResourceCount"] = suspiciousAnonymous.Count;

		// Report anonymous resources for review (informational)
		if (anonymousResources.Count > 0) {
			const int maxTypesToInclude = 5;

			var typeSample = anonymousResources
				.Take(maxTypesToInclude)
				.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)
				.ToList();

			var issue = Issues.AnonymousResourcesFound(anonymousResources.Count, maxTypesToInclude);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: typeSample,
				Recommendation: issue.Recommendation));
		}

		// Flag potentially suspicious anonymous resources
		if (suspiciousAnonymous.Count > 0) {
			var issue = Issues.SuspiciousAnonymousResources(suspiciousAnonymous.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [.. suspiciousAnonymous.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)],
				Recommendation: issue.Recommendation));
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);

	}

	private static bool IsPotentiallySecuritySensitive(string typeName) {
		var lowerName = typeName.ToLowerInvariant();
		return SuspiciousWords.Any(word => lowerName.Contains(word));
	}

}
