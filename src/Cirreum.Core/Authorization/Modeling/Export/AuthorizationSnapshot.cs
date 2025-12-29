namespace Cirreum.Authorization.Modeling.Export;

using Cirreum.Authorization.Analysis;
using Cirreum.Authorization.Documentation.Formatters;

/// <summary>
/// A complete, serializable snapshot of an authorization system's current state.
/// </summary>
/// <remarks>
/// <para>
/// This record serves as the single entry point for extracting all authorization
/// information from a runtime environment. It is designed to be:
/// </para>
/// <list type="bullet">
/// <item><description>Fully serializable for API transport (JSON-friendly types only)</description></item>
/// <item><description>Self-contained with all data needed for visualization</description></item>
/// <item><description>Comparable across different runtime hosts (e.g., WASM vs Server)</description></item>
/// </list>
/// <para>
/// Typical usage scenarios:
/// </para>
/// <list type="bullet">
/// <item><description>WASM client interrogating its own runtime</description></item>
/// <item><description>Server API endpoint returning its authorization model to clients</description></item>
/// <item><description>Admin dashboard displaying both client and server authorization models</description></item>
/// </list>
/// </remarks>
public record AuthorizationSnapshot {

	/// <summary>
	/// UTC timestamp when this snapshot was captured.
	/// </summary>
	public required DateTime CapturedAtUtc { get; init; }

	/// <summary>
	/// The runtime environment where this snapshot was captured.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This identifies whether the snapshot originated from a Server, SPA (Blazor WASM), 
	/// Azure Function, or other supported runtime. Useful for comparing authorization 
	/// configurations across different deployment targets.
	/// </para>
	/// <para>
	/// For example, an admin dashboard might display side-by-side snapshots from both
	/// the WASM client and server API to verify consistent authorization coverage.
	/// </para>
	/// </remarks>
	public required DomainRuntimeType Runtime { get; init; }

	/// <summary>
	/// The complete domain catalog containing all resources organized by domain and kind.
	/// </summary>
	public required DomainCatalog Catalog { get; init; }

	/// <summary>
	/// Security analysis report with issues, metrics, and recommendations.
	/// </summary>
	public required AnalysisReport AnalysisReport { get; init; }

	/// <summary>
	/// Analysis summary with aggregated counts and pass/fail status.
	/// </summary>
	public required AnalysisSummary AnalysisSummary { get; init; }

	/// <summary>
	/// Role hierarchy information for all registered roles.
	/// </summary>
	public required IReadOnlyList<RoleHierarchyInfo> RoleHierarchy { get; init; }

	/// <summary>
	/// Mermaid diagram markup showing the authorization flow pipeline.
	/// </summary>
	public required string AuthorizationFlowDiagram { get; init; }

	/// <summary>
	/// Mermaid diagram markup showing the role inheritance hierarchy.
	/// </summary>
	public required string RoleHierarchyDiagram { get; init; }

	/// <summary>
	/// Total number of registered roles.
	/// </summary>
	public int TotalRoles => this.RoleHierarchy.Count;

	/// <summary>
	/// Creates a snapshot of the current authorization system state.
	/// </summary>
	/// <param name="roleRegistry">The role registry containing role definitions and hierarchy.</param>
	/// <param name="serviceProvider">Service provider for resolving validators and analyzers.</param>
	/// <param name="options">Optional analysis options. If null, defaults are used.</param>
	/// <returns>A complete authorization snapshot.</returns>
	public static AuthorizationSnapshot Capture(
		IAuthorizationRoleRegistry roleRegistry,
		IServiceProvider serviceProvider,
		AnalysisOptions? options = null) {

		// Ensure the model is initialized
		AuthorizationModel.Instance.Initialize(serviceProvider);

		// Run analysis
		var analysisOptions = options ?? new AnalysisOptions {
			MaxHierarchyDepth = 10,
			ExcludedCategories = []
		};

		var analyzer = DefaultAnalyzerProvider.CreateAnalyzer(roleRegistry, serviceProvider, analysisOptions);
		var analysisReport = analyzer.AnalyzeAll();

		// Build role hierarchy info
		var roleHierarchy = BuildRoleHierarchy(roleRegistry);

		return new AuthorizationSnapshot {
			Runtime = DomainContext.RuntimeType,
			CapturedAtUtc = DateTime.UtcNow,
			Catalog = AuthorizationModel.Instance.GetCatalog(),
			AnalysisReport = analysisReport,
			AnalysisSummary = analysisReport.GetSummary(),
			RoleHierarchy = roleHierarchy,
			AuthorizationFlowDiagram = AuthorizationFlowRenderer.ToMermaidDiagram(),
			RoleHierarchyDiagram = RoleHierarchyRenderer.ToMermaidDiagram(roleRegistry)
		};
	}

	private static List<RoleHierarchyInfo> BuildRoleHierarchy(IAuthorizationRoleRegistry roleRegistry) {
		var allRoles = roleRegistry.GetRegisteredRoles();
		var result = new List<RoleHierarchyInfo>();

		foreach (var role in allRoles) {
			var childRoles = roleRegistry.GetInheritedRoles(role);
			var parentRoles = roleRegistry.GetInheritingRoles(role);
			var depth = CalculateHierarchyDepth(role, [], roleRegistry);

			result.Add(new RoleHierarchyInfo(
				RoleString: role.ToString(),
				IsApplicationRole: role.IsApplicationRole,
				ChildRoleStrings: [.. childRoles.Select(r => r.ToString())],
				ParentRoleStrings: [.. parentRoles.Select(r => r.ToString())],
				InheritsFromCount: childRoles.Count,
				InheritedByCount: parentRoles.Count,
				HierarchyDepth: depth
			));
		}

		return [.. result
			.OrderBy(r => r.HierarchyDepth)
			.ThenBy(r => r.RoleString)];
	}

	private static int CalculateHierarchyDepth(Role role, HashSet<Role> visited, IAuthorizationRoleRegistry registry) {
		if (visited.Contains(role)) {
			return 0;
		}

		visited.Add(role);

		var inheritedRoles = registry.GetInheritedRoles(role);
		if (inheritedRoles.Count == 0) {
			return 0;
		}

		return inheritedRoles.Max(inherited =>
			CalculateHierarchyDepth(inherited, [.. visited], registry)) + 1;
	}

}
