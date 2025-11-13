namespace Cirreum.Security;

using System.Collections.Generic;

/// <summary>
/// Defines a contract for a builder that offers the ability
/// to configure and build a content-security-policy.
/// </summary>
public interface ICspBuilder {
	/// <summary>
	/// Based on the directive and sources configured, generates a
	/// content-security-policy string.
	/// </summary>
	/// <returns>A <see cref="string"/> containing the Csp value.</returns>
	string Build();
	/// <summary>
	/// Determines if a given directive exists within the provider.
	/// </summary>
	/// <param name="directive">The directive to evaluate for existence.</param>
	/// <returns><see langword="true"/> if the directive exists; otherwise <see langword="false"/></returns>
	bool ContainsDirective(string directive);
	/// <summary>
	/// Determines if a given source exists for a given directive within the provider.
	/// </summary>
	/// <param name="directive">The directive to evaluate.</param>
	/// <param name="source">The source to evaluate for existence.</param>
	/// <returns><see langword="true"/> if the directive and source exists; otherwise <see langword="false"/></returns>
	bool ContainsSource(string directive, string source);
	/// <summary>
	/// Attempts to add the specified source if it doesn't already exist.
	/// </summary>
	/// <param name="directive">The directive to add the source to.</param>
	/// <param name="source">The source to add.</param>
	/// <returns>The <see cref="ICspBuilder"/> instance.</returns>
	ICspBuilder AddSource(string directive, string source);
	/// <summary>
	/// Attempts to remove the specified source if it exists.
	/// </summary>
	/// <param name="directive">The directive to remove the source from.</param>
	/// <param name="source">The source to remove.</param>
	void RemoveSource(string directive, string source);
	/// <summary>
	/// Attempts to get the list of sources for a given directive.
	/// </summary>
	/// <param name="directive">The directive to evaluate.</param>
	/// <returns>The list of source, if the directive was found; otherwise an empty list.</returns>
	List<string> GetSources(string directive);
	/// <summary>
	/// Removes the directive from the policy.
	/// </summary>
	/// <param name="directive">The directive to remove.</param>
	void ClearDirective(string directive);
	/// <summary>
	/// Removes all directives from the provider.
	/// </summary>
	void Reset();
}