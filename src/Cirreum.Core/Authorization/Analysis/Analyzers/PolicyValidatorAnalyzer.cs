namespace Cirreum.Authorization.Analysis.Analyzers;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes policy validators for coverage, conflicts, and configuration issues.
/// </summary>
public class PolicyValidatorAnalyzer(
	IServiceProvider services
) : IAuthorizationAnalyzer {

	public const string AnalyzerCategory = "Policy Validators";

	#region Issue Definitions

	private static class Issues {

		public static IssueDefinition DuplicateOrderValues(int order, IEnumerable<string> policyNames) {
			var names = policyNames.ToList();
			var recommendation = names.Count == 2
				? $"Assign different order values to '{names[0]}' and '{names[1]}' to ensure predictable execution."
				: "Assign unique order values to each policy validator to ensure predictable execution sequence.";

			return new(
				$"Multiple policy validators have the same order ({order}): {string.Join(", ", names)}",
				recommendation);
		}

		public static IssueDefinition LargeOrderingGap(int before, int after) => new(
			$"Large gap in policy ordering between {before} and {after}",
			"Consider consolidating order values for easier maintenance. Large gaps aren't harmful but may indicate removed policies.");

		public static IssueDefinition NoRuntimeTypeSupport(string policyName) => new(
			$"Policy '{policyName}' doesn't support any runtime types",
			"Specify supported runtime types for this policy validator, or remove it if no longer needed.");

		public static IssueDefinition NoCurrentRuntimeCoverage(DomainRuntimeType runtimeType) => new(
			$"Current runtime type '{runtimeType}' has no policy validator coverage",
			"Register policy validators that support the current runtime type to ensure policies are enforced.");

		public static IssueDefinition PolicyNotForCurrentRuntime(string policyName, DomainRuntimeType currentRuntime, DomainRuntimeType[] supportedRuntimes) => new(
			$"Policy '{policyName}' doesn't support current runtime type '{currentRuntime}' (supports: {string.Join(", ", supportedRuntimes)})",
			"This policy won't execute in the current runtime. Verify this is intentional or add support for the current runtime type.");

		public static IssueDefinition MultiplePolicesForSameAttribute(string attributeName, IEnumerable<string> policyNames) => new(
			$"Multiple policies target the same attribute type '{attributeName}': {string.Join(", ", policyNames)}",
			"Consider consolidating these policies or ensure they have different order values for predictable behavior.");

		public static IssueDefinition UnusedAttributePolicy(string policyName, string attributeName) => new(
			$"Attribute policy '{policyName}' targets attribute '{attributeName}' but no resources use this attribute",
			"Either apply this attribute to resources that need this policy, or remove the unused policy validator.");

	}

	#endregion

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		var policyValidators = services.GetServices<IAuthorizationPolicyValidator>().ToList();
		var domainEnvironment = services.GetRequiredService<IDomainEnvironment>();

		// Basic metrics
		metrics[$"{MetricCategories.PolicyValidation}PolicyCount"] = policyValidators.Count;
		metrics[$"{MetricCategories.PolicyValidation}AttributePolicyCount"] = policyValidators.Count(IsAttributeBasedPolicy);
		metrics[$"{MetricCategories.PolicyValidation}GlobalPolicyCount"] = policyValidators.Count(pv => !IsAttributeBasedPolicy(pv));

		// Analyze policy ordering conflicts
		var orderingIssues = AnalyzePolicyOrdering(policyValidators);
		issues.AddRange(orderingIssues);

		// Analyze runtime type coverage
		var runtimeCoverageIssues = AnalyzeRuntimeTypeCoverage(policyValidators, domainEnvironment.RuntimeType);
		issues.AddRange(runtimeCoverageIssues);

		// Analyze policy overlap and conflicts
		var overlapIssues = AnalyzePolicyOverlap(policyValidators);
		issues.AddRange(overlapIssues);

		// Analyze attribute usage patterns
		var attributeIssues = AnalyzeAttributeUsage(policyValidators);
		issues.AddRange(attributeIssues);

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);

	}

	/// <summary>
	/// Helper method to safely check if a policy is attribute-based
	/// </summary>
	private static bool IsAttributeBasedPolicy(IAuthorizationPolicyValidator policy) {
		var baseType = policy.GetType().BaseType;
		return baseType?.IsGenericType == true &&
			   baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>);
	}

	/// <summary>
	/// Helper method to safely get the attribute type from an attribute-based policy
	/// </summary>
	private static Type? GetAttributeType(IAuthorizationPolicyValidator policy) {
		var baseType = policy.GetType().BaseType;
		if (baseType?.IsGenericType == true &&
			baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>)) {

			var genericArguments = baseType.GetGenericArguments();
			return genericArguments.Length > 0 ? genericArguments[0] : null;
		}
		return null;
	}

	private static List<AnalysisIssue> AnalyzePolicyOrdering(List<IAuthorizationPolicyValidator> policyValidators) {
		var issues = new List<AnalysisIssue>();

		// Check for duplicate order values
		var orderGroups = policyValidators.GroupBy(pv => pv.Order).Where(g => g.Count() > 1);
		foreach (var group in orderGroups) {
			var issue = Issues.DuplicateOrderValues(group.Key, group.Select(g => g.PolicyName));
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [.. group.Select(g => g.GetType().FullName ?? g.GetType().Name)],
				Recommendation: issue.Recommendation));
		}

		// Check for large gaps in ordering
		var orders = policyValidators.Select(pv => pv.Order).OrderBy(o => o).ToList();
		for (var i = 1; i < orders.Count; i++) {
			if (orders[i] - orders[i - 1] > 100) {
				var issue = Issues.LargeOrderingGap(orders[i - 1], orders[i]);
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Info,
					Description: issue.Description,
					RelatedTypeNames: [orders[i - 1].ToString(), orders[i].ToString()],
					Recommendation: issue.Recommendation));
			}
		}

		return issues;
	}

	private static List<AnalysisIssue> AnalyzeRuntimeTypeCoverage(
		List<IAuthorizationPolicyValidator> policyValidators,
		DomainRuntimeType currentRuntimeType) {
		var issues = new List<AnalysisIssue>();

		// Check for policies that don't support any runtime types
		var unsupportedPolicies = policyValidators.Where(pv => pv.SupportedRuntimeTypes.Length == 0);
		foreach (var policy in unsupportedPolicies) {
			var issue = Issues.NoRuntimeTypeSupport(policy.PolicyName);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [policy.GetType().FullName ?? policy.GetType().Name],
				Recommendation: issue.Recommendation));
		}

		// Check if the CURRENT runtime type has policy coverage
		var policiesForCurrentRuntime = policyValidators
			.Where(pv => pv.SupportedRuntimeTypes.Contains(currentRuntimeType))
			.ToList();

		if (policiesForCurrentRuntime.Count == 0 && policyValidators.Count > 0) {
			var issue = Issues.NoCurrentRuntimeCoverage(currentRuntimeType);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [currentRuntimeType.ToString()],
				Recommendation: issue.Recommendation));
		}

		// Warn about policies that don't support the current runtime
		var irrelevantPolicies = policyValidators
			.Where(pv => pv.SupportedRuntimeTypes.Length > 0 &&
						 !pv.SupportedRuntimeTypes.Contains(currentRuntimeType))
			.ToList();

		foreach (var policy in irrelevantPolicies) {
			var issue = Issues.PolicyNotForCurrentRuntime(policy.PolicyName, currentRuntimeType, policy.SupportedRuntimeTypes);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [policy.GetType().FullName ?? policy.GetType().Name],
				Recommendation: issue.Recommendation));
		}

		return issues;
	}

	private static List<AnalysisIssue> AnalyzePolicyOverlap(List<IAuthorizationPolicyValidator> policyValidators) {
		var issues = new List<AnalysisIssue>();

		// Analyze attribute-based policies that might conflict
		var attributePolicies = policyValidators.Where(IsAttributeBasedPolicy).ToList();

		// Check for multiple policies targeting the same attribute type
		var attributeTypes = new Dictionary<Type, List<IAuthorizationPolicyValidator>>();
		foreach (var policy in attributePolicies) {
			var attributeType = GetAttributeType(policy);
			if (attributeType != null) {
				if (!attributeTypes.TryGetValue(attributeType, out var value)) {
					value = [];
					attributeTypes[attributeType] = value;
				}

				value.Add(policy);
			}
		}

		foreach (var kvp in attributeTypes.Where(kvp => kvp.Value.Count > 1)) {
			var issue = Issues.MultiplePolicesForSameAttribute(kvp.Key.Name, kvp.Value.Select(p => p.PolicyName));
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [.. kvp.Value.Select(p => p.GetType().FullName ?? p.GetType().Name)],
				Recommendation: issue.Recommendation));
		}

		return issues;
	}

	private static List<AnalysisIssue> AnalyzeAttributeUsage(List<IAuthorizationPolicyValidator> policyValidators) {

		var issues = new List<AnalysisIssue>();

		// Find attribute-based policies
		var attributePolicies = policyValidators.Where(IsAttributeBasedPolicy).ToList();

		foreach (var policy in attributePolicies) {
			var attributeType = GetAttributeType(policy);
			if (attributeType != null) {
				// Check if any resources actually use this attribute
				var resourcesWithAttribute = FindResourcesWithAttribute(attributeType);

				if (resourcesWithAttribute.Count == 0) {
					var issue = Issues.UnusedAttributePolicy(policy.PolicyName, attributeType.Name);
					issues.Add(new AnalysisIssue(
						Category: AnalyzerCategory,
						Severity: IssueSeverity.Warning,
						Description: issue.Description,
						RelatedTypeNames: [policy.GetType().FullName ?? policy.GetType().Name, attributeType.FullName ?? attributeType.Name],
						Recommendation: issue.Recommendation));
				}
			}
		}

		return issues;
	}

	private static List<Type> FindResourcesWithAttribute(Type attributeType) {
		// Scan all types that implement IAuthorizableResource in loaded assemblies
		var allAuthorizableResources = AssemblyScanner.ScanAssemblies()
			.SelectMany(assembly => {
				try {
					return assembly.GetTypes();
				} catch {
					return Type.EmptyTypes; // Skip assemblies that can't be loaded
				}
			})
			.Where(type => type.IsClass && !type.IsAbstract &&
						  typeof(IAuthorizableResource).IsAssignableFrom(type))
			.Distinct();

		return [.. allAuthorizableResources.Where(resourceType => resourceType.GetCustomAttributes(attributeType, false).Length != 0)];
	}

}