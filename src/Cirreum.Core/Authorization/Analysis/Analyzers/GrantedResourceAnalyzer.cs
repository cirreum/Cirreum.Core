namespace Cirreum.Authorization.Analysis.Analyzers;

using Cirreum.Authorization.Grants;
using Cirreum.Authorization.Modeling;
using Cirreum.Authorization.Modeling.Types;
using System.Reflection;

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
			.Where(r => r.RequiredPermissions is null or { Count: 0 })
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

		var inertPermissions = allResources
			.Where(r => !r.IsGranted && r.RequiresAuthorization)
			.Where(r => HasRequiresPermissionAttribute(r.ResourceType))
			.ToList();

		if (inertPermissions.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {inertPermissions.Count} authorizable resource(s) with [RequiresPermission] " +
					"that do not implement a Granted interface (IGrantedCommand, IGrantedRead, etc.). " +
					"The permission attribute is inert without the grant pipeline.",
				RelatedTypeNames: [.. inertPermissions.Select(TypeName)],
				Recommendation: "Either add the appropriate Granted interface (e.g., IGrantedCommand<TDomain>) " +
					"to enable grant evaluation, or remove the [RequiresPermission] attribute if permissions " +
					"are not intended for this resource."));
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
		// 4. Orphaned grant domains
		// ──────────────────────────────────────────────

		var declaredDomains = DiscoverDeclaredGrantDomains();
		var usedDomains = new HashSet<string>(grantDomains, StringComparer.OrdinalIgnoreCase);

		var orphanedDomains = declaredDomains
			.Where(d => !usedDomains.Contains(d.Namespace))
			.ToList();

		if (orphanedDomains.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {orphanedDomains.Count} [GrantDomain] marker interface(s) with no " +
					"granted resources. These domain declarations are unused.",
				RelatedTypeNames: [.. orphanedDomains.Select(d => d.MarkerType.FullName ?? d.MarkerType.Name)],
				Recommendation: "Either add granted resources (IGrantedCommand<TDomain>, IGrantedRead<TDomain, T>, etc.) " +
					"that use these domain markers, or remove the unused [GrantDomain] declarations."));
		}

		// ──────────────────────────────────────────────
		// 5. Mixed authorization within a domain
		// ──────────────────────────────────────────────

		DetectMixedAuthorizationDomains(allResources, grantDomains, issues);

		// ──────────────────────────────────────────────
		// Metrics
		// ──────────────────────────────────────────────

		var permissionCount = grantedResources
			.Where(r => r.RequiredPermissions is not null)
			.SelectMany(r => r.RequiredPermissions!)
			.Select(p => p.ToString())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Count();

		metrics[$"{AnalyzerCategory}.GrantedResourceCount"] = grantedResources.Count;
		metrics[$"{AnalyzerCategory}.GrantDomainCount"] = grantDomains.Count;
		metrics[$"{AnalyzerCategory}.TotalPermissionCount"] = permissionCount;
		metrics[$"{AnalyzerCategory}.MissingPermissionCount"] = missingPermissions.Count;
		metrics[$"{AnalyzerCategory}.InertPermissionCount"] = inertPermissions.Count;
		metrics[$"{AnalyzerCategory}.OrphanedDomainCount"] = orphanedDomains.Count;

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
	/// Scans assemblies for interfaces decorated with <see cref="GrantDomainAttribute"/>.
	/// </summary>
	private static List<(Type MarkerType, string Namespace)> DiscoverDeclaredGrantDomains() {
		var result = new List<(Type, string)>();

		foreach (var assembly in AssemblyScanner.ScanAssemblies()) {
			Type[] types;
			try {
				types = assembly.GetTypes();
			} catch {
				continue;
			}

			foreach (var type in types) {
				if (!type.IsInterface) {
					continue;
				}
				var attr = type.GetCustomAttribute<GrantDomainAttribute>();
				if (attr is not null) {
					result.Add((type, attr.Namespace));
				}
			}
		}

		return result;
	}

	private static bool HasRequiresPermissionAttribute(Type resourceType) =>
		resourceType.GetCustomAttributes<RequiresPermissionAttribute>(inherit: true).Any();

	private static string TypeName(ResourceTypeInfo r) =>
		r.ResourceType.FullName ?? r.ResourceType.Name;

}
