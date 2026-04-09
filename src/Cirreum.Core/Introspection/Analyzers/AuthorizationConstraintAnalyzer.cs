namespace Cirreum.Introspection.Analyzers;

using Cirreum.Authorization.Operations;
using Cirreum.Introspection.Modeling;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes authorization constraint registrations. Detects whether
/// <see cref="IAuthorizationConstraint"/> implementations are registered
/// when authorizable operations exist in the domain.
/// </summary>
public class AuthorizationConstraintAnalyzer(
	IServiceProvider services
) : IDomainAnalyzer {

	public const string AnalyzerCategory = "Authorization Constraints";

	private static class Issues {

		public static IssueDefinition NoConstraintsRegistered(int authorizableCount) => new(
			$"No IAuthorizationConstraint implementations registered, but {authorizableCount} " +
			"authorizable operation(s) exist. Authorization constraints provide cross-cutting " +
			"pre-checks (Stage 1, Step 1) that run before per-operation authorizers.",
			"If global pre-authorization checks are needed (e.g., tenant isolation, feature flags, " +
			"maintenance mode), implement IAuthorizationConstraint and register it in DI. " +
			"If constraints are not needed, this can be safely ignored.");

		public static IssueDefinition ConstraintSummary(int count) => new(
			$"{count} authorization constraint(s) registered. These run in registration order " +
			"as Stage 1, Step 1 of the authorization pipeline.",
			null);
	}

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		var constraints = services.GetServices<IAuthorizationConstraint>().ToList();
		var authorizableResources = DomainModel.Instance.GetAuthorizableResources();
		var authorizableCount = authorizableResources.Count(r => r.RequiresAuthorization);

		metrics[$"{MetricCategories.AuthorizationConstraints}ConstraintCount"] = constraints.Count;
		metrics[$"{MetricCategories.AuthorizationConstraints}AuthorizableOperationCount"] = authorizableCount;

		if (constraints.Count == 0 && authorizableCount > 0) {
			var issue = Issues.NoConstraintsRegistered(authorizableCount);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [],
				Recommendation: issue.Recommendation));
		} else if (constraints.Count > 0) {
			var issue = Issues.ConstraintSummary(constraints.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [.. constraints.Select(c => c.GetType().FullName ?? c.GetType().Name)],
				Recommendation: issue.Recommendation));
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);
	}
}
