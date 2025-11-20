namespace Cirreum.Conductor.Configuration;

using Cirreum.Conductor.Intercepts;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Builder for configuring Conductor options during service registration.
/// </summary>
public class ConductorOptionsBuilder {

	private IConfiguration? _configuration;
	private string _configurationSection = ConductorSettings.SectionName;
	private ConductorSettings? _settings;
	private readonly List<Action<ConductorBuilder>> _interceptConfigurations = [];

	/// <summary>
	/// Binds Conductor settings from configuration.
	/// </summary>
	/// <param name="configuration">The configuration instance.</param>
	/// <param name="sectionName">The configuration section name. Defaults to "Conductor".</param>
	/// <returns>The builder for method chaining.</returns>
	public ConductorOptionsBuilder BindConfiguration(
		IConfiguration configuration,
		string sectionName = ConductorSettings.SectionName) {

		ArgumentNullException.ThrowIfNull(configuration);
		_configuration = configuration;
		_configurationSection = sectionName;
		return this;
	}

	/// <summary>
	/// Manually configures Conductor settings, bypassing appsettings.json.
	/// </summary>
	/// <param name="configure">Action to configure settings.</param>
	/// <returns>The builder for method chaining.</returns>
	public ConductorOptionsBuilder ConfigureSettings(Action<ConductorSettings> configure) {
		ArgumentNullException.ThrowIfNull(configure);
		_settings ??= new ConductorSettings();
		configure(_settings);
		return this;
	}

	/// <summary>
	/// Adds custom intercepts to the Conductor pipeline.
	/// Intercepts added here will be inserted AFTER authorization but BEFORE performance monitoring.
	/// </summary>
	/// <param name="configure">Action to configure intercepts.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <remarks>
	/// This extensibility point allows you to add custom cross-cutting concerns that should run:
	/// <list type="bullet">
	/// <item>AFTER validation and authorization (security is enforced)</item>
	/// <item>BEFORE performance monitoring (your intercept execution is measured)</item>
	/// <item>BEFORE query caching (your intercept can affect what gets cached)</item>
	/// </list>
	/// Common use cases include: tenant isolation, audit logging, request transformation, 
	/// business-specific validation, or custom telemetry.
	/// </remarks>
	public ConductorOptionsBuilder AddCustomIntercepts(Action<ConductorBuilder> configure) {
		ArgumentNullException.ThrowIfNull(configure);
		_interceptConfigurations.Add(configure);
		return this;
	}

	internal ConductorSettings GetSettings() {
		// Priority 1: Manual settings (explicit ConfigureSettings call)
		if (_settings is not null) {
			return _settings;
		}

		// Priority 2: Configuration binding (explicit BindConfiguration call)
		if (_configuration is not null) {
			_settings = new ConductorSettings();
			_configuration.GetSection(_configurationSection).Bind(_settings);
			return _settings;
		}

		// Priority 3: Defaults (no configuration provided)
		return new ConductorSettings();
	}

	internal void ConfigureIntercepts(ConductorBuilder builder) {
		// Core intercepts in fixed order
		builder
			.AddOpenIntercept(typeof(Validation<,>))
			.AddOpenIntercept(typeof(Authorization<,>));

		// Custom intercepts (extensibility point)
		foreach (var config in _interceptConfigurations) {
			config(builder);
		}

		// Wrapping and pre-emptive intercepts
		builder
			.AddOpenIntercept(typeof(HandlerPerformance<,>))
			.AddOpenIntercept(typeof(QueryCaching<,>));
	}
}