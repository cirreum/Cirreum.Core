namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Visualization;

/// <summary>
/// Analyzes authorization rules for security implications and consistency.
/// </summary>
public class AuthorizationRuleAnalyzer : IAuthorizationAnalyzerWithOptions {

	public const string AnalyzerCategory = "Authorization Rules";

	public AnalysisReport Analyze() => this.Analyze(AnalysisOptions.Default);

	public AnalysisReport Analyze(AnalysisOptions options) {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();
		var rules = AuthorizationRuleProvider.Instance.GetAllRules();
		var rulesByResource = rules.GroupBy(r => r.ResourceType).ToList();
		var rulesWithMissingResource = rules.Where(r => r.ResourceType == typeof(MissingResource)).ToList();

		// Capture metrics for this analyzer
		metrics[$"{MetricCategories.AuthorizationRules}AuthorizerCount"] = rules.Select(r => r.AuthorizorType).Distinct().Count();
		metrics[$"{MetricCategories.AuthorizationRules}ResourceCount"] = rulesByResource.Count(g => g.Key != typeof(MissingResource));
		metrics[$"{MetricCategories.AuthorizationRules}OrphanedAuthorizerCount"] = rulesWithMissingResource.Select(r => r.AuthorizorType).Distinct().Count();
		metrics[$"{MetricCategories.AuthorizationRules}RuleCount"] = rules.Count;

		// Check for authorizers with a missing/orphaned resource (critical error)
		if (rulesWithMissingResource.Count > 0) {
			var orphanedAuthorizers = rulesWithMissingResource
				.Select(r => r.AuthorizorType)
				.Distinct()
				.ToList();
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Error,
				Description: $"Found {orphanedAuthorizers.Count} authorizers with no matching resource type (orphaned authorizers)",
				RelatedTypeNames: [.. orphanedAuthorizers.Select(t => t.FullName ?? t.Name)]));
		}

		// Check for resources with only role-based checks (informational)
		if (options.IncludeInfoIssues) {
			var resourcesWithOnlyRoleChecks = rulesByResource
				.Where(g => g.Key != typeof(MissingResource))
				.Where(g => g.All(r =>
					r.ValidationLogic.Contains("HasRole") ||
					r.ValidationLogic.Contains("HasAnyRole") ||
					r.ValidationLogic.Contains("HasAllRoles")))
				.ToList();

			if (resourcesWithOnlyRoleChecks.Count != 0) {
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Info,
					Description: $"Found {resourcesWithOnlyRoleChecks.Count} resources with only role-based authorization checks",
					RelatedTypeNames: [.. resourcesWithOnlyRoleChecks.Select(g => g.Key.FullName ?? g.Key.Name)]));
			}
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);
	}

}