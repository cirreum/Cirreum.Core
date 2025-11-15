namespace Cirreum.Authorization.Analysis.Analyzers;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes policy validators for coverage, conflicts, and configuration issues.
/// </summary>
public class PolicyValidatorAnalyzer(
	IServiceProvider services
) : IAuthorizationAnalyzer {

	private const string AnalyzerCategory = "Policy Validators";

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, object>();

		var policyValidators = services.GetServices<IAuthorizationPolicyValidator>().ToList();

		// Basic metrics
		metrics["PolicyCount"] = policyValidators.Count;
		metrics["AttributePolicyCount"] = policyValidators.Count(IsAttributeBasedPolicy);
		metrics["GlobalPolicyCount"] = policyValidators.Count(pv => !IsAttributeBasedPolicy(pv));

		// Analyze policy ordering conflicts
		var orderingIssues = AnalyzePolicyOrdering(policyValidators);
		issues.AddRange(orderingIssues);

		// Analyze runtime type coverage
		var runtimeCoverageIssues = AnalyzeRuntimeTypeCoverage(policyValidators);
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
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Multiple policy validators have the same order ({group.Key}): {string.Join(", ", group.Select(g => g.PolicyName))}",
				RelatedObjects: [.. group]));
		}

		// Check for large gaps in ordering
		var orders = policyValidators.Select(pv => pv.Order).OrderBy(o => o).ToList();
		for (var i = 1; i < orders.Count; i++) {
			if (orders[i] - orders[i - 1] > 100) {
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Info,
					Description: $"Large gap in policy ordering between {orders[i - 1]} and {orders[i]}",
					RelatedObjects: [orders[i - 1], orders[i]]));
			}
		}

		return issues;
	}

	private static List<AnalysisIssue> AnalyzeRuntimeTypeCoverage(List<IAuthorizationPolicyValidator> policyValidators) {
		var issues = new List<AnalysisIssue>();

		var currentRuntimeType = ApplicationRuntime.Current.RuntimeType;

		// Check for policies that don't support any runtime types
		var unsupportedPolicies = policyValidators.Where(pv => pv.SupportedRuntimeTypes.Length == 0);
		foreach (var policy in unsupportedPolicies) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Policy '{policy.PolicyName}' doesn't support any runtime types",
				RelatedObjects: [policy]));
		}

		// ✅ Check if the CURRENT runtime type has policy coverage using bitwise flags
		var policiesForCurrentRuntime = policyValidators
			.Where(pv => pv.SupportedRuntimeTypes.Contains(currentRuntimeType))
			.ToList();

		if (policiesForCurrentRuntime.Count == 0 && policyValidators.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Current runtime type '{currentRuntimeType}' has no policy validator coverage",
				RelatedObjects: [currentRuntimeType]));
		}

		// ✅ Warn about policies that don't support the current runtime
		var irrelevantPolicies = policyValidators
			.Where(pv => pv.SupportedRuntimeTypes.Length > 0 &&
						 !pv.SupportedRuntimeTypes.Contains(currentRuntimeType))
			.ToList();

		foreach (var policy in irrelevantPolicies) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Policy '{policy.PolicyName}' doesn't support current runtime type '{currentRuntimeType}' (supports: {string.Join(", ", policy.SupportedRuntimeTypes)})",
				RelatedObjects: [policy]));
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
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Multiple policies target the same attribute type '{kvp.Key.Name}': {string.Join(", ", kvp.Value.Select(p => p.PolicyName))}",
				RelatedObjects: [.. kvp.Value]));
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
					issues.Add(new AnalysisIssue(
						Category: AnalyzerCategory,
						Severity: IssueSeverity.Warning,
						Description: $"Attribute policy '{policy.PolicyName}' targets attribute '{attributeType.Name}' but no resources use this attribute",
						RelatedObjects: [policy, attributeType]));
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