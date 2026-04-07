namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Grants;
using Cirreum.Authorization.Modeling;
using Cirreum.Authorization.Modeling.Types;


/// <summary>
/// Analyzes granted resources and grant domain hygiene. Detects misconfigurations
/// such as missing permissions, orphaned domains, inert permission attributes,
/// and mixed authorization patterns within a single domain.
/// </summary>
public class GrantedResourceAnalyzer : IAuthorizationAnalyzer {

	public const string AnalyzerCategory = "Granted Resources";

	public AnalysisReport Analyze() {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, int>();

		var allResources = AuthorizationModel.Instance.GetAllResources();

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
					"interface (e.g., IGrantedCommand). Otherwise, ensure the resource authorizer " +
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
					"evaluation, add a ResourceAuthorizerBase<T> implementation. If grants-only " +
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
					"Granted interface (IGrantedCommand, IGrantedRead<T>, etc.) to their resources."));
		}

		// ──────────────────────────────────────────────
		// 5. Mixed authorization within a domain
		// ──────────────────────────────────────────────

		DetectMixedAuthorizationDomains(allResources, grantDomains, issues);

		// ──────────────────────────────────────────────
		// Metrics
		// ──────────────────────────────────────────────

		var permissionCount = grantedResources
			.SelectMany(r => r.Permissions)
			.Select(p => p.ToString())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Count();

		metrics[$"{AnalyzerCategory}.GrantedResourceCount"] = grantedResources.Count;
		metrics[$"{AnalyzerCategory}.GrantDomainCount"] = grantDomains.Count;
		metrics[$"{AnalyzerCategory}.TotalPermissionCount"] = permissionCount;
		metrics[$"{AnalyzerCategory}.MissingPermissionCount"] = missingPermissions.Count;
		metrics[$"{AnalyzerCategory}.PermissionsWithoutGrantsCount"] = permissionsWithoutGrants.Count;
		metrics[$"{AnalyzerCategory}.UnusedDomainCount"] = unusedDomains.Count;

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
