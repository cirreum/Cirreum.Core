namespace Cirreum.Introspection;

using Cirreum.Authorization;
using Cirreum.Introspection.Modeling;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Boot-time entry point for running the full authorization analyzer suite and converting
/// <see cref="IssueSeverity.Error"/> findings into a startup failure. Closes the gap
/// between "the framework gives you an analyzer" and "the framework prevents you from
/// shipping a misconfigured app."
/// </summary>
/// <remarks>
/// <para>
/// Call once after the host has been built, before the app begins serving requests:
/// </para>
/// <code>
/// var app = builder.Build();
/// app.Services.ValidateAuthorizationConfiguration();   // throws on Error severity
/// app.Run();
/// </code>
/// <para>
/// Cross-platform — works in any host that exposes <see cref="IServiceProvider"/>:
/// ASP.NET Core, Functions, WASM bootstraps, console hosts. Runtime-extension packages
/// may wrap this in a hosted service so the call site is implicit.
/// </para>
/// <para>
/// The validator runs every registered <see cref="IDomainAnalyzer"/> against the live DI
/// container and the scanned domain model. Findings at <see cref="IssueSeverity.Error"/>
/// throw <see cref="AuthorizationConfigurationException"/> with the full report attached;
/// warnings and info-level findings are not fatal but are present on the exception's
/// <see cref="AuthorizationConfigurationException.Report"/> for inspection.
/// </para>
/// <para>
/// Both extensions internally create a DI scope via <see cref="ServiceProviderServiceExtensions.CreateScope"/>
/// so scoped services (<see cref="IAuthorizationRoleRegistry"/>, the policy/constraint/grant
/// service collections that analyzers depend on) resolve correctly. Calling these methods
/// against the root provider is safe under .NET's default scope-validation mode.
/// </para>
/// </remarks>
public static class AuthorizationStartupValidationExtensions {

	/// <summary>
	/// Runs the configured authorization analyzers and throws
	/// <see cref="AuthorizationConfigurationException"/> if any reports an error.
	/// </summary>
	/// <param name="services">The application service provider, after host build.</param>
	/// <param name="options">
	/// Optional analysis options (e.g., excluded categories, max hierarchy depth). Defaults
	/// to <see cref="AnalysisOptions.Default"/>.
	/// </param>
	/// <exception cref="AuthorizationConfigurationException">
	/// One or more analyzers reported <see cref="IssueSeverity.Error"/> findings.
	/// </exception>
	public static void ValidateAuthorizationConfiguration(
		this IServiceProvider services,
		AnalysisOptions? options = null) {

		ArgumentNullException.ThrowIfNull(services);

		using var scope = services.CreateScope();
		var sp = scope.ServiceProvider;

		var registry = sp.GetRequiredService<IAuthorizationRoleRegistry>();

		// Ensure the domain model has scanned (idempotent if already initialized).
		DomainModel.Instance.Initialize(sp);

		var analyzer = DomainAnalyzerProvider.CreateAnalyzer(registry, sp, options);
		var report = analyzer.AnalyzeAll();
		var summary = report.GetSummary();

		if (!summary.Passed) {
			throw new AuthorizationConfigurationException(report, summary);
		}
	}

	/// <summary>
	/// Same as <see cref="ValidateAuthorizationConfiguration"/> but returns the report
	/// instead of throwing — for callers that want to log or branch on the result.
	/// Returns <see langword="null"/> when validation passes.
	/// </summary>
	/// <param name="services">The application service provider, after host build.</param>
	/// <param name="options">Optional analysis options.</param>
	/// <returns>
	/// The full <see cref="AnalysisReport"/> if Error-severity findings were detected, or
	/// <see langword="null"/> when the configuration passes.
	/// </returns>
	public static AnalysisReport? CheckAuthorizationConfiguration(
		this IServiceProvider services,
		AnalysisOptions? options = null) {

		ArgumentNullException.ThrowIfNull(services);

		using var scope = services.CreateScope();
		var sp = scope.ServiceProvider;

		var registry = sp.GetRequiredService<IAuthorizationRoleRegistry>();
		DomainModel.Instance.Initialize(sp);

		var analyzer = DomainAnalyzerProvider.CreateAnalyzer(registry, sp, options);
		var report = analyzer.AnalyzeAll();
		var summary = report.GetSummary();

		return summary.Passed ? null : report;
	}
}
