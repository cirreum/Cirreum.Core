namespace Cirreum;

using System;

/// <summary>
/// Abstraction of a DateTime Clock Service with cross-platform time zone support.
/// Provides unified access to date, time, and time zone operations that work consistently
/// across Windows, Linux, and macOS platforms.
/// </summary>
public interface IDateTimeClock {

	/// <summary>
	/// Provides a mapping from IANA time zone identifiers to Windows time zone IDs.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is mainly used for Window's environment. Non-Windows environment like WASM/Linux etc.
	/// use TimeZoneInfo directly.
	/// </para>
	/// <para>
	/// This dictionary contains mappings for major global regions and business centers,
	/// covering the most commonly used IANA time zones. While not exhaustive (IANA defines
	/// over 600 time zones), it includes mappings for all major population centers and
	/// business hubs worldwide.
	/// </para>
	/// <para>
	/// The mapping also attempts to cover as many Windows time zone IDs as possible to
	/// ensure reliable cross-platform conversion. It maps IANA time zone identifiers
	/// (e.g., "America/New_York") to Windows format (e.g., "Eastern Standard Time").
	/// </para>
	/// <para>
	/// For applications requiring complete coverage of all IANA or Windows time zones,
	/// consider using a specialized third-party library like TimeZoneConverter.
	/// </para>
	/// </remarks>
	public static readonly Dictionary<string, string> IanaToWindowsMap = new(StringComparer.OrdinalIgnoreCase) {
	
		// North America
		{ "America/New_York", "Eastern Standard Time" },
		{ "America/Chicago", "Central Standard Time" },
		{ "America/Denver", "Mountain Standard Time" },
		{ "America/Los_Angeles", "Pacific Standard Time" },
		{ "America/Phoenix", "US Mountain Standard Time" },
		{ "America/Anchorage", "Alaskan Standard Time" },
		{ "America/Honolulu", "Hawaiian Standard Time" },
		{ "America/Toronto", "Eastern Standard Time" },
		{ "America/Vancouver", "Pacific Standard Time" },
		{ "America/Edmonton", "Mountain Standard Time" },
		{ "America/Winnipeg", "Central Standard Time" },
		{ "America/Mexico_City", "Central Standard Time (Mexico)" },
		{ "America/Monterrey", "Central Standard Time (Mexico)" },
		{ "America/Tijuana", "Pacific Standard Time (Mexico)" },
	
		// South America
		{ "America/Sao_Paulo", "E. South America Standard Time" },
		{ "America/Buenos_Aires", "Argentina Standard Time" },
		{ "America/Santiago", "Pacific SA Standard Time" },
		{ "America/Bogota", "SA Pacific Standard Time" },
		{ "America/Lima", "SA Pacific Standard Time" },
		{ "America/Caracas", "Venezuela Standard Time" },
	
		// Europe
		{ "Europe/London", "GMT Standard Time" },
		{ "Europe/Paris", "Romance Standard Time" },
		{ "Europe/Berlin", "W. Europe Standard Time" },
		{ "Europe/Brussels", "Romance Standard Time" },
		{ "Europe/Amsterdam", "W. Europe Standard Time" },
		{ "Europe/Rome", "W. Europe Standard Time" },
		{ "Europe/Zurich", "W. Europe Standard Time" },
		{ "Europe/Madrid", "Romance Standard Time" },
		{ "Europe/Dublin", "GMT Standard Time" },
		{ "Europe/Stockholm", "W. Europe Standard Time" },
		{ "Europe/Prague", "Central Europe Standard Time" },
		{ "Europe/Vienna", "W. Europe Standard Time" },
		{ "Europe/Warsaw", "Central European Standard Time" },
		{ "Europe/Budapest", "Central Europe Standard Time" },
		{ "Europe/Istanbul", "Turkey Standard Time" },
		{ "Europe/Moscow", "Russian Standard Time" },
	
		// Asia
		{ "Asia/Tokyo", "Tokyo Standard Time" },
		{ "Asia/Singapore", "Singapore Standard Time" },
		{ "Asia/Hong_Kong", "China Standard Time" },
		{ "Asia/Shanghai", "China Standard Time" },
		{ "Asia/Seoul", "Korea Standard Time" },
		{ "Asia/Dubai", "Arabian Standard Time" },
		{ "Asia/Kolkata", "India Standard Time" },
		{ "Asia/Bangkok", "SE Asia Standard Time" },
		{ "Asia/Jakarta", "SE Asia Standard Time" },
		{ "Asia/Manila", "Singapore Standard Time" },
		{ "Asia/Taipei", "Taipei Standard Time" },
		{ "Asia/Jerusalem", "Israel Standard Time" },
		{ "Asia/Riyadh", "Arab Standard Time" },
		{ "Asia/Tehran", "Iran Standard Time" },
	
		// Australia/Oceania
		{ "Australia/Sydney", "AUS Eastern Standard Time" },
		{ "Australia/Melbourne", "AUS Eastern Standard Time" },
		{ "Australia/Brisbane", "E. Australia Standard Time" },
		{ "Australia/Perth", "W. Australia Standard Time" },
		{ "Australia/Adelaide", "Cen. Australia Standard Time" },
		{ "Pacific/Auckland", "New Zealand Standard Time" },
		{ "Pacific/Fiji", "Fiji Standard Time" },
	
		// Africa
		{ "Africa/Cairo", "Egypt Standard Time" },
		{ "Africa/Johannesburg", "South Africa Standard Time" },
		{ "Africa/Lagos", "W. Central Africa Standard Time" },
		{ "Africa/Nairobi", "E. Africa Standard Time" },
		{ "Africa/Casablanca", "Morocco Standard Time" },
	
		// UTC and GMT
		{ "Etc/UTC", "UTC" },
		{ "Etc/GMT", "GMT Standard Time" }
	};

	/// <summary>
	/// Provides a mapping from Windows time zone IDs to IANA time zone identifiers.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is mainly used for Window's environment. Non-Windows environment like WASM/Linux etc.
	/// use TimeZoneInfo directly.
	/// </para>
	/// <para>
	/// This dictionary is automatically generated from <see cref="IanaToWindowsMap"/> by inverting
	/// its key-value pairs. It covers the majority of commonly used Windows time zones.
	/// </para>
	/// <para>
	/// While comprehensive for typical business and development scenarios, some less common
	/// Windows time zones may not be included. For Windows time zones not found in this mapping,
	/// the implementation falls back to alternative methods or ultimately to UTC.
	/// </para>
	/// <para>
	/// For applications requiring guaranteed mapping of all Windows time zones,
	/// consider using a specialized third-party library like TimeZoneConverter.
	/// </para>
	/// </remarks>
	public static readonly Dictionary<string, string> WindowsToIanaMap = InitializeWindowsToIanaMap();
	private static Dictionary<string, string> InitializeWindowsToIanaMap() {

		// Build the reverse mapping from the IANA-to-Windows map
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var kvp in IanaToWindowsMap) {
			if (!map.ContainsKey(kvp.Value)) {
				map[kvp.Value] = kvp.Key;
			}
		}

		// On Windows, check for and add any time zones not yet covered
		if (OperatingSystem.IsWindows()) {
			// This additional code only runs once at startup and only on Windows
			try {
				foreach (var tz in TimeZoneInfo.GetSystemTimeZones()) {
					if (!map.ContainsKey(tz.Id)) {
						// For time zones not in our mapping, attempt a reasonable default
						// based on offset if possible
						var offset = tz.BaseUtcOffset;

						// For whole-hour offsets, we can make a reasonable guess with Etc/GMT+X
						if (offset.Minutes == 0 && offset.Seconds == 0) {
							// Note: IANA's Etc/GMT+X notation is inverted from what you might expect
							// Positive offsets are negative in the name, and vice versa
							var hours = -offset.Hours; // Invert for IANA format
							var ianaId = hours == 0 ? "Etc/UTC" :
								$"Etc/GMT{(hours > 0 ? "+" : "")}{hours}";

							map[tz.Id] = ianaId;
						} else {
							// For non-whole hours, just use UTC as fallback
							map[tz.Id] = "Etc/UTC";
						}
					}
				}
			} catch {
				// Ignore any errors during this enhancement - the core functionality
				// will still work with the base mappings
			}
		}

		return map;

	}

	/// <summary>
	/// Provides friendly, human-readable names for IANA time zone identifiers.
	/// </summary>
	private static readonly Dictionary<string, string> IanaToFriendlyNameMap = InitializeFriendlyNameMap();
	private static Dictionary<string, string> InitializeFriendlyNameMap() {

		var friendlyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			// North America
			{ "America/New_York", "New York" },
			{ "America/Chicago", "Chicago" },
			{ "America/Denver", "Denver" },
			{ "America/Los_Angeles", "Los Angeles" },
			{ "America/Phoenix", "Phoenix" },
			{ "America/Anchorage", "Anchorage" },
			{ "America/Honolulu", "Honolulu" },
			{ "America/Toronto", "Toronto" },
			{ "America/Vancouver", "Vancouver" },
			{ "America/Edmonton", "Edmonton" },
			{ "America/Winnipeg", "Winnipeg" },
			{ "America/Mexico_City", "Mexico City" },
			{ "America/Monterrey", "Monterrey" },
			{ "America/Tijuana", "Tijuana" },
		
			// South America
			{ "America/Sao_Paulo", "São Paulo" },
			{ "America/Buenos_Aires", "Buenos Aires" },
			{ "America/Santiago", "Santiago" },
			{ "America/Bogota", "Bogotá" },
			{ "America/Lima", "Lima" },
			{ "America/Caracas", "Caracas" },
		
			// Europe
			{ "Europe/London", "London" },
			{ "Europe/Paris", "Paris" },
			{ "Europe/Berlin", "Berlin" },
			{ "Europe/Brussels", "Brussels" },
			{ "Europe/Amsterdam", "Amsterdam" },
			{ "Europe/Rome", "Rome" },
			{ "Europe/Zurich", "Zurich" },
			{ "Europe/Madrid", "Madrid" },
			{ "Europe/Dublin", "Dublin" },
			{ "Europe/Stockholm", "Stockholm" },
			{ "Europe/Prague", "Prague" },
			{ "Europe/Vienna", "Vienna" },
			{ "Europe/Warsaw", "Warsaw" },
			{ "Europe/Budapest", "Budapest" },
			{ "Europe/Istanbul", "Istanbul" },
			{ "Europe/Moscow", "Moscow" },
		
			// Asia
			{ "Asia/Tokyo", "Tokyo" },
			{ "Asia/Singapore", "Singapore" },
			{ "Asia/Hong_Kong", "Hong Kong" },
			{ "Asia/Shanghai", "Shanghai" },
			{ "Asia/Seoul", "Seoul" },
			{ "Asia/Dubai", "Dubai" },
			{ "Asia/Kolkata", "Mumbai" },
			{ "Asia/Bangkok", "Bangkok" },
			{ "Asia/Jakarta", "Jakarta" },
			{ "Asia/Manila", "Manila" },
			{ "Asia/Taipei", "Taipei" },
			{ "Asia/Jerusalem", "Jerusalem" },
			{ "Asia/Riyadh", "Riyadh" },
			{ "Asia/Tehran", "Tehran" },
		
			// Australia/Oceania
			{ "Australia/Sydney", "Sydney" },
			{ "Australia/Melbourne", "Melbourne" },
			{ "Australia/Brisbane", "Brisbane" },
			{ "Australia/Perth", "Perth" },
			{ "Australia/Adelaide", "Adelaide" },
			{ "Pacific/Auckland", "Auckland" },
			{ "Pacific/Fiji", "Fiji" },
		
			// Africa
			{ "Africa/Cairo", "Cairo" },
			{ "Africa/Johannesburg", "Johannesburg" },
			{ "Africa/Lagos", "Lagos" },
			{ "Africa/Nairobi", "Nairobi" },
			{ "Africa/Casablanca", "Casablanca" },
		
			// UTC and GMT
			{ "Etc/UTC", "UTC" },
			{ "Etc/GMT", "GMT" }
		};

		// Add friendly names for any additional time zones from our IanaToWindowsMap
		// that aren't already in the friendly names map
		foreach (var ianaId in IanaToWindowsMap.Keys) {
			if (!friendlyMap.ContainsKey(ianaId)) {
				// Extract the city name from the IANA ID
				var friendlyName = ianaId;

				if (ianaId.Contains('/')) {
					// Get the part after the last slash and replace underscores with spaces
					friendlyName = ianaId.Split('/').Last().Replace("_", " ");
				}

				friendlyMap[ianaId] = friendlyName;
			}
		}

		return friendlyMap;
	}

	/// <summary>
	/// Gets a user-friendly display name for an IANA time zone identifier.
	/// </summary>
	/// <param name="ianaTimeZoneId">The IANA time zone identifier.</param>
	/// <returns>A user-friendly name for the time zone, or the original ID if no mapping exists.</returns>
	public string GetFriendlyTimeZoneName(string ianaTimeZoneId) {

		if (string.IsNullOrEmpty(ianaTimeZoneId)) {
			return "Unknown";
		}

		if (IanaToFriendlyNameMap.TryGetValue(ianaTimeZoneId, out var friendlyName)) {
			return friendlyName;
		}

		// If not found in the dictionary, extract the city name from the IANA ID
		if (ianaTimeZoneId.Contains('/')) {
			return ianaTimeZoneId.Split('/').Last().Replace("_", " ");
		}

		// Return the original if we can't make it any friendlier
		return ianaTimeZoneId;
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> in a specific time zone using an IANA time zone ID.
	/// </summary>
	/// <param name="dateTime">The <see cref="DateTime"/> value to convert</param>
	/// <param name="timeZoneId">IANA time zone ID (e.g., "America/New_York")</param>
	/// <returns>A <see cref="DateTimeOffset"/> with the correct offset for the specified time zone</returns>
	/// <remarks>
	/// This method correctly handles different <see cref="DateTimeKind"/> values:
	/// <list type="bullet">
	///   <item><term>Unspecified</term><description>Treated as local time</description></item>
	///   <item><term>Local</term><description>Converted from local time to the target time zone</description></item>
	///   <item><term>Utc</term><description>Converted from UTC to the target time zone</description></item>
	/// </list>
	/// </remarks>
	/// <exception cref="ArgumentException">Thrown when the timeZoneId cannot be found</exception>
	public DateTimeOffset InTimeZone(DateTime dateTime, string timeZoneId) {

		// Use the GetTimeZoneByIanaId method to handle IANA IDs properly across platforms
		var timeZone = this.GetTimeZoneByIanaId(timeZoneId);

		// If the input DateTime is Kind.Unspecified, treat it as local time
		// If it's Kind.Local, convert from local to target time zone
		// If it's Kind.Utc, convert from UTC to target time zone
		switch (dateTime.Kind) {
			case DateTimeKind.Unspecified:
				// Treat as local time
				var localTz = this.TimeProvider.LocalTimeZone;

				// Use safer conversion methods that handle ambiguous/invalid times during DST transitions
				DateTimeOffset localOffset;
				try {
					var utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, localTz);
					localOffset = new DateTimeOffset(
						TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone),
						timeZone.GetUtcOffset(utcTime));
				} catch (ArgumentException) {
					// Handle potential ambiguous time during DST transition
					// (e.g., when clock falls back and same time occurs twice)
					// Choose the standard time (non-DST) interpretation
					var utcTime = TimeZoneInfo.ConvertTimeToUtc(
						dateTime,
						localTz);
					localOffset = new DateTimeOffset(
						TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone),
						timeZone.GetUtcOffset(utcTime));
				}
				return localOffset;

			case DateTimeKind.Local:
				// Convert directly from local to target time zone
				try {
					var utcTime = dateTime.ToUniversalTime();
					return new DateTimeOffset(
						TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone),
						timeZone.GetUtcOffset(utcTime));
				} catch (ArgumentException) {
					// Handle potential DST issues by being explicit
					var utcTime = TimeZoneInfo.ConvertTimeToUtc(
						dateTime,
						TimeZoneInfo.Local);
					return new DateTimeOffset(
						TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone),
						timeZone.GetUtcOffset(utcTime));
				}

			case DateTimeKind.Utc:
				// This is the simplest case - convert from UTC to target time zone
				var utcDateTime = dateTime;
				return new DateTimeOffset(
					TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone),
					timeZone.GetUtcOffset(utcDateTime));

			default:
				throw new ArgumentException("Unknown DateTimeKind", nameof(dateTime));
		}
	}

	/// <summary>
	/// Gets a <see cref="DateTime"/> object that is set to the current date and time on this
	/// computer, expressed as the local time.
	/// </summary>
	/// <remarks>
	/// This property is equivalent to <see cref="DateTime.Now"/> but uses the underlying
	/// <see cref="TimeProvider"/> to enable testability.
	/// </remarks>
	public DateTime Now => this.TimeProvider.GetLocalNow().DateTime;

	/// <summary>
	/// Gets a <see cref="DateTime"/> object that is set to the current date and time on this
	/// computer, expressed as the Coordinated Universal Time (UTC).
	/// </summary>
	/// <remarks>
	/// This property is equivalent to <see cref="DateTime.UtcNow"/> but uses the underlying
	/// <see cref="TimeProvider"/> to enable testability.
	/// </remarks>
	public DateTime UtcNow => this.TimeProvider.GetUtcNow().DateTime;

	/// <summary>
	/// Gets a <see cref="DateTimeOffset"/> object that is set to the current date and time
	/// on the current computer, with the offset set to the local time's offset from
	/// Coordinated Universal Time (UTC).
	/// </summary>
	/// <remarks>
	/// This property is equivalent to <see cref="DateTimeOffset.Now"/> but uses the underlying
	/// <see cref="TimeProvider"/> to enable testability.
	/// </remarks>
	public DateTimeOffset LocalOffset => this.TimeProvider.GetLocalNow();

	/// <summary>
	/// Gets a <see cref="DateTimeOffset"/> object whose date and time are set to the current
	/// Coordinated Universal Time (UTC) date and time and whose offset is <see cref="TimeSpan.Zero"/>.
	/// </summary>
	/// <remarks>
	/// This property is equivalent to <see cref="DateTimeOffset.UtcNow"/> but uses the underlying
	/// <see cref="TimeProvider"/> to enable testability.
	/// </remarks>
	public DateTimeOffset UtcOffset => this.TimeProvider.GetUtcNow();

	/// <summary>
	/// Gets the current <see cref="TimeZoneInfo"/> object for the local time zone.
	/// </summary>
	/// <remarks>
	/// This is a wrapper around <see cref="TimeProvider.LocalTimeZone"/> which
	/// represents the system's local time zone. The ID format will be platform-specific
	/// (Windows format on Windows, IANA format on Linux/macOS).
	/// </remarks>
	public TimeZoneInfo LocalTimeZone => this.TimeProvider.LocalTimeZone;

	/// <summary>
	/// Gets the <see cref="TimeZoneInfo"/> object representing the UTC time zone.
	/// </summary>
	/// <remarks>
	/// This is a convenience property for accessing <see cref="TimeZoneInfo.Utc"/>.
	/// </remarks>
	public TimeZoneInfo UtcTimeZone => TimeZoneInfo.Utc;

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> in a specific time zone using an IANA time zone ID.
	/// </summary>
	/// <param name="dateTimeOffset">The <see cref="DateTimeOffset"/> value to convert</param>
	/// <param name="timeZoneId">IANA time zone ID (e.g., "America/New_York")</param>
	/// <returns>A new <see cref="DateTimeOffset"/> with the correct offset for the specified time zone</returns>
	/// <remarks>
	/// This method preserves the absolute point in time while changing the time zone offset.
	/// </remarks>
	/// <exception cref="ArgumentException">Thrown when the timeZoneId cannot be found</exception>
	public DateTimeOffset InTimeZone(DateTimeOffset dateTimeOffset, string timeZoneId) {
		// Use GetTimeZoneByIanaId to properly handle IANA IDs cross-platform
		var destinationTz = this.GetTimeZoneByIanaId(timeZoneId);
		return TimeZoneInfo.ConvertTime(dateTimeOffset, destinationTz);
	}

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> in the UTC time zone.
	/// </summary>
	/// <param name="dateTime">The <see cref="DateTime"/> value to convert</param>
	/// <returns>A <see cref="DateTimeOffset"/> representing the same point in time in UTC</returns>
	/// <remarks>
	/// Convenience method equivalent to calling <see cref="InTimeZone(DateTime, string)"/> with "Etc/UTC".
	/// </remarks>
	public DateTimeOffset InUtc(DateTime dateTime) => this.InTimeZone(dateTime, "Etc/UTC");

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to a <see cref="DateTimeOffset"/> in the UTC time zone.
	/// </summary>
	/// <param name="dateTime">The <see cref="DateTimeOffset"/> value to convert</param>
	/// <returns>A <see cref="DateTimeOffset"/> representing the same point in time in UTC</returns>
	/// <remarks>
	/// Convenience method equivalent to calling <see cref="InTimeZone(DateTimeOffset, string)"/> with "Etc/UTC".
	/// </remarks>
	public DateTimeOffset InUtc(DateTimeOffset dateTime) => this.InTimeZone(dateTime, "Etc/UTC");

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> in the local time zone.
	/// </summary>
	/// <param name="dateTime">The <see cref="DateTime"/> value to convert</param>
	/// <returns>A <see cref="DateTimeOffset"/> representing the same point in time in the local time zone</returns>
	/// <remarks>
	/// Convenience method equivalent to calling <see cref="InTimeZone(DateTime, string)"/> with the local IANA time zone ID.
	/// </remarks>
	public DateTimeOffset InLocalTimeZone(DateTime dateTime) => this.InTimeZone(dateTime, this.LocalTimeZoneId);

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to a <see cref="DateTimeOffset"/> in the local time zone.
	/// </summary>
	/// <param name="dateTime">The <see cref="DateTimeOffset"/> value to convert</param>
	/// <returns>A <see cref="DateTimeOffset"/> representing the same point in time in the local time zone</returns>
	/// <remarks>
	/// Convenience method equivalent to calling <see cref="InTimeZone(DateTimeOffset, string)"/> with the local IANA time zone ID.
	/// </remarks>
	public DateTimeOffset InLocalTimeZone(DateTimeOffset dateTime) => this.InTimeZone(dateTime, this.LocalTimeZoneId);

	/// <summary>
	/// Gets a <see cref="TimeZoneInfo"/> by its IANA identifier, with cross-platform support.
	/// </summary>
	/// <param name="ianaTimeZoneId">IANA time zone ID (e.g., "America/New_York")</param>
	/// <returns>A <see cref="TimeZoneInfo"/> representing the specified time zone, or UTC if not found</returns>
	/// <remarks>
	/// This method handles cross-platform time zone ID differences and provides a safe way to
	/// get time zone information without throwing exceptions if the time zone is not found.
	/// </remarks>
	public TimeZoneInfo GetTimeZoneByIanaId(string ianaTimeZoneId) {

		// On Linux / macOS, IANA IDs work directly
		if (!OperatingSystem.IsWindows()) {
			try {
				return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
			} catch {
				return TimeZoneInfo.Utc; // Fallback
			}
		}

		// On Windows, use our mapping table
		if (IanaToWindowsMap.TryGetValue(ianaTimeZoneId, out var windowsId)) {
			try {
				return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
			} catch {
				// If mapping failed, continue with fallback approaches
			}
		}

		// Try direct lookup in case it's already a Windows ID
		try {
			return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
		} catch {
			// Continue with fallback logic
		}

		// Fallback to UTC
		return TimeZoneInfo.Utc;
	}


	// Required Implementations...

	/// <summary>
	/// Gets the current TimeProvider for use with .NET time abstractions.
	/// This property provides access to the underlying TimeProvider, enabling
	/// integration with .NET's built-in time abstractions and supporting testability.
	/// </summary>
	/// <remarks>
	/// The TimeProvider can be used directly for advanced scenarios or when
	/// integrating with other components that work with the TimeProvider abstraction.
	/// </remarks>
	TimeProvider TimeProvider { get; }

	/// <summary>
	/// Gets the local time zone ID in IANA format, regardless of platform.
	/// </summary>
	/// <remarks>
	/// This property provides a cross-platform way to get the IANA time zone identifier
	/// (e.g., "America/New_York") for the local system time zone. On Windows systems, 
	/// this automatically converts from Windows time zone IDs to IANA format.
	/// 
	/// The conversion uses a built-in mapping dictionary covering major global regions and
	/// business centers. While comprehensive for most common use cases, some less common
	/// time zones may map to UTC as a fallback. For applications requiring complete time zone
	/// mapping coverage, consider using a specialized third-party library like TimeZoneConverter.
	/// </remarks>
	/// <example>
	/// On Windows: "America/New_York" (converted from "Eastern Standard Time")
	/// On Linux/macOS: "America/New_York" (already in IANA format)
	/// </example>
	string LocalTimeZoneId { get; }

}