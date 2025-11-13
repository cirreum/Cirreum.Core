namespace Cirreum.Components.Interop;

/// <summary>
/// Represents resolved formatting options for date and time operations.
/// </summary>
public class ResolvedDateTimeFormatOptions {
	/// <summary>
	/// Gets or sets the locale identifier.
	/// </summary>
	public string? Locale { get; set; }

	/// <summary>
	/// Gets or sets the calendar system.
	/// </summary>
	public string? Calendar { get; set; }

	/// <summary>
	/// Gets or sets the numbering system.
	/// </summary>
	public string? NumberingSystem { get; set; }

	/// <summary>
	/// Gets or sets the time zone.
	/// </summary>
	public string? TimeZone { get; set; }

	/// <summary>
	/// Gets or sets the time zone.
	/// </summary>
	public int TimeZoneOffset { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to use 12-hour time format.
	/// </summary>
	public bool? Hour12 { get; set; }

	/// <summary>
	/// Gets or sets the weekday format.
	/// </summary>
	public string? Weekday { get; set; }

	/// <summary>
	/// Gets or sets the era format.
	/// </summary>
	public string? Era { get; set; }

	/// <summary>
	/// Gets or sets the year format.
	/// </summary>
	public string? Year { get; set; }

	/// <summary>
	/// Gets or sets the month format.
	/// </summary>
	public string? Month { get; set; }

	/// <summary>
	/// Gets or sets the day format.
	/// </summary>
	public string? Day { get; set; }

	/// <summary>
	/// Gets or sets the hour format.
	/// </summary>
	public string? Hour { get; set; }

	/// <summary>
	/// Gets or sets the minute format.
	/// </summary>
	public string? Minute { get; set; }

	/// <summary>
	/// Gets or sets the second format.
	/// </summary>
	public string? Second { get; set; }

	/// <summary>
	/// Gets or sets the time zone name format.
	/// </summary>
	public string? TimeZoneName { get; set; }

}