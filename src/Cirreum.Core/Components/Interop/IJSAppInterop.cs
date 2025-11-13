namespace Cirreum.Components.Interop;

/// <summary>
/// Interface for JavaScript interop operations in the application.
/// </summary>
public interface IJSAppInterop {

	/// <summary>
	/// Initializes the JavaScript interop functionality.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	ValueTask InitializeAsync();

	/// <summary>
	/// Gets the browser's internationalization format information.
	/// </summary>
	/// <returns>An object containing the browser's locale, timezone, and formatting preferences.</returns>
	ResolvedDateTimeFormatOptions GetInternationalFormats();

	/// <summary>
	/// Gets the current local time as a string.
	/// </summary>
	/// <returns>A string representation of the current local time.</returns>
	string GetCurrentLocalTime();

	/// <summary>
	/// Gets the current UTC time as a string.
	/// </summary>
	/// <returns>A string representation of the current UTC time.</returns>
	string GetCurrentUtcTime();

	/// <summary>
	/// Determines if daylight saving time is currently in effect.
	/// </summary>
	/// <returns>'Yes' if DST is in effect, 'No' otherwise.</returns>
	string IsDaylightSavingTime();

	/// <summary>
	/// Gets samples of different date/time formats.
	/// </summary>
	/// <returns>An object containing various formatted date/time strings.</returns>
	FormattedSamples GetFormattedSamples();

	/// <summary>
	/// Checks if the browser supports the Intl.DateTimeFormat timeZone feature.
	/// </summary>
	/// <returns>True if timeZone is supported, false otherwise.</returns>
	bool HasTimeZoneSupport();

	/// <summary>
	/// Checks if the browser supports the Date.getTimezoneOffset method.
	/// </summary>
	/// <returns>True if getTimezoneOffset is supported, false otherwise.</returns>
	bool HasOffsetSupport();

	/// <summary>
	/// Gets the browser's user agent string.
	/// </summary>
	/// <returns>The user agent string.</returns>
	string GetUserAgent();

	TResult Invoke<TResult>(string identifier, params object?[]? args);
	ValueTask<TResult> InvokeAsync<TResult>(string identifier, CancellationToken token, params object?[]? args);
	ValueTask<TResult> InvokeAsync<TResult>(string identifier, params object?[]? args);
	void InvokeVoid(string identifier, params object?[]? args);
	ValueTask InvokeVoidAsync(string identifier, CancellationToken token, params object?[]? args);
	ValueTask InvokeVoidAsync(string identifier, params object?[]? args);

}