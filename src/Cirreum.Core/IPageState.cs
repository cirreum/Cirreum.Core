namespace Cirreum;

/// <summary>
/// Defines the page display settings for the application.
/// </summary>
/// <remarks>
/// <para>
/// Manages settings that control how pages are presented to users, particularly
/// in the browser title bar.
/// </para>
/// </remarks>
public interface IPageState : IScopedNotificationState {

	/// <summary>
	/// Gets the App Name.
	/// </summary>
	string AppName { get; }

	/// <summary>
	/// Sets the <see cref="AppName"/> for the application.
	/// </summary>
	/// <param name="value">The name of the application.</param>
	void SetAppName(string value);

	/// <summary>
	/// Gets the default page title prefix for all pages.
	/// </summary>
	/// <remarks>
	/// This text appears before the main page title, separated by the <see cref="PageTitleSeparator"/>.
	/// </remarks>
	string PageTitlePrefix { get; }

	/// <summary>
	/// Sets the <see cref="PageTitlePrefix"/> for all pages.
	/// </summary>
	/// <param name="value">The text to use as the prefix for page titles.</param>
	void SetPageTitlePrefix(string value);

	/// <summary>
	/// Gets the default page title suffix for all pages.
	/// </summary>
	/// <remarks>
	/// This text appears after the main page title, separated by the <see cref="PageTitleSeparator"/>.
	/// Often includes the application name.
	/// </remarks>
	string PageTitleSuffix { get; }

	/// <summary>
	/// Sets the <see cref="PageTitleSuffix"/> for all pages.
	/// </summary>
	/// <param name="value">The text to use as the suffix for page titles.</param>
	void SetPageTitleSuffix(string value);

	/// <summary>
	/// Gets the default page title separator for all pages.
	/// </summary>
	/// <remarks>
	/// This character or string separates the main page title from the 
	/// <see cref="PageTitlePrefix"/> and <see cref="PageTitleSuffix"/>.
	/// Common values include "|", "-", or "•".
	/// </remarks>
	string PageTitleSeparator { get; }

	/// <summary>
	/// Sets the <see cref="PageTitleSeparator"/> for all pages.
	/// </summary>
	/// <param name="value">The character or string to use as the separator in page titles.</param>
	void SetPageTitleSeparator(string value);

	/// <summary>
	/// Gets whether the application is currently running in Stand-Alone mode.
	/// </summary>
	/// <remarks>
	/// This should be based on the display-mode media query. If it is
	/// stand-alone, then it can support the Progressive Web Application (PWA)
	/// features. When true, page titles may be simplified to improve the PWA experience.
	/// </remarks>
	bool IsStandAlone { get; }

	/// <summary>
	/// Sets the <see cref="IsStandAlone"/> status of the application.
	/// </summary>
	/// <param name="value">Whether the application is running in stand-alone mode.</param>
	/// <remarks>
	/// This affects how page titles are rendered and may enable PWA-specific features.
	/// </remarks>
	void SetIsStandAlone(bool value);

}