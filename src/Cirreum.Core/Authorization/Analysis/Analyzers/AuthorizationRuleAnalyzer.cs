namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Visualization;

/// <summary>
/// Analyzes authorization validators and rules for security implications and consistency.
/// </summary>
public class AuthorizationRuleAnalyzer : IAuthorizationAnalyzer {

	private const string AnalyzerCategory = "Authorization Rules";

	public AnalysisReport Analyze() {
		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, object>();
		var rules = AuthorizationRuleProvider.Instance.GetAllRules();

		// Analyze rules for potential issues
		var rulesByResource = rules.GroupBy(r => r.ResourceType).ToList();
		metrics["ResourceCount"] = rulesByResource.Count;
		metrics["ValidatorCount"] = rules.Select(r => r.ValidatorType).Distinct().Count();
		metrics["RuleCount"] = rules.Count;

		// Check for resources with no validators
		var resourcesWithNoRules = rulesByResource.Where(g => !g.Any()).ToList();
		if (resourcesWithNoRules.Count != 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {resourcesWithNoRules.Count} resources with no authorization rules",
				RelatedObjects: [.. resourcesWithNoRules.Select(g => g.Key).Cast<object>()]));
		}

		// Check for resources with only HasRole checks
		var resourcesWithOnlyRoleChecks = rulesByResource
			.Where(g => g.All(r => r.ValidationLogic.Contains("HasRole") || r.ValidationLogic.Contains("HasAnyRole")))
			.ToList();

		if (resourcesWithOnlyRoleChecks.Count != 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {resourcesWithOnlyRoleChecks.Count} resources with only role-based checks",
				RelatedObjects: [.. resourcesWithOnlyRoleChecks.Select(g => g.Key).Cast<object>()]));
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);
	}

}