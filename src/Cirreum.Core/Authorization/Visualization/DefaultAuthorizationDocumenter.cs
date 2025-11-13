namespace Cirreum.Authorization.Visualization;

using Cirreum.Authorization.Analysis;
using FluentValidation;
using System.Data;
using System.Text;

sealed class DefaultAuthorizationDocumenter(
	IAuthorizationRoleRegistry roleRegistry,
	IServiceProvider serviceProvider
) : IAuthorizationDocumenter {

	public async Task<string> GenerateMarkdown() {
		var sb = new StringBuilder();

		// First, visualize the role hierarchy
		sb.AppendLine("\n# Authorization System Documentation");
		sb.AppendLine();

		sb.AppendLine("## Role Hierarchy");
		sb.AppendLine();
		sb.AppendLine("```text");
		sb.AppendLine(RoleHierarchyRenderer.ToTextTree(roleRegistry));
		sb.AppendLine("```");
		sb.AppendLine();

		sb.AppendLine("## Role Hierarchy Diagram");
		sb.AppendLine();
		sb.AppendLine("```mermaid");
		sb.AppendLine(RoleHierarchyRenderer.ToMermaidDiagram(roleRegistry));
		sb.AppendLine("```");
		sb.AppendLine();

		// Then, document authorization rules
		var rules = AuthorizationRuleProvider.Instance.GetAllRules();
		sb.AppendLine("# Authorization Rules");
		sb.AppendLine();

		// Add CSS for indentation and styling
		sb.AppendLine("<style>");
		sb.AppendLine("  .auth-resource > summary {");
		sb.AppendLine("    font-size: 18px;");
		sb.AppendLine("    font-weight: bold;");
		sb.AppendLine("    padding: 8px;");
		sb.AppendLine("    background-color: #000;");
		sb.AppendLine("    border-left: 4px solid #555;");
		sb.AppendLine("    margin-bottom: 10px;");
		sb.AppendLine("  }");
		sb.AppendLine("  .auth-validator {");
		sb.AppendLine("    margin-left: 25px;");
		sb.AppendLine("  }");
		sb.AppendLine("  .auth-validator > summary {");
		sb.AppendLine("    font-size: 16px;");
		sb.AppendLine("    font-weight: bold;");
		sb.AppendLine("    padding: 6px;");
		sb.AppendLine("    background-color: #181818;");
		sb.AppendLine("    border-left: 3px solid #777;");
		sb.AppendLine("  }");
		sb.AppendLine("  .auth-validator table {");
		sb.AppendLine("    margin-left: 15px;");
		sb.AppendLine("    margin-bottom: 10px;");
		sb.AppendLine("  }");
		sb.AppendLine("  .auth-validator th {");
		sb.AppendLine("    background-color: #252525;");
		sb.AppendLine("  }");
		sb.AppendLine("  .auth-property {");
		sb.AppendLine("    font-weight: bold;");
		sb.AppendLine("  }");
		sb.AppendLine("</style>");
		sb.AppendLine();

		// Group rules by resource type
		var rulesByResource = rules
			.GroupBy(r => r.ResourceType)
			.OrderBy(g => g.Key.Name);

		foreach (var resourceGroup in rulesByResource) {
			sb.AppendLine();
			// Create a collapsible section for each resource type
			sb.AppendLine($"<details class=\"auth-resource\">");
			sb.AppendLine($"<summary>{resourceGroup.Key.Name}</summary>");
			sb.AppendLine();

			// Group by validator within each resource
			var validatorGroups = resourceGroup
				.GroupBy(r => r.ValidatorType)
				.OrderBy(g => g.Key.Name);

			foreach (var validatorGroup in validatorGroups) {
				// Create a nested collapsible section for each validator
				sb.AppendLine($"<details class=\"auth-validator\">");
				sb.AppendLine($"<summary>{validatorGroup.Key.Name}</summary>");
				sb.AppendLine();

				// Create a table for all rules within this validator
				sb.AppendLine("<table>");
				sb.AppendLine("  <thead>");
				sb.AppendLine("    <tr>");
				sb.AppendLine("      <th>Property</th>");
				sb.AppendLine("      <th>Validation</th>");
				sb.AppendLine("      <th>Message</th>");
				sb.AppendLine("      <th>Condition</th>");
				sb.AppendLine("    </tr>");
				sb.AppendLine("  </thead>");
				sb.AppendLine("  <tbody>");

				foreach (var rule in validatorGroup) {
					var condition = !string.IsNullOrEmpty(rule.Condition) ? rule.Condition : "-";
					sb.AppendLine("    <tr>");
					sb.AppendLine($"      <td class=\"auth-property\">{rule.PropertyPath ?? "AuthorizationContext"}</td>");
					sb.AppendLine($"      <td>{rule.ValidationLogic}</td>");
					sb.AppendLine($"      <td>{rule.Message}</td>");
					sb.AppendLine($"      <td>{condition}</td>");
					sb.AppendLine("    </tr>");
				}

				sb.AppendLine("  </tbody>");
				sb.AppendLine("</table>");
				sb.AppendLine();
				sb.AppendLine("</details>");
			}

			sb.AppendLine("</details>");
		}


		// Add a section for role-permission mapping
		sb.AppendLine();
		sb.AppendLine("## Role-Based Access Rules");
		sb.AppendLine();
		sb.AppendLine("| Role | Resources |");
		sb.AppendLine("|------|-----------|");

		var allRoles = roleRegistry.GetRegisteredRoles();
		foreach (var role in allRoles) {
			var roleRules = rules
				.Where(r => !string.IsNullOrWhiteSpace(r.PropertyPath) && r.PropertyPath.Contains("UserRoles") && r.Message.Contains(role))
				.ToList();

			var resourceCount = roleRules.Select(r => r.ResourceType).Distinct().Count();

			sb.AppendLine($"| {role} | {resourceCount} |");
		}
		sb.AppendLine();

		// Add Analysis Results
		var analysisReport = await this.GetAnalysisReportAsync();
		sb.Append(analysisReport.ToMarkdown());

		return sb.ToString();

	}

	public async Task<string> GenerateCsv() {
		var sb = new StringBuilder();
		var allRoles = roleRegistry.GetRegisteredRoles();

		// Add metadata header for better import functionality
		sb.AppendLine("# AUTHORIZATION SYSTEM EXPORT");
		sb.AppendLine($"# Generated: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
		sb.AppendLine("# Format: CSV");
		sb.AppendLine();

		// SECTION 1: Role hierarchy with improved structure
		sb.AppendLine("## ROLE HIERARCHY");
		sb.AppendLine("Section,ParentRole,ChildRole,InheritanceDepth");

		var processedRoles = new HashSet<string>();
		foreach (var role in allRoles) {
			var childRoles = roleRegistry.GetInheritedRoles(role);
			foreach (var childRole in childRoles) {
				// Calculate an approximate inheritance depth for visualization tools
				var inheritanceDepth = 1; // Default to direct inheritance

				// Add relationship to CSV
				sb.AppendLine(
					$"RoleHierarchy," +
					$"{EscapeCsvField(role.ToString())}," +
					$"{EscapeCsvField(childRole.ToString())}," +
					$"{inheritanceDepth}");

				processedRoles.Add(role.ToString());
				processedRoles.Add(childRole.ToString());
			}
		}

		// Add standalone roles (not in any hierarchy)
		foreach (var role in allRoles) {
			if (!processedRoles.Contains(role.ToString())) {
				sb.AppendLine(
					$"RoleHierarchy," +
					$"{EscapeCsvField(role.ToString())}," +
					$"," + // No child
					$"0"); // Zero depth (standalone)
			}
		}

		sb.AppendLine();

		// SECTION 2: Authorization rules with improved structure for visualization
		var rules = AuthorizationRuleProvider.Instance.GetAllRules();
		sb.AppendLine("## AUTHORIZATION RULES");
		sb.AppendLine("Section,ResourceName,ValidatorName,PropertyPath,ValidationType,Message,Condition,IncludesRBAC,SortOrder");

		var sortOrder = 0;
		foreach (var rule in rules) {
			sortOrder++;

			// Determine if the ABAC rule includes RBAC
			var includesRBAC =
				!string.IsNullOrWhiteSpace(rule.PropertyPath)
				&& rule.PropertyPath == nameof(AuthorizationContext<IAuthorizableResource>.UserRoles);

			// Extract validation type for better categorization
			var validationType = ExtractValidationType(rule.ValidationLogic);

			sb.AppendLine(
				$"AuthRule," +
				$"{EscapeCsvField(rule.ResourceType.Name)}," +
				$"{EscapeCsvField(rule.ValidatorType.Name)}," +
				$"{EscapeCsvField(rule.PropertyPath ?? "AuthorizationContext")}," +
				$"{EscapeCsvField(validationType)}," +
				$"{EscapeCsvField(rule.Message)}," +
				$"{EscapeCsvField(rule.Condition ?? "")}," +
				$"{(includesRBAC ? "True" : "False")}," +
				$"{sortOrder}");
		}

		sb.AppendLine();

		// SECTION 3: Resource-Role Matrix (excellent for heat map visualizations)
		sb.AppendLine("## RESOURCE ROLE MATRIX");
		sb.AppendLine("Section,ResourceName,RoleName,AccessConditions");

		// Get unique resource types
		var resourceTypes = rules
			.Select(r => r.ResourceType.Name)
			.Distinct();

		// Generate the matrix
		foreach (var resourceType in resourceTypes) {
			foreach (var role in allRoles) {
				// Check for explicit rules
				var explicitRules = rules.Where(r =>
					r.ResourceType.Name == resourceType &&
					r.Message.Contains(role.ToString()));
				if (explicitRules.Any()) {

					// Get access conditions
					var conditions = explicitRules
						.Where(r => !string.IsNullOrEmpty(r.Condition))
						.Select(r => r.Condition)
						.Where(c => c != null);

					var accessConditions = string.Join("; ", conditions);

					sb.AppendLine(
						$"ResourceRoleMatrix," +
						$"{EscapeCsvField(resourceType)}," +
						$"{EscapeCsvField(role.ToString())}," +
						$"{EscapeCsvField(accessConditions)}");

				}
			}
		}

		sb.AppendLine();

		// SECTION 4: Security analysis with improved structure
		var analysisReport = await this.GetAnalysisReportAsync();
		sb.AppendLine("## SECURITY ANALYSIS");
		sb.AppendLine("Section,Category,Severity,Description,RelatedObjects,ImpactedResources,ImpactedRoles");

		foreach (var issue in analysisReport.Issues) {
			// Join related objects with semicolon for CSV compatibility
			var relatedObjs = string.Join(";", issue.RelatedObjects.Select(o => o.ToString()));

			// Extract impacted resources
			var impactedResources = string.Join(";",
				issue.RelatedObjects
					.OfType<Type>()
					.Where(t => resourceTypes.Contains(t.Name))
					.Select(t => t.Name));

			// Extract impacted roles
			var impactedRoles = string.Join(";",
				issue.RelatedObjects
					.Where(o => allRoles.Any(r => o.ToString()?.Contains(r.ToString()) ?? false))
					.Select(o => o.ToString()));

			sb.AppendLine(
				$"SecurityIssue," +
				$"{EscapeCsvField(issue.Category)}," +
				$"{EscapeCsvField(issue.Severity.ToString())}," +
				$"{EscapeCsvField(issue.Description)}," +
				$"{EscapeCsvField(relatedObjs)}," +
				$"{EscapeCsvField(impactedResources)}," +
				$"{EscapeCsvField(impactedRoles)}");
		}

		return sb.ToString();
	}
	private static string ExtractValidationType(string validationLogic) {
		return validationLogic.Replace(" ", "");
	}
	private static string EscapeCsvField(string field) {
		if (string.IsNullOrEmpty(field)) {
			return "";
		}

		if (field.Contains(',') || field.Contains('"') || field.Contains('\n')) {
			return $"\"{field.Replace("\"", "\"\"")}\"";
		}
		return field;
	}

	public async Task<string> RenderHtmlPage() {
		var sb = new StringBuilder();

		sb.AppendLine("<!DOCTYPE html>");
		sb.AppendLine("<html>");
		sb.AppendLine("<head>");
		sb.AppendLine("  <title>Authorization System Visualization</title>");
		sb.AppendLine("  <style>");
		sb.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; }");
		sb.AppendLine("    .role { background-color: #f8f0ff; border: 1px solid #d0b0ff; border-radius: 4px; margin: 5px; padding: 10px; }");
		sb.AppendLine("    .app-role { background-color: #f0f0ff; border: 1px solid #b0b0ff; }");
		sb.AppendLine("    .custom-role { background-color: #fff0f0; border: 1px solid #ffb0b0; }");
		sb.AppendLine("    .resource { background-color: #f0fff0; border: 1px solid #b0ffb0; border-radius: 4px; margin: 10px 0; padding: 10px; }");
		sb.AppendLine("    .validator { margin-left: 20px; }");
		sb.AppendLine("    .rule { margin-left: 40px; background-color: #fffff0; border: 1px solid #ffffd0; border-radius: 4px; padding: 8px; margin-bottom: 5px; }");
		sb.AppendLine("    .inheritance { color: #666; }");
		sb.AppendLine("    .error { background-color: #ffeeee; border: 1px solid #ffaaaa; border-radius: 4px; padding: 8px; margin: 5px 0; }");
		sb.AppendLine("    .warning { background-color: #ffffee; border: 1px solid #ffffaa; border-radius: 4px; padding: 8px; margin: 5px 0; }");
		sb.AppendLine("    .info { background-color: #eeeeff; border: 1px solid #aaaaff; border-radius: 4px; padding: 8px; margin: 5px 0; }");
		sb.AppendLine("    h1, h2, h3 { color: #444; }");
		sb.AppendLine("    .diagram { margin: 20px 0; overflow: auto; max-height: 800px; }");
		sb.AppendLine("    .tabs { display: flex; margin-bottom: 10px; }");
		sb.AppendLine("    .tab { padding: 8px 16px; cursor: pointer; border: 1px solid #ccc; margin-right: 4px; }");
		sb.AppendLine("    .tab.active { background-color: #f0f0f0; border-bottom: none; }");
		sb.AppendLine("    .tab-content { display: none; padding: 20px; border: 1px solid #ccc; }");
		sb.AppendLine("    .tab-content.active { display: block; }");
		sb.AppendLine("  </style>");
		sb.AppendLine("  <script src=\"https://unpkg.com/mermaid@11.7.0/dist/mermaid.min.js\"></script>");
		sb.AppendLine("</head>");
		sb.AppendLine("<body>");

		sb.AppendLine("<h1>Authorization System Documentation</h1>");

		// Add tabs for navigation - REMOVED the Complete Diagram tab
		sb.AppendLine("<div class=\"tabs\">");
		sb.AppendLine("  <div class=\"tab active\" onclick=\"showTab('roles')\">Roles</div>");
		sb.AppendLine("  <div class=\"tab\" onclick=\"showTab('rules')\">Rules</div>");
		sb.AppendLine("  <div class=\"tab\" onclick=\"showTab('analysis')\">Security Analysis</div>");
		sb.AppendLine("</div>");

		// Roles Tab
		sb.AppendLine("<div id=\"roles\" class=\"tab-content active\">");
		sb.AppendLine("  <h2>Role Hierarchy</h2>");

		var allRoles = roleRegistry.GetRegisteredRoles();

		foreach (var role in allRoles) {
			var roleClass = role.IsApplicationRole ? "app-role" : "custom-role";
			sb.AppendLine($"  <div class=\"role {roleClass}\">");
			sb.AppendLine($"    <h4>{role}</h4>");

			var childRoles = roleRegistry.GetInheritedRoles(role);
			if (childRoles.Count > 0) {
				sb.AppendLine("    <div class=\"inheritance\">");
				sb.AppendLine("      <strong>Inherits from:</strong> " + string.Join(", ", childRoles));
				sb.AppendLine("    </div>");
			}

			var parentRoles = roleRegistry.GetInheritingRoles(role);
			if (parentRoles.Count > 0) {
				sb.AppendLine("    <div class=\"inheritance\">");
				sb.AppendLine("      <strong>Inherited by:</strong> " + string.Join(", ", parentRoles));
				sb.AppendLine("    </div>");
			}

			sb.AppendLine("  </div>");
		}

		sb.AppendLine("  <h3>Role Hierarchy Diagram</h3>");
		sb.AppendLine("  <div class=\"diagram\">");
		sb.AppendLine("    <div class=\"mermaid\">");
		sb.AppendLine("graph TD");

		// Add role relationships for diagram
		foreach (var role in allRoles) {
			var roleId = role.ToString().Replace(":", "_");
			var childRoles = roleRegistry.GetInheritedRoles(role);

			foreach (var childRole in childRoles) {
				var childRoleId = childRole.ToString().Replace(":", "_");
				sb.AppendLine($"      {roleId}[\"{role}\"] --> {childRoleId}[\"{childRole}\"]");
			}
		}

		sb.AppendLine("    </div>");
		sb.AppendLine("  </div>");
		sb.AppendLine("</div>");

		// Rules Tab
		sb.AppendLine("<div id=\"rules\" class=\"tab-content\">");
		sb.AppendLine("  <h2>Authorization Rules</h2>");

		// Group rules by resource
		var rules = AuthorizationRuleProvider.Instance.GetAllRules();
		var rulesByResource = rules
			.GroupBy(r => r.ResourceType)
			.OrderBy(g => g.Key.Name);

		foreach (var resourceGroup in rulesByResource) {
			sb.AppendLine($"  <div class=\"resource\">");
			sb.AppendLine($"    <h3>Resource: {resourceGroup.Key.Name}</h3>");

			// Group by validator
			var validatorGroups = resourceGroup
				.GroupBy(r => r.ValidatorType)
				.OrderBy(g => g.Key.Name);

			foreach (var validatorGroup in validatorGroups) {
				sb.AppendLine($"    <div class=\"validator\">");
				sb.AppendLine($"      <h4>Validator: {validatorGroup.Key.Name}</h4>");

				foreach (var rule in validatorGroup) {
					sb.AppendLine($"      <div class=\"rule\">");
					sb.AppendLine($"        <strong>{rule.PropertyPath ?? "AuthorizationContext"}</strong>");
					sb.AppendLine($"        <div>Validation: {rule.ValidationLogic}</div>");
					sb.AppendLine($"        <div>Message: {rule.Message}</div>");

					if (!string.IsNullOrEmpty(rule.Condition)) {
						sb.AppendLine($"        <div>Condition: {rule.Condition}</div>");
					}

					// If rule mentions roles, display them
					var relatedRoles = allRoles.Where(r => rule.ValidationLogic.Contains(r.ToString())).ToList();
					if (relatedRoles.Count != 0) {
						sb.AppendLine($"        <div>Related Roles: {string.Join(", ", relatedRoles)}</div>");
					}

					sb.AppendLine($"      </div>");
				}

				sb.AppendLine($"    </div>");
			}

			sb.AppendLine($"  </div>");
		}
		sb.AppendLine("</div>");

		// Analysis Tab
		sb.AppendLine("<div id=\"analysis\" class=\"tab-content\">");
		sb.AppendLine("  <h2>Security Analysis</h2>");

		var analysisReport = await this.GetAnalysisReportAsync();

		// Overall Status
		sb.AppendLine("  <div class=\"resource\">");
		sb.AppendLine("    <h3>Overall Status</h3>");
		sb.AppendLine($"    <div>Issues Found: {(analysisReport.HasIssues ? "Yes" : "No")}</div>");
		sb.AppendLine($"    <div>Total Issues: {analysisReport.Issues.Count}</div>");

		// Quick Summary
		sb.AppendLine("    <h4>Summary</h4>");
		var errorCount = analysisReport.Issues.Count(i => i.Severity == IssueSeverity.Error);
		var warningCount = analysisReport.Issues.Count(i => i.Severity == IssueSeverity.Warning);
		var infoCount = analysisReport.Issues.Count(i => i.Severity == IssueSeverity.Info);

		sb.AppendLine($"    <div><span style=\"color: #cc0000; font-weight: bold;\">&#9679;</span> Error: {errorCount}</div>");
		sb.AppendLine($"    <div><span style=\"color: #cccc00; font-weight: bold;\">&#9679;</span> Warning: {warningCount}</div>");
		sb.AppendLine($"    <div><span style=\"color: #00cc00; font-weight: bold;\">&#9679;</span> Info: {infoCount}</div>");
		sb.AppendLine("  </div>");

		// Detailed Issues
		sb.AppendLine("  <div class=\"resource\">");
		sb.AppendLine("    <h3>Detailed Issues</h3>");

		foreach (var category in analysisReport.AnalyzerCategories.OrderBy(c => c)) {
			sb.AppendLine($"    <h4>{category}</h4>");
			var categoryIssues = analysisReport.Issues.Where(i => i.Category == category);

			if (!categoryIssues.Any()) {
				sb.AppendLine("    <div>No issues found</div>");
				continue;
			}

			foreach (var severityGroup in categoryIssues.GroupBy(issue => issue.Severity)) {
				var severityClass = severityGroup.Key.ToString().ToLower();
				var issueIndex = 1;
				foreach (var issue in severityGroup) {
					sb.AppendLine($"    <div class=\"{severityClass}\">");
					sb.AppendLine($"      <strong>Issue {issueIndex++}: {issue.Description}</strong>");
					if (issue.RelatedObjects != null && issue.RelatedObjects.Any()) {
						sb.AppendLine($"      <div>Related Objects: {string.Join(", ", issue.RelatedObjects)}</div>");
					}
					sb.AppendLine("    </div>");
				}
			}
		}
		sb.AppendLine("  </div>");

		// Security Issues Diagram (optional, only if there are issues)
		var analysisIssues = analysisReport.Issues.Where(i => i.Severity == IssueSeverity.Error || i.Severity == IssueSeverity.Warning).ToList();
		if (analysisIssues.Count != 0) {
			sb.AppendLine("  <h3>Security Issues Diagram</h3>");
			sb.AppendLine("  <div class=\"diagram\">");
			sb.AppendLine("    <div class=\"mermaid\">");
			sb.AppendLine("graph TD");

			for (var i = 0; i < analysisIssues.Count; i++) {
				var issue = analysisIssues[i];
				var issueId = $"Issue_{i}";
				var severity = issue.Severity == IssueSeverity.Error ? "ERROR" : "WARNING";
				var description = issue.Description.Replace("\"", "'").Replace("\n", "<br/>");
				sb.AppendLine($"    {issueId}[\"{severity}: {description}\"]");

				// Connect issues to related roles if applicable
				foreach (var relatedObj in issue.RelatedObjects) {
					if (relatedObj is Role role) {
						var roleId = role.ToString().Replace(":", "_");
						sb.AppendLine($"    {issueId} -.-> {roleId}[\"{role}\"]");
					}
				}
			}

			sb.AppendLine("    %% Styling");
			sb.AppendLine("    classDef error fill:#ffcccc,stroke:#990000,stroke-width:2px;");
			sb.AppendLine("    classDef warning fill:#ffffcc,stroke:#999900,stroke-width:1px;");

			// Apply styles
			for (var i = 0; i < analysisIssues.Count; i++) {
				var issue = analysisIssues[i];
				var issueId = $"Issue_{i}";
				var styleClass = issue.Severity == IssueSeverity.Error ? "error" : "warning";
				sb.AppendLine($"    class {issueId} {styleClass};");
			}

			sb.AppendLine("    </div>");
			sb.AppendLine("  </div>");
		}

		sb.AppendLine("</div>");

		// Add tab switching JavaScript and initialize Mermaid
		sb.AppendLine("<script>");
		sb.AppendLine("// Initialize mermaid with configuration");
		sb.AppendLine("mermaid.initialize({");
		sb.AppendLine("  startOnLoad: true,");
		sb.AppendLine("  securityLevel: 'loose',");
		sb.AppendLine("  theme: 'default',");
		sb.AppendLine("  flowchart: { useMaxWidth: false, htmlLabels: true }");
		sb.AppendLine("});");
		sb.AppendLine("");
		sb.AppendLine("function showTab(tabId) {");
		sb.AppendLine("  // Hide all tab contents");
		sb.AppendLine("  document.querySelectorAll('.tab-content').forEach(content => {");
		sb.AppendLine("    content.classList.remove('active');");
		sb.AppendLine("  });");
		sb.AppendLine("  ");
		sb.AppendLine("  // Show the selected tab content");
		sb.AppendLine("  document.getElementById(tabId).classList.add('active');");
		sb.AppendLine("  ");
		sb.AppendLine("  // Update tab buttons");
		sb.AppendLine("  document.querySelectorAll('.tab').forEach(tab => {");
		sb.AppendLine("    tab.classList.remove('active');");
		sb.AppendLine("  });");
		sb.AppendLine("  ");
		sb.AppendLine("  // Add active class to clicked tab");
		sb.AppendLine("  document.querySelectorAll('.tab').forEach(tab => {");
		sb.AppendLine("    if (tab.textContent.toLowerCase().includes(tabId)) {");
		sb.AppendLine("      tab.classList.add('active');");
		sb.AppendLine("    }");
		sb.AppendLine("  });");
		sb.AppendLine("  ");
		sb.AppendLine("  // Re-render mermaid diagrams when switching tabs");
		sb.AppendLine("  if (document.getElementById(tabId).querySelector('.mermaid')) {");
		sb.AppendLine("    setTimeout(() => {");
		sb.AppendLine("      try {");
		sb.AppendLine("        mermaid.init(undefined, document.getElementById(tabId).querySelectorAll('.mermaid'));");
		sb.AppendLine("      } catch (error) {");
		sb.AppendLine("        console.error('Error initializing mermaid:', error);");
		sb.AppendLine("      }");
		sb.AppendLine("    }, 100);");
		sb.AppendLine("  }");
		sb.AppendLine("}");
		sb.AppendLine("");
		sb.AppendLine("// Initialize on page load");
		sb.AppendLine("window.addEventListener('load', function() {");
		sb.AppendLine("  // Initialize for the active tab on page load");
		sb.AppendLine("  try {");
		sb.AppendLine("    mermaid.init(undefined, document.querySelectorAll('.tab-content.active .mermaid'));");
		sb.AppendLine("  } catch (error) {");
		sb.AppendLine("    console.error('Error initializing mermaid:', error);");
		sb.AppendLine("  }");
		sb.AppendLine("});");
		sb.AppendLine("</script>");

		sb.AppendLine("</body>");
		sb.AppendLine("</html>");

		return sb.ToString();
	}

	private Task<AnalysisReport> GetAnalysisReportAsync()
		=> roleRegistry.AnalyzeSecurityModelAsync(serviceProvider);

}