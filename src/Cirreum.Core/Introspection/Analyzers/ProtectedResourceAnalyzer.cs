namespace Cirreum.Introspection.Analyzers;

using Cirreum.Authorization.Resources;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes object-level ACL configuration. Detects whether
/// <see cref="IResourceAccessEvaluator"/> is registered, scans for
/// <see cref="IProtectedResource"/> implementations, and validates
/// that matching <see cref="IAccessEntryProvider{T}"/> registrations exist.
/// </summary>
public class ProtectedResourceAnalyzer(
	IServiceProvider services
) : IDomainAnalyzer {

	public const string AnalyzerCategory = "Object-Level ACL";

	private static class Issues {

		public static IssueDefinition NoEvaluatorRegistered() => new(
			"No IResourceAccessEvaluator is registered. Object-level ACL checks " +
			"(Check/Filter on IProtectedResource) are not available.",
			"If the application uses IProtectedResource types for object-level access control, " +
			"register via services.AddResourceAccess(...). If object-level ACLs are not needed, " +
			"this can be safely ignored.");

		public static IssueDefinition MissingProviders(int missingCount, int totalCount) => new(
			$"Found {missingCount} of {totalCount} IProtectedResource type(s) without a registered " +
			"IAccessEntryProvider<T>. Object-level ACL checks for these types will fail at runtime.",
			"Register an IAccessEntryProvider<T> for each IProtectedResource type to supply access " +
			"entries and hierarchy navigation.");

		public static IssueDefinition EvaluatorWithoutProtectedTypes() => new(
			"IResourceAccessEvaluator is registered but no IProtectedResource types were found " +
			"in the domain. The evaluator is available but has no types to protect.",
			"If object-level ACLs are intended, implement IProtectedResource on domain model types " +
			"that carry embedded access lists.");

		public static IssueDefinition Summary(int typeCount, int providerCount) => new(
			$"Object-level ACL active: {typeCount} IProtectedResource type(s) with " +
			$"{providerCount} IAccessEntryProvider registration(s).",
			null);
	}

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		var evaluator = services.GetService<IResourceAccessEvaluator>();
		var evaluatorRegistered = evaluator is not null;

		// Scan assemblies for IProtectedResource implementations
		var protectedTypes = DiscoverProtectedResourceTypes();

		// Check for matching IAccessEntryProvider<T> registrations
		var providerCount = 0;
		var missingProviders = new List<Type>();

		foreach (var protectedType in protectedTypes) {
			var providerType = typeof(IAccessEntryProvider<>).MakeGenericType(protectedType);
			var provider = services.GetService(providerType);
			if (provider is not null) {
				providerCount++;
			} else {
				missingProviders.Add(protectedType);
			}
		}

		// Metrics
		metrics[$"{MetricCategories.ObjectLevelAcl}EvaluatorRegistered"] = evaluatorRegistered ? 1 : 0;
		metrics[$"{MetricCategories.ObjectLevelAcl}ProtectedResourceTypeCount"] = protectedTypes.Count;
		metrics[$"{MetricCategories.ObjectLevelAcl}ProviderCount"] = providerCount;

		// ──────────────────────────────────────────────
		// 1. No evaluator registered
		// ──────────────────────────────────────────────

		if (!evaluatorRegistered && protectedTypes.Count == 0) {
			var issue = Issues.NoEvaluatorRegistered();
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [],
				Recommendation: issue.Recommendation));
		}

		// ──────────────────────────────────────────────
		// 2. Evaluator registered but no protected types
		// ──────────────────────────────────────────────

		if (evaluatorRegistered && protectedTypes.Count == 0) {
			var issue = Issues.EvaluatorWithoutProtectedTypes();
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [],
				Recommendation: issue.Recommendation));
		}

		// ──────────────────────────────────────────────
		// 3. Protected types without providers
		// ──────────────────────────────────────────────

		if (missingProviders.Count > 0) {
			var issue = Issues.MissingProviders(missingProviders.Count, protectedTypes.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: evaluatorRegistered ? IssueSeverity.Warning : IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [.. missingProviders.Select(t => t.FullName ?? t.Name)],
				Recommendation: issue.Recommendation));
		}

		// ──────────────────────────────────────────────
		// Summary
		// ──────────────────────────────────────────────

		if (evaluatorRegistered && protectedTypes.Count > 0) {
			var issue = Issues.Summary(protectedTypes.Count, providerCount);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [.. protectedTypes.Select(t => t.FullName ?? t.Name)],
				Recommendation: issue.Recommendation));
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);
	}

	/// <summary>
	/// Scans all assemblies for concrete types implementing <see cref="IProtectedResource"/>.
	/// </summary>
	private static List<Type> DiscoverProtectedResourceTypes() {
		var types = new List<Type>();

		foreach (var assembly in AssemblyScanner.ScanAssemblies()) {
			Type[] assemblyTypes;
			try {
				assemblyTypes = assembly.GetTypes();
			} catch {
				continue;
			}

			foreach (var type in assemblyTypes) {
				if (!type.IsAbstract && typeof(IProtectedResource).IsAssignableFrom(type)) {
					types.Add(type);
				}
			}
		}

		return types;
	}
}
