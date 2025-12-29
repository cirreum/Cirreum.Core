namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Modeling;
using Cirreum.Authorization.Modeling.Types;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes authorizable resources for authorization quality issues.
/// </summary>
public class AuthorizableResourceAnalyzer(
	IServiceProvider services
) : IAuthorizationAnalyzer {

	public const string AnalyzerCategory = "Authorizable Resources";

	#region Issue Definitions

	private static class Issues {

		public static IssueDefinition RequestsWithNoAuthorizer(int count) {
			var recommendation = count == 1
				? "Critical: This request will fail authorization. Create an authorizer for it or mark it as anonymous if public access is intended."
				: "Critical: These requests will fail authorization. Create an authorizer for each resource or mark them as anonymous if public access is intended.";

			return new(
				$"Found {count} resource(s) that implement IAuthorizableRequest but have no authorizer defined",
				recommendation);
		}

		public static IssueDefinition ResourcesWithNoProtection(int count, bool hasGlobalPolicies) {
			if (hasGlobalPolicies) {
				var recommendation = count == 1
					? "Verify this resource is covered by a registered global policy, or add an authorizer if dedicated protection is needed."
					: "Verify these resources are covered by registered global policies, or add authorizers if dedicated protection is needed.";

				return new(
					$"Found {count} IAuthorizableResource type(s) without an authorizer or attribute-based policy (may be covered by global policies)",
					recommendation);
			}

			var defaultRecommendation = count == 1
				? "Add an authorizer or apply a policy attribute to protect this resource, or convert to anonymous if public access is intended."
				: "Add an authorizer or apply a policy attribute to protect these resources, or convert to anonymous if public access is intended.";

			return new(
				$"Found {count} IAuthorizableResource type(s) without an authorizer or attribute-based policy protection",
				defaultRecommendation);
		}


		public static IssueDefinition ResourcesWithOnlyPolicyProtection(int count) => new(
			$"Found {count} IAuthorizableResource type(s) protected only by attribute-based policies (no dedicated authorizer)",
			"This is valid but consider dedicated authorizers for complex authorization logic or fine-grained control.");

		public static IssueDefinition ResourcesWithOnlyRoleChecks(int count) => new(
			$"Found {count} resource(s) with only role-based authorization (consider attribute-based policies for additional security)",
			"Role-based checks are valid. Consider attribute-based policies for cross-cutting concerns like tenant isolation or audit requirements.");

		public static IssueDefinition HighPolicyToAuthorizerRatio(int policyCount, int authorizerCount) => new(
			$"High ratio of authorization policies ({policyCount}) to resource-specific authorizers ({authorizerCount}) - consider if this is intentional",
			"Many policies relative to authorizers may indicate over-reliance on global policies. Verify this architecture is intentional.");

		public static IssueDefinition AuthorizersWithNoRules(int count) {
			var recommendation = count == 1
				? "This authorizer may be empty or use patterns the analyzer doesn't recognize. Verify it contains authorization logic."
				: "These authorizers may be empty or use patterns the analyzer doesn't recognize. Verify they contain authorization logic.";

			return new(
				$"Found {count} resource(s) with authorizers that have no extracted rules (authorizers may be empty or use unsupported patterns)",
				recommendation);
		}

	}

	#endregion

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		// Get resources
		var authorizableResources = AuthorizationModel.Instance.GetAuthorizableResources();
		var protectedResources = authorizableResources.Where(r => r.IsProtected).ToList();
		var unprotectedResources = authorizableResources.Where(r => !r.IsProtected).ToList();
		var policyValidators = services.GetServices<IAuthorizationPolicyValidator>().ToList();

		// Metrics
		metrics[$"{MetricCategories.AuthorizableResources}ResourceCount"] = authorizableResources.Count;
		metrics[$"{MetricCategories.AuthorizableResources}ProtectedCount"] = protectedResources.Count;
		metrics[$"{MetricCategories.AuthorizableResources}UnprotectedCount"] = unprotectedResources.Count;
		metrics[$"{MetricCategories.AuthorizableResources}PolicyCount"] = policyValidators.Count;
		metrics[$"{MetricCategories.AuthorizableResources}RuleCount"] = authorizableResources.Sum(r => r.Rules.Count);

		// Analyze authorization patterns
		AnalyzeUnprotectedResources(issues, unprotectedResources, policyValidators);
		AnalyzeRoleOnlyResources(issues, protectedResources, policyValidators);
		AnalyzeAuthorizationBalance(issues, protectedResources, policyValidators);
		AnalyzeEmptyValidators(issues, protectedResources);

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);

	}

	private static void AnalyzeUnprotectedResources(
		List<AnalysisIssue> issues,
		List<ResourceTypeInfo> unprotectedResources,
		List<IAuthorizationPolicyValidator> policyValidators) {

		if (unprotectedResources.Count == 0) {
			return;
		}

		// Split into resources that MUST have an authorizer vs those that don't
		var requestResources = unprotectedResources.Where(r => r.RequiresAuthorization).ToList();
		var nonRequestResources = unprotectedResources.Where(r => !r.RequiresAuthorization).ToList();

		// CRITICAL: IAuthorizableRequest without authorizer = security gap in the pipeline
		if (requestResources.Count > 0) {
			var issue = Issues.RequestsWithNoAuthorizer(requestResources.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Error,
				Description: issue.Description,
				RelatedTypeNames: [.. requestResources.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)],
				Recommendation: issue.Recommendation));
		}

		// For non-request IAuthorizableResource without authorizer, check policy coverage
		if (nonRequestResources.Count > 0) {

			var withPolicyProtection = nonRequestResources
				.Where(r => HasAttributeBasedPolicyProtection(r.ResourceType, policyValidators))
				.ToList();

			var withoutPolicyProtection = nonRequestResources
				.Where(r => !HasAttributeBasedPolicyProtection(r.ResourceType, policyValidators))
				.ToList();

			// WARNING/INFO: IAuthorizableResource without authorizer AND without policy = potential gap
			if (withoutPolicyProtection.Count > 0) {
				var globalPolicyCount = policyValidators.Count(IsGlobalPolicy);
				var issue = Issues.ResourcesWithNoProtection(withoutPolicyProtection.Count, globalPolicyCount > 0);

				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: globalPolicyCount > 0 ? IssueSeverity.Info : IssueSeverity.Warning,
					Description: issue.Description,
					RelatedTypeNames: [.. withoutPolicyProtection.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)],
					Recommendation: issue.Recommendation));
			}


			// INFO: IAuthorizableResource relying only on policies (valid but worth noting)
			if (withPolicyProtection.Count > 0) {
				var issue = Issues.ResourcesWithOnlyPolicyProtection(withPolicyProtection.Count);
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Info,
					Description: issue.Description,
					RelatedTypeNames: [.. withPolicyProtection.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)],
					Recommendation: issue.Recommendation));
			}

		}

	}

	private static void AnalyzeRoleOnlyResources(
		List<AnalysisIssue> issues,
		List<ResourceTypeInfo> protectedResources,
		List<IAuthorizationPolicyValidator> policyValidators) {

		// Resources with only role-based checks and no policy coverage
		var resourcesWithOnlyRoleChecks = protectedResources
			.Where(r => r.Rules.Count > 0 &&
						r.Rules.All(rule => rule.ValidationLogic.Contains("HasRole") || rule.ValidationLogic.Contains("HasAnyRole")))
			.Where(r => !HasAttributeBasedPolicyProtection(r.ResourceType, policyValidators))
			.ToList();

		if (resourcesWithOnlyRoleChecks.Count != 0) {
			var issue = Issues.ResourcesWithOnlyRoleChecks(resourcesWithOnlyRoleChecks.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [.. resourcesWithOnlyRoleChecks.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)],
				Recommendation: issue.Recommendation));
		}
	}

	private static void AnalyzeAuthorizationBalance(
		List<AnalysisIssue> issues,
		List<ResourceTypeInfo> protectedResources,
		List<IAuthorizationPolicyValidator> policyValidators) {

		var policyCount = policyValidators.Count;

		// Check for over-reliance on global policies
		if (policyCount > protectedResources.Count * 0.5 && protectedResources.Count > 0) {
			var issue = Issues.HighPolicyToAuthorizerRatio(policyCount, protectedResources.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: issue.Description,
				RelatedTypeNames: [],
				Recommendation: issue.Recommendation));
		}
	}

	private static void AnalyzeEmptyValidators(
		List<AnalysisIssue> issues,
		IReadOnlyList<ResourceTypeInfo> protectedResources) {

		// Resources with authorizers but no extracted rules (possibly empty authorizers)
		var resourcesWithEmptyAuthorizers = protectedResources
			.Where(r => r.AuthorizerType != null && r.Rules.Count == 0)
			.ToList();

		if (resourcesWithEmptyAuthorizers.Count > 0) {
			var issue = Issues.AuthorizersWithNoRules(resourcesWithEmptyAuthorizers.Count);
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: issue.Description,
				RelatedTypeNames: [.. resourcesWithEmptyAuthorizers.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)],
				Recommendation: issue.Recommendation));
		}
	}

	private static bool IsGlobalPolicy(IAuthorizationPolicyValidator policy) {
		var baseType = policy.GetType().BaseType;
		if (baseType?.IsGenericType == true &&
			baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>)) {
			return false;
		}
		return true;
	}

	private static bool HasAttributeBasedPolicyProtection(Type resourceType, List<IAuthorizationPolicyValidator> policyValidators) {
		return policyValidators.Any(pv => CouldPolicyApplyToResource(pv, resourceType));
	}

	private static bool CouldPolicyApplyToResource(IAuthorizationPolicyValidator policy, Type resourceType) {
		var baseType = policy.GetType().BaseType;
		if (baseType?.IsGenericType == true &&
			baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>)) {

			var genericArguments = baseType.GetGenericArguments();
			if (genericArguments.Length > 0) {
				var attributeType = genericArguments[0];
				return resourceType.GetCustomAttributes(attributeType, false).Length != 0;
			}
		}

		// Global policies are handled separately via IsGlobalPolicy
		return false;

	}

}