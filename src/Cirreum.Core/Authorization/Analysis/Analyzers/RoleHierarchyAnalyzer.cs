namespace Cirreum.Authorization.Analysis.Analyzers;

using System.Collections.Immutable;

/// <summary>
/// Analyzes role hierarchy for structural issues, inheritance patterns, and security implications.
/// </summary>
public class RoleHierarchyAnalyzer(
	IAuthorizationRoleRegistry registry
) : IAuthorizationAnalyzer {

	private const string AnalyzerCategory = "Role Hierarchy";
	private const int MaxDepth = 10;

	public AnalysisReport Analyze() {
		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, object>();

		// Analyze basic hierarchy structure
		var (structureIssues, structureMetrics) = AnalyzeHierarchyStructure(registry);
		issues.AddRange(structureIssues);
		foreach (var metric in structureMetrics) {
			metrics[metric.Key] = metric.Value;
		}

		// Find possible circular references
		var circularIssues = AnalyzeCircularReferences(registry);
		issues.AddRange(circularIssues);

		return AnalysisReport.ForCategory(AnalyzerCategory, issues, metrics);
	}

	private static (List<AnalysisIssue>, Dictionary<string, object>) AnalyzeHierarchyStructure(
		IAuthorizationRoleRegistry registry) {

		var issues = new List<AnalysisIssue>();
		var metrics = new Dictionary<string, object>();
		var allRoles = registry.GetRegisteredRoles();

		// Find top-level roles (not inherited by any other role)
		var topRoles = allRoles.Where(r => registry.GetInheritingRoles(r).Count == 0).ToList();
		metrics["TopLevelRolesCount"] = topRoles.Count;

		// Find leaf roles (don't inherit from any other role)
		var leafRoles = allRoles.Where(r => registry.GetInheritedRoles(r).Count == 0).ToList();
		metrics["LeafRolesCount"] = leafRoles.Count;

		// Calculate hierarchy depth
		var (maxDepth, longestPath) = FindLongestPath(registry);
		metrics["MaxHierarchyDepth"] = maxDepth;

		if (maxDepth > MaxDepth) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Role hierarchy depth of {maxDepth} exceeds recommended maximum of {MaxDepth}",
				RelatedObjects: [.. longestPath.Cast<object>()]));
		}

		// Detect isolated roles (not part of main hierarchy)
		var isolatedRoles = FindIsolatedRoles(registry, allRoles);
		if (isolatedRoles.Count > 0) {
			issues.Add(new AnalysisIssue(
				Category: AnalyzerCategory,
				Severity: IssueSeverity.Warning,
				Description: $"Found {isolatedRoles.Count} isolated roles not connected to main hierarchy",
				RelatedObjects: [.. isolatedRoles.Cast<object>()]));
		}

		return (issues, metrics);
	}

	private static List<AnalysisIssue> AnalyzeCircularReferences(IAuthorizationRoleRegistry registry) {
		var issues = new List<AnalysisIssue>();
		var allRoles = registry.GetRegisteredRoles();

		foreach (var role in allRoles) {
			var visited = new HashSet<Role>();
			var path = new Stack<Role>();

			if (HasCycle(registry, role, visited, path)) {
				var cycle = path.Reverse().ToList();
				issues.Add(new AnalysisIssue(
					Category: AnalyzerCategory,
					Severity: IssueSeverity.Error,
					Description: $"Circular reference detected in role hierarchy: {string.Join(" -> ", cycle)}",
					RelatedObjects: [.. cycle.Cast<object>()]));
			}
		}

		return issues;
	}

	private static bool HasCycle(
		IAuthorizationRoleRegistry registry,
		Role current,
		HashSet<Role> visited,
		Stack<Role> path) {

		if (path.Contains(current)) {
			// Found a cycle
			while (path.Peek() != current) {
				path.Pop();
			}
			return true;
		}

		if (visited.Contains(current)) {
			return false;
		}

		visited.Add(current);
		path.Push(current);

		foreach (var child in registry.GetInheritedRoles(current)) {
			if (HasCycle(registry, child, visited, path)) {
				return true;
			}
		}

		path.Pop();
		return false;
	}

	//private static List<AnalysisIssue> AnalyzeSecurityBoundaries(IAuthorizationRoleRegistry registry) {
	//	var issues = new List<AnalysisIssue>();
	//	var appRoles = registry.GetRegisteredRoles().Where(r => r.IsApplicationRole).ToList();

	//	// Check that system role only inherits from admin
	//	var systemRole = ApplicationRoles.AppSystemRole;
	//	var systemInherits = registry.GetInheritedRoles(systemRole);

	//	if (systemInherits.Count > 1 || (systemInherits.Count == 1 && !systemInherits.Contains(ApplicationRoles.AppAdminRole))) {
	//		issues.Add(new AnalysisIssue(
	//			Category: AnalyzerCategory,
	//			Severity: IssueSeverity.Error,
	//			Description: $"Security boundary violation: System role should only inherit from Admin role",
	//			RelatedObjects: [systemRole, .. systemInherits.Cast<object>()]));
	//	}

	//	// Check that admin role only inherits from manager and agent
	//	var adminRole = ApplicationRoles.AppAdminRole;
	//	var adminInherits = registry.GetInheritedRoles(adminRole);
	//	var expectedAdminInherits = new HashSet<Role> {
	//		ApplicationRoles.AppManagerRole,
	//		ApplicationRoles.AppAgentRole
	//	};

	//	if (!adminInherits.SetEquals(expectedAdminInherits)) {
	//		var unexpected = adminInherits.Except(expectedAdminInherits).ToList();
	//		var missing = expectedAdminInherits.Except(adminInherits).ToList();

	//		if (unexpected.Count > 0) {
	//			issues.Add(new AnalysisIssue(
	//				Category: AnalyzerCategory,
	//				Severity: IssueSeverity.Warning,
	//				Description: $"Admin role has unexpected inheritance from: {string.Join(", ", unexpected)}",
	//				RelatedObjects: [.. unexpected.Cast<object>()]));
	//		}

	//		if (missing.Count > 0) {
	//			issues.Add(new AnalysisIssue(
	//				Category: AnalyzerCategory,
	//				Severity: IssueSeverity.Warning,
	//				Description: $"Admin role is missing expected inheritance from: {string.Join(", ", missing)}",
	//				RelatedObjects: [.. missing.Cast<object>()]));
	//		}
	//	}

	//	return issues;
	//}

	//private static List<AnalysisIssue> AnalyzeInvalidInheritance(IAuthorizationRoleRegistry registry) {
	//	var issues = new List<AnalysisIssue>();
	//	var allRoles = registry.GetRegisteredRoles();

	//	foreach (var role in allRoles) {
	//		var inheritedRoles = registry.GetInheritedRoles(role);

	//		// Check for custom roles inheriting from app:system or app:admin
	//		if (!role.IsApplicationRole ||
	//			(role != ApplicationRoles.AppSystemRole && role != ApplicationRoles.AppAdminRole)) {

	//			var restrictedInheritance = inheritedRoles.Where(r =>
	//				r == ApplicationRoles.AppSystemRole ||
	//				r == ApplicationRoles.AppAdminRole).ToList();

	//			if (restrictedInheritance.Count > 0) {
	//				issues.Add(new AnalysisIssue(
	//					Category: AnalyzerCategory,
	//					Severity: IssueSeverity.Error,
	//					Description: $"Security violation: Role '{role}' inherits from restricted role(s): {string.Join(", ", restrictedInheritance)}",
	//					RelatedObjects: [role, .. restrictedInheritance.Cast<object>()]));
	//			}
	//		}

	//		// Check if app namespace roles inherit from custom namespace roles
	//		if (role.IsApplicationRole) {
	//			var customNamespaceInheritance = inheritedRoles.Where(r => !r.IsApplicationRole).ToList();

	//			if (customNamespaceInheritance.Count > 0) {
	//				issues.Add(new AnalysisIssue(
	//					Category: AnalyzerCategory,
	//					Severity: IssueSeverity.Warning,
	//					Description: $"Application role '{role}' inherits from custom namespace role(s): {string.Join(", ", customNamespaceInheritance)}",
	//					RelatedObjects: [role, .. customNamespaceInheritance.Cast<object>()]));
	//			}
	//		}
	//	}

	//	return issues;
	//}

	private static (int Depth, List<Role> Path) FindLongestPath(IAuthorizationRoleRegistry registry) {
		var allRoles = registry.GetRegisteredRoles();
		var maxDepth = 0;
		var longestPath = new List<Role>();

		foreach (var role in allRoles) {
			var (depth, path) = FindLongestPathFromRole(registry, role);
			if (depth > maxDepth) {
				maxDepth = depth;
				longestPath = path;
			}
		}

		return (maxDepth, longestPath);
	}

	private static (int Depth, List<Role> Path) FindLongestPathFromRole(
		IAuthorizationRoleRegistry registry,
		Role role,
		HashSet<Role>? visited = null) {

		visited ??= [];

		if (visited.Contains(role)) {
			return (0, new List<Role>());
		}

		visited.Add(role);

		var inheritedRoles = registry.GetInheritedRoles(role);
		if (inheritedRoles.Count == 0) {
			return (1, new List<Role> { role });
		}

		var maxDepth = 0;
		var longestPath = new List<Role>();

		foreach (var child in inheritedRoles) {
			var visitedCopy = new HashSet<Role>(visited);
			var (childDepth, childPath) = FindLongestPathFromRole(registry, child, visitedCopy);

			if (childDepth > maxDepth) {
				maxDepth = childDepth;
				longestPath = childPath;
			}
		}

		longestPath.Insert(0, role);
		return (maxDepth + 1, longestPath);
	}

	private static List<Role> FindIsolatedRoles(IAuthorizationRoleRegistry registry, IImmutableSet<Role> allRoles) {
		// Build a graph of all connected roles
		var connected = new HashSet<Role>();
		var queue = new Queue<Role>();

		// Start with app:system role as it should be at the top of hierarchy
		queue.Enqueue(ApplicationRoles.AppSystemRole);
		connected.Add(ApplicationRoles.AppSystemRole);

		while (queue.Count > 0) {
			var current = queue.Dequeue();

			// Add roles that inherit from current role
			foreach (var parent in registry.GetInheritingRoles(current)) {
				if (connected.Add(parent)) {
					queue.Enqueue(parent);
				}
			}

			// Add roles that current role inherits from
			foreach (var child in registry.GetInheritedRoles(current)) {
				if (connected.Add(child)) {
					queue.Enqueue(child);
				}
			}
		}

		// Return roles not in the connected set
		return [.. allRoles.Except(connected)];
	}
}