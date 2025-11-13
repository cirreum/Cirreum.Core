namespace Cirreum.Authorization.Analysis;

/// <summary>
/// Defines interface of an authorization analyzers.
/// </summary>
public interface IAuthorizationAnalyzer {
	/// <summary>
	/// Analyzes for potential issues.
	/// </summary>
	/// <returns>A report containing any found issues and metrics.</returns>
	AnalysisReport Analyze();
}