namespace Cirreum;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Validates internet domain names according to standard formatting rules.
/// </summary>
/// <remarks>
/// <para>
/// Domains must meet the following criteria:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Not include protocols/schemes (e.g., no "http://").</description>
/// </item>
/// <item>
/// <description>Match standard patterns like "example.com" or "sub.example.com".</description>
/// </item>
/// <item>
/// <description>Not contain invalid characters.</description>
/// </item>
/// <item>
/// <description>Be properly formatted after trimming whitespace.</description>
/// </item>
/// </list>
/// </remarks>
public static partial class InternetDomainValidator {

	[GeneratedRegex(@"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$")]
	private static partial Regex DomainRegex();

	/// <summary>
	/// Validates if a single domain string is valid.
	/// </summary>
	/// <param name="domain">The domain to validate.</param>
	/// <returns>True if the domain is valid, otherwise false.</returns>
	public static bool IsValidDomain(string domain) {

		if (string.IsNullOrWhiteSpace(domain)) {
			return false;
		}

		if (domain.Equals("localhost", StringComparison.OrdinalIgnoreCase)) {
			return true;
		}

		var trimmedDomain = domain.Trim();

		// Check if it contains protocols or matches the domain regex
		return !trimmedDomain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
			   !trimmedDomain.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
			   DomainRegex().IsMatch(trimmedDomain);

	}

	/// <summary>
	/// Cleans a list of domains by removing invalid ones.
	/// </summary>
	/// <param name="domains">The list of domains to validate and clean.</param>
	/// <returns>A list of valid domains.</returns>
	public static List<string> CleanDomains(List<string> domains) {
		if (domains == null || domains.Count == 0) {
			return [];
		}

		var validDomains = new List<string>();

		foreach (var domain in domains) {
			if (IsValidDomain(domain)) {
				validDomains.Add(domain.Trim());
			}
		}

		return validDomains;

	}

}