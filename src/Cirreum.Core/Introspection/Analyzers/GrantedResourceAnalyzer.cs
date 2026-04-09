namespace Cirreum.Introspection.Analyzers;

using Cirreum.Authorization.Operations.Grants;
using Cirreum.Introspection.Modeling;
using Cirreum.Introspection.Modeling.Types;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Analyzes granted resources and grant domain hygiene. Detects misconfigurations
/// such as missing permissions, orphaned domains, inert permission attributes,
/// mixed authorization patterns, missing grant providers, self-scoped patterns,
/// and cross-domain permission inconsistencies.
/// </summary>
public class GrantedResourceAnalyzer(
	IServiceProvider? services = null
) : IDomainAnalyzer {

	public const string AnalyzerCategory = "Granted Resources";

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		var allResources = DomainModel.Instance.GetAllResources();

		var grantedResources = allResources.Where(r => r.IsGranted).ToList();
		var grantDomains = grantedResources
			.Where(r => r.GrantDomain is not null)
			.Select(r => r.GrantDomain!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		// ──────────────────────────────────────────────
		// 1. Granted resources without [RequiresPermission]
		// ──────────────────────────────────────────────

		var missingPermissions = grantedResources
			.Where(r => r.Permissions.Count == 0)
			.ToList();

		if (missingPermissions.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {missingPermissions.Count} granted resource(s) without [RequiresPermission]. " +
					"These resources participate in the grant pipeline but have no permission gate, " +
					"so grant evaluation cannot enforce access control.",
				RelatedTypeNames: [.. missingPermissions.Select(TypeName)],
				Recommendation: "Add [RequiresPermission(\"name\")] to each granted resource to define " +
					"the permission(s) required for access."));
		}

		// ──────────────────────────────────────────────
		// 2. [RequiresPermission] on non-granted resources
		// ──────────────────────────────────────────────

		var permissionsWithoutGrants = allResources
			.Where(r => !r.IsGranted && r.Permissions.Count > 0)
			.ToList();

		if (permissionsWithoutGrants.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {permissionsWithoutGrants.Count} resource(s) with [RequiresPermission] " +
					"that do not implement a Granted interface. Permissions are available on " +
					"AuthorizationContext.Permissions for use in resource authorizers.",
				RelatedTypeNames: [.. permissionsWithoutGrants.Select(TypeName)],
				Recommendation: "If grant-based access control is intended, add the appropriate Granted " +
					"interface (e.g., IOwnerMutateOperation). Otherwise, ensure the resource authorizer " +
					"consumes Permissions for authorization decisions."));
		}

		// ──────────────────────────────────────────────
		// 3. Granted resources without a resource authorizer
		// ──────────────────────────────────────────────

		var grantedWithoutAuthorizer = grantedResources
			.Where(r => !r.IsProtected)
			.ToList();

		if (grantedWithoutAuthorizer.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {grantedWithoutAuthorizer.Count} granted resource(s) without a " +
					"resource authorizer (Stage 2). Grant evaluation (Stage 1) runs, but no " +
					"resource-level authorization rules are applied.",
				RelatedTypeNames: [.. grantedWithoutAuthorizer.Select(TypeName)],
				Recommendation: "If these resources require resource-level authorization beyond grant " +
					"evaluation, add a AuthorizerBase<T> implementation. If grants-only " +
					"authorization is intentional, this can be safely ignored."));
		}

		// ──────────────────────────────────────────────
		// 4. Domain namespaces with granted interfaces but no granted resources
		// ──────────────────────────────────────────────

		var namespaceDomains = DiscoverNamespaceDomains();
		var usedDomains = new HashSet<string>(grantDomains, StringComparer.OrdinalIgnoreCase);

		var unusedDomains = namespaceDomains
			.Where(d => !usedDomains.Contains(d))
			.ToList();

		if (unusedDomains.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {unusedDomains.Count} domain namespace(s) with no granted resources: " +
					string.Join(", ", unusedDomains) + ".",
				RelatedTypeNames: unusedDomains,
				Recommendation: "If these domains should use grant-based access control, add the appropriate " +
					"Granted interface (IOwnerMutateOperation, IOwnerLookupOperation<T>, etc.) to their resources."));
		}

		// ──────────────────────────────────────────────
		// 5. Mixed authorization within a domain
		// ──────────────────────────────────────────────

		DetectMixedAuthorizationDomains(allResources, grantDomains, issues);

		// ──────────────────────────────────────────────
		// 6. No IOperationGrantProvider registered
		// ──────────────────────────────────────────────

		var grantProviderRegistered = false;

		if (services is not null && grantedResources.Count > 0) {
			var grantProvider = services.GetService<IOperationGrantProvider>();
			grantProviderRegistered = grantProvider is not null;

			if (!grantProviderRegistered) {
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Error,
					Description: $"Found {grantedResources.Count} granted resource(s) but no IOperationGrantProvider " +
						"is registered. Grant evaluation (Stage 1) cannot run without a grant resolver.",
					RelatedTypeNames: [],
					Recommendation: "Register an IOperationGrantProvider implementation via " +
						"services.AddOperationGrants<TResolver>() to enable grant-based access control."));
			}
		}

		// ──────────────────────────────────────────────
		// 7. Self-scoped operations summary
		// ──────────────────────────────────────────────

		var selfScoped = grantedResources.Where(r => r.IsSelfScoped).ToList();

		if (selfScoped.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"{selfScoped.Count} self-scoped operation(s) detected. These use identity " +
					"matching (ExternalId == UserId) instead of owner-scope grant resolution.",
				RelatedTypeNames: [.. selfScoped.Select(TypeName)],
				Recommendation: null));
		}

		// ──────────────────────────────────────────────
		// 8. Self-scoped operations without permissions
		// ──────────────────────────────────────────────

		var selfScopedNoPermissions = selfScoped
			.Where(r => r.Permissions.Count == 0)
			.ToList();

		if (selfScopedNoPermissions.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Found {selfScopedNoPermissions.Count} self-scoped operation(s) without " +
					"[RequiresPermission]. Self-scoped operations rely on identity matching; " +
					"permissions are optional but enable permission-gated self-access.",
				RelatedTypeNames: [.. selfScopedNoPermissions.Select(TypeName)],
				Recommendation: "Add [RequiresPermission] if you need the grant system to verify specific " +
					"permissions before allowing self-access. Otherwise, identity matching alone is sufficient."));
		}

		// ──────────────────────────────────────────────
		// 9. Cross-domain permissions
		// ──────────────────────────────────────────────

		var crossDomain = grantedResources
			.Where(r => r.Permissions.Count >= 2)
			.Where(r => r.Permissions.Select(p => p.Feature).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
			.ToList();

		if (crossDomain.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {crossDomain.Count} resource(s) with [RequiresPermission] attributes " +
					"spanning multiple domain features.",
				RelatedTypeNames: [.. crossDomain.Select(TypeName)],
				Recommendation: "All permissions on a granted resource should use the same domain feature. " +
					"Cross-cutting concerns belong in Stage 2 resource authorizers or Stage 3 policies."));
		}

		// ──────────────────────────────────────────────
		// Metrics
		// ──────────────────────────────────────────────

		var permissionCount = grantedResources
			.SelectMany(r => r.Permissions)
			.Select(p => p.ToString())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Count();

		metrics[$"{MetricCategories.GrantedResources}GrantedResourceCount"] = grantedResources.Count;
		metrics[$"{MetricCategories.GrantedResources}GrantDomainCount"] = grantDomains.Count;
		metrics[$"{MetricCategories.GrantedResources}TotalPermissionCount"] = permissionCount;
		metrics[$"{MetricCategories.GrantedResources}MissingPermissionCount"] = missingPermissions.Count;
		metrics[$"{MetricCategories.GrantedResources}PermissionsWithoutGrantsCount"] = permissionsWithoutGrants.Count;
		metrics[$"{MetricCategories.GrantedResources}UnusedDomainCount"] = unusedDomains.Count;
		metrics[$"{MetricCategories.GrantedResources}GrantProviderRegistered"] = grantProviderRegistered ? 1 : 0;
		metrics[$"{MetricCategories.GrantedResources}SelfScopedCount"] = selfScoped.Count;
		metrics[$"{MetricCategories.GrantedResources}CrossDomainPermissionCount"] = crossDomain.Count;

		// Summary
		if (grantedResources.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Info,
				Description: $"Grant system active: {grantedResources.Count} granted resource(s) across " +
					$"{grantDomains.Count} domain(s) using {permissionCount} distinct permission(s).",
				RelatedTypeNames: grantDomains));
		}

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);

	}

	/// <summary>
	/// Detects domain boundaries where some authorizable resources are granted and others
	/// are not — may indicate an incomplete migration to grants.
	/// </summary>
	private static void DetectMixedAuthorizationDomains(
		IReadOnlyList<ResourceTypeInfo> allResources,
		List<string> grantDomains,
		List<AnalysisIssue> issues) {

		if (grantDomains.Count == 0) {
			return;
		}

		// Group authorizable resources by DomainBoundary, then check if the boundary
		// has both granted and non-granted resources
		var authorizableByBoundary = allResources
			.Where(r => r.RequiresAuthorization)
			.GroupBy(r => r.DomainBoundary, StringComparer.OrdinalIgnoreCase);

		foreach (var group in authorizableByBoundary) {
			var granted = group.Where(r => r.IsGranted).ToList();
			var nonGranted = group.Where(r => !r.IsGranted).ToList();

			if (granted.Count > 0 && nonGranted.Count > 0) {
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Info,
					Description: $"Domain boundary '{group.Key}' has {granted.Count} granted and " +
						$"{nonGranted.Count} non-granted authorizable resource(s). " +
						"This may indicate an incomplete migration to grants.",
					RelatedTypeNames: [.. nonGranted.Select(TypeName)],
					Recommendation: "If all resources in this domain should use grant-based access control, " +
						"add the appropriate Granted interface to the remaining resources. If the mix is " +
						"intentional (e.g., some operations are role-only), this can be safely ignored."));
			}
		}

	}

	/// <summary>
	/// Derives domain feature names from all authorizable resource types' namespaces.
	/// </summary>
	private static HashSet<string> DiscoverNamespaceDomains() {
		var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var assembly in AssemblyScanner.ScanAssemblies()) {
			Type[] types;
			try {
				types = assembly.GetTypes();
			} catch {
				continue;
			}

			foreach (var type in types) {
				if (!type.IsClass || type.IsAbstract) {
					continue;
				}
				var domain = DomainFeatureResolver.Resolve(type);
				if (domain is not null) {
					domains.Add(domain);
				}
			}
		}

		return domains;
	}

	private static string TypeName(ResourceTypeInfo r) =>
		r.ResourceType.FullName ?? r.ResourceType.Name;

}
