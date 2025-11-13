namespace Cirreum;

/// <summary>
/// Represents theme-related state
/// </summary>
public interface IThemeState : IScopedNotificationState {
	/// <summary>
	/// Gets the applied theme (light or dark).
	/// </summary>
	string AppliedTheme { get; }
	/// <summary>
	/// Sets the applied theme (light or dark).
	/// </summary>
	/// <param name="value">The theme value (light or dark)</param>
	void SetAppliedTheme(string value);
	/// <summary>
	/// The name of the Icon to use for the Auto Theme option.
	/// </summary>
	string AutoThemeIcon { get; }
	/// <summary>
	/// The name of the Icon to use for the Light Theme option.
	/// </summary>
	string LightThemeIcon { get; }
	/// <summary>
	/// The name of the Icon to use for the Dark Theme option.
	/// </summary>
	string DarkThemeIcon { get; }
	/// <summary>
	/// Gets the icon name for the selected theme.
	/// </summary>
	/// <remarks>
	/// Should support: auto, light or dark
	/// </remarks>
	string SelectedThemeIcon { get; }
	/// <summary>
	/// Gets the selected theme name: auto, light or dark.
	/// </summary>
	string SelectedTheme { get; }
	/// <summary>
	/// Sets the selected theme name: auto, light or dark and notifies subscribers.
	/// </summary>
	/// <param name="value">The theme name to set.</param>
	void SetSelectedTheme(string value);
}