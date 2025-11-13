namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Visualization;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Enhanced analyzer that includes both resource validators and policy validators.
/// </summary>
public class EnhancedAuthorizationRuleAnalyzer(
	IServiceProvider services
) : IAuthorizationAnalyzer {

	private const string AnalyzerCategory = "Authorization Rules";

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, object>();
		var rules = AuthorizationRuleProvider.Instance.GetAllRules();
		var policyValidators = services.GetServices<IAuthorizationPolicyValidator>().ToList();

		// Enhanced metrics
		var rulesByResource = rules.GroupBy(r => r.ResourceType).ToList();
		metrics["ResourceCount"] = rulesByResource.Count;
		metrics["ResourceValidatorCount"] = rules.Select(r => r.ValidatorType).Distinct().Count();
		metrics["PolicyCount"] = policyValidators.Count;
		metrics["ResourceRuleCount"] = rules.Count;
		metrics["TotalAuthorizationRules"] = rules.Count + policyValidators.Count;

		// Check for resources with no validators AND no applicable policies
		var resourcesWithoutProtection = FindUnprotectedResources(rulesByResource, policyValidators);
		if (resourcesWithoutProtection.Count != 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Error,
				Description: $"Found {resourcesWithoutProtection.Count} resources with no authorization protection (no validators and no applicable policies)",
				RelatedObjects: [.. resourcesWithoutProtection.Cast<object>()]));
		}

		// Analyze authorization coverage patterns
		var coverageIssues = AnalyzeAuthorizationCoverage(rulesByResource, policyValidators);
		issues.AddRange(coverageIssues);

		// Check for over-reliance on policies vs resource-specific rules
		var relianceIssues = AnalyzeAuthorizationBalance(rulesByResource, policyValidators);
		issues.AddRange(relianceIssues);

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);
	}

	private static List<Type> FindUnprotectedResources(
		List<IGrouping<Type, AuthorizationRuleInfo>> rulesByResource,
		List<IAuthorizationPolicyValidator> policyValidators) {

		var unprotectedResources = new List<Type>();
		var resourcesWithRules = rulesByResource.Where(g => g.Any()).Select(g => g.Key).ToHashSet();

		// For resources without rules, check if any policies would apply
		var resourcesWithoutRules = rulesByResource.Where(g => !g.Any()).Select(g => g.Key);

		foreach (var resourceType in resourcesWithoutRules) {
			// Simulate creating a resource instance to test policy applicability
			// This is a simplified check - in practice you might want more sophisticated analysis
			var hasApplicablePolicy = policyValidators.Any(pv =>
				CouldPolicyApplyToResource(pv, resourceType));

			if (!hasApplicablePolicy) {
				unprotectedResources.Add(resourceType);
			}
		}

		return unprotectedResources;
	}

	private static bool CouldPolicyApplyToResource(IAuthorizationPolicyValidator policy, Type resourceType) {
		// Check if this is an attribute-based policy
		var baseType = policy.GetType().BaseType;
		if (baseType?.IsGenericType == true &&
			baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>)) {

			var genericArguments = baseType.GetGenericArguments();
			if (genericArguments.Length > 0) {
				var attributeType = genericArguments[0];
				return resourceType.GetCustomAttributes(attributeType, false).Length != 0;
			}
		}

		// For non-attribute policies, we can't easily determine applicability without an instance
		// So we assume they might apply (conservative approach)
		return true;
	}

	private static List<AnalysisIssue> AnalyzeAuthorizationCoverage(
		List<IGrouping<Type, AuthorizationRuleInfo>> rulesByResource,
		List<IAuthorizationPolicyValidator> policyValidators) {

		var issues = new List<AnalysisIssue>();

		// Resources with only role-based checks and no policy coverage
		var resourcesWithOnlyRoleChecks = rulesByResource
			.Where(g => g.All(r => r.ValidationLogic.Contains("HasRole") || r.ValidationLogic.Contains("HasAnyRole")))
			.Where(g => !HasAttributeBasedPolicyProtection(g.Key, policyValidators))
			.ToList();

		if (resourcesWithOnlyRoleChecks.Count != 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {resourcesWithOnlyRoleChecks.Count} resources with only role-based authorization (consider attribute-based policies for additional security)",
				RelatedObjects: [.. resourcesWithOnlyRoleChecks.Select(g => g.Key).Cast<object>()]));
		}

		return issues;
	}

	private static bool HasAttributeBasedPolicyProtection(Type resourceType, List<IAuthorizationPolicyValidator> policyValidators) {
		return policyValidators.Any(pv => CouldPolicyApplyToResource(pv, resourceType));
	}

	private static List<AnalysisIssue> AnalyzeAuthorizationBalance(
		List<IGrouping<Type, AuthorizationRuleInfo>> rulesByResource,
		List<IAuthorizationPolicyValidator> policyValidators) {

		var issues = new List<AnalysisIssue>();

		var resourcesWithRules = rulesByResource.Where(g => g.Any()).ToList();
		var policyCount = policyValidators.Count;

		// Check for over-reliance on global policies
		if (policyCount > resourcesWithRules.Count * 0.5) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"High ratio of policy validators ({policyCount}) to resource-specific validators ({resourcesWithRules.Count}) - consider if this is intentional",
				RelatedObjects: []));
		}

		return issues;
	}

}