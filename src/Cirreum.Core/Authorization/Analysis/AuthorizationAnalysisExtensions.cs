namespace Cirreum.Authorization.Analysis;

using Cirreum.Authorization.Analysis.Analyzers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for analyzing and visualizing the authorization system.
/// </summary>
public static class AuthorizationAnalysisExtensions {

	/// <summary>
	/// Performs a comprehensive security analysis of the role registry.
	/// </summary>
	/// <param name="registry">The <see cref="IAuthorizationRoleRegistry"/> to analyze.</param>
	/// <param name="services"></param>
	/// <returns>A comprehensive analysis report.</returns>
	public static Task<AnalysisReport> AnalyzeSecurityModelAsync(
		this IAuthorizationRoleRegistry registry,
		IServiceProvider services) {

		var analyzers = new List<IAuthorizationAnalyzer> {
			new RoleHierarchyAnalyzer(registry),
			new EnhancedAuthorizationRuleAnalyzer(services),
			new PolicyValidatorAnalyzer(services)
		};

		var analyzer = new CompositeAnalyzer(analyzers);
		return analyzer.AnalyzeAllAsync();
	}

	/// <summary>
	/// Analyzes the role hierarchy for structural issues.
	/// </summary>
	/// <param name="registry">The role registry to analyze.</param>
	/// <returns>Analysis report for role hierarchy.</returns>
	public static AnalysisReport AnalyzeRoleHierarchy(
		this IAuthorizationRoleRegistry registry)
		=> new RoleHierarchyAnalyzer(registry).Analyze();

	/// <summary>
	/// Logs the security analysis report using the provided logger.
	/// </summary>
	/// <param name="registry">The role registry to analyze.</param>
	/// <param name="logger">The logger to use.</param>
	/// <param name="services"></param>
	public static async Task LogSecurityAnalysisAsync(
		this IAuthorizationRoleRegistry registry,
		ILogger logger,
		IServiceProvider services) {

		var report = await registry.AnalyzeSecurityModelAsync(services);

		logger.LogInformation("Authorization Security Analysis:");

		foreach (var issue in report.Issues.OrderByDescending(i => i.Severity)) {
			switch (issue.Severity) {
				case IssueSeverity.Error:
					if (logger.IsEnabled(LogLevel.Error)) {
						logger.LogError("[{Category}] {Description}", issue.Category, issue.Description);
					}
					break;
				case IssueSeverity.Warning:
					if (logger.IsEnabled(LogLevel.Warning)) {
						logger.LogWarning("[{Category}] {Description}", issue.Category, issue.Description);
					}
					break;
				case IssueSeverity.Info:
					if (logger.IsEnabled(LogLevel.Information)) {
						logger.LogInformation("[{Category}] {Description}", issue.Category, issue.Description);
					}
					break;
			}
		}

		if (logger.IsEnabled(LogLevel.Information)) {
			foreach (var metric in report.Metrics) {
				logger.LogInformation("Metric: {MetricName} = {MetricValue}", metric.Key, metric.Value);
			}
		}

	}

}