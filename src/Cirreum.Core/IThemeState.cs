namespace Cirreum;

/// <summary>
/// Represents theme-related state for the current scope, including:
/// <list type="bullet">
/// <item><description><see cref="Mode"/> / <see cref="AppliedMode"/> for light/dark/auto selection.</description></item>
/// <item><description><see cref="Scheme"/> for the active color palette (e.g. "default", "aspire").</description></item>
/// </list>
/// </summary>
public interface IThemeState : IScopedNotificationState {

	// ---------------------------------------------------------------------
	// THEME MODE (light / dark / auto)
	// ---------------------------------------------------------------------

	/// <summary>
	/// Gets the selected theme mode value.
	/// </summary>
	/// <remarks>
	/// Expected values are typically <c>"light"</c>, <c>"dark"</c> or <c>"auto"</c>.
	/// Implementations may support additional modes, but UI components should be
	/// prepared to handle at least these three.
	/// </remarks>
	string Mode { get; }

	/// <summary>
	/// Sets the selected theme mode and notifies subscribers.
	/// </summary>
	/// <param name="value">
	/// The theme mode to select (for example <c>"light"</c>, <c>"dark"</c>, or <c>"auto"</c>).
	/// </param>
	void SetMode(string value);

	/// <summary>
	/// Gets the currently applied theme mode.
	/// </summary>
	/// <remarks>
	/// This is the effective mode used by the UI, typically <c>"light"</c> or <c>"dark"</c>.
	/// When <see cref="Mode"/> is <c>"auto"</c>, the implementation usually resolves this
	/// based on system or browser preferences and exposes the resolved value here.
	/// </remarks>
	string AppliedMode { get; }

	/// <summary>
	/// Sets the currently applied theme mode.
	/// </summary>
	/// <param name="value">
	/// The effective mode to apply, usually <c>"light"</c> or <c>"dark"</c>.
	/// </param>
	void SetAppliedMode(string value);

	/// <summary>
	/// Gets the icon name for the Auto mode option.
	/// </summary>
	string AutoModeIcon { get; }

	/// <summary>
	/// Gets the icon name for the Light mode option.
	/// </summary>
	string LightModeIcon { get; }

	/// <summary>
	/// Gets the icon name for the Dark mode option.
	/// </summary>
	string DarkModeIcon { get; }

	/// <summary>
	/// Gets the icon name representing the currently selected mode.
	/// </summary>
	/// <remarks>
	/// Typically returns one of <see cref="AutoModeIcon"/>, <see cref="LightModeIcon"/>,
	/// or <see cref="DarkModeIcon"/> depending on <see cref="Mode"/>.
	/// </remarks>
	string ModeIcon { get; }

	// ---------------------------------------------------------------------
	// COLOR SCHEME (palette / brand theme)
	// ---------------------------------------------------------------------

	/// <summary>
	/// Gets the current theme color scheme identifier.
	/// </summary>
	/// <remarks>
	/// Examples include <c>"default"</c>, <c>"aspire"</c>, <c>"excel"</c>,
	/// <c>"office"</c>, <c>"outlook"</c>, or <c>"windows"</c>. Applications may
	/// define additional custom schemes as needed.
	/// </remarks>
	string Scheme { get; }

	/// <summary>
	/// Sets the current theme color scheme and notifies subscribers.
	/// </summary>
	/// <param name="value">
	/// The scheme identifier to set. This should match a known scheme in the
	/// consuming UI layer or fall back to a sensible default.
	/// </param>
	void SetScheme(string value);

}