namespace Cirreum.Authorization.Analysis;

using Cirreum.Authorization.Analysis.Analyzers;

/// <summary>
/// Provides default authorization analyzers and a convenient way to run analysis.
/// </summary>
public static class DefaultAnalyzerProvider {

	/// <summary>
	/// Creates a composite analyzer with all default analyzers and the specified options.
	/// This is the recommended way to run authorization analysis.
	/// </summary>
	/// <param name="roleRegistry">The role registry for role-based analysis.</param>
	/// <param name="services">Optional service provider for analyzers that require DI.</param>
	/// <param name="options">Optional analysis options. If null, uses default options.</param>
	/// <returns>A composite analyzer ready to run analysis.</returns>
	public static CompositeAnalyzer CreateAnalyzer(
		IAuthorizationRoleRegistry roleRegistry,
		IServiceProvider? services = null,
		AnalysisOptions? options = null) {

		var analysisOptions = options ?? AnalysisOptions.Default;
		var analyzers = GetAnalyzers(roleRegistry, analysisOptions, services);
		return new CompositeAnalyzer(analyzers, analysisOptions);
	}

	/// <summary>
	/// Gets all default analyzers.
	/// </summary>
	public static IReadOnlyList<IAuthorizationAnalyzer> GetDefaultAnalyzers(
		IAuthorizationRoleRegistry roleRegistry,
		IServiceProvider? services = null) {

		var analyzers = new List<IAuthorizationAnalyzer> {
			new AuthorizationRuleAnalyzer(),
			new RoleHierarchyAnalyzer(roleRegistry),
			new AnonymousResourceAnalyzer()
		};

		// Only add service-dependent analyzers if services are provided
		if (services != null) {
			analyzers.Add(new AuthorizableResourceAnalyzer(services));
			analyzers.Add(new PolicyValidatorAnalyzer(services));
		}

		return analyzers;
	}

	/// <summary>
	/// Gets analyzers filtered by options.
	/// </summary>
	public static IReadOnlyList<IAuthorizationAnalyzer> GetAnalyzers(
		IAuthorizationRoleRegistry roleRegistry,
		AnalysisOptions options,
		IServiceProvider? services = null) {

		var analyzers = GetDefaultAnalyzers(roleRegistry, services);

		if (options.ExcludedCategories.Count == 0) {
			return analyzers;
		}

		// Filter based on analyzer category
		return [.. analyzers.Where(a => {
			var categoryField = a.GetType()
				.GetField("AnalyzerCategory",
					System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.Static);

			if (categoryField?.GetValue(null) is string category) {
				return !options.ExcludedCategories.Contains(category);
			}

			return true;
		})];
	}
}