namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Visualization;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes authorizable resources for authorization quality issues.
/// </summary>
public class AuthorizableResourceAnalyzer(
	IServiceProvider services
) : IAuthorizationAnalyzer {

	public const string AnalyzerCategory = "Authorizable Resources";

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		// Get resources
		var authorizableResources = AuthorizationRuleProvider.Instance.GetAuthorizableResources();
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
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Error,
				Description: $"Found {requestResources.Count} resources that implement IAuthorizableRequest but have no authorizer defined",
				RelatedTypeNames: [.. requestResources.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)]));
		}

		// For non-request IAuthorizableResource without authorizer, check policy coverage
		if (nonRequestResources.Count > 0) {

			var withPolicyProtection = nonRequestResources
				.Where(r => HasAttributeBasedPolicyProtection(r.ResourceType, policyValidators))
				.ToList();

			var withoutPolicyProtection = nonRequestResources
				.Where(r => !HasAttributeBasedPolicyProtection(r.ResourceType, policyValidators))
				.ToList();

			// WARNING: IAuthorizableResource without authorizer AND without policy = potential gap
			if (withoutPolicyProtection.Count > 0) {
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Warning,
					Description: $"Found {withoutPolicyProtection.Count} IAuthorizableResource types without an authorizer or attribute-based policy protection",
					RelatedTypeNames: [.. withoutPolicyProtection.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)]));
			}

			// INFO: IAuthorizableResource relying only on policies (valid but worth noting)
			if (withPolicyProtection.Count > 0) {
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Info,
					Description: $"Found {withPolicyProtection.Count} IAuthorizableResource types protected only by attribute-based policies (no dedicated authorizer)",
					RelatedTypeNames: [.. withPolicyProtection.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)]));
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
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {resourcesWithOnlyRoleChecks.Count} resources with only role-based authorization (consider attribute-based policies for additional security)",
				RelatedTypeNames: [.. resourcesWithOnlyRoleChecks.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)]));
		}
	}

	private static void AnalyzeAuthorizationBalance(
		List<AnalysisIssue> issues,
		List<ResourceTypeInfo> protectedResources,
		List<IAuthorizationPolicyValidator> policyValidators) {

		var policyCount = policyValidators.Count;

		// Check for over-reliance on global policies
		if (policyCount > protectedResources.Count * 0.5 && protectedResources.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"High ratio of authorization policies ({policyCount}) to resource-specific authorizors ({protectedResources.Count}) - consider if this is intentional",
				RelatedTypeNames: []));
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
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {resourcesWithEmptyAuthorizers.Count} resources with authorizers that have no extracted rules (authorizers may be empty or use unsupported patterns)",
				RelatedTypeNames: [.. resourcesWithEmptyAuthorizers.Select(r => r.ResourceType.FullName ?? r.ResourceType.Name)]));
		}
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

		// For non-attribute policies, assume they might apply (conservative approach)
		return true;
	}

}