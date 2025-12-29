namespace Cirreum.Authorization.Documentation;

public interface IAuthorizationDocumenter {
	/// <summary>
	/// Generates a comprehensive Markdown documentation of the authorization system.
	/// </summary>
	/// <returns>A string containing markdown documentation.</returns>
	string GenerateMarkdown();

	/// <summary>
	/// Generates a CSV export of the authorization system.
	/// </summary>
	/// <returns>A string containing CSV data.</returns>
	string GenerateCsv();

	/// <summary>
	/// Renders a complete HTML page visualizing the authorization system.
	/// </summary>
	/// <returns>A string containing HTML.</returns>
	string RenderHtmlPage();
}