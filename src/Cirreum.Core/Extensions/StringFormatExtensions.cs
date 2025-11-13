namespace System;

using Cirreum;

/// <summary>
/// Utilities extension methods to help format strings.
/// </summary>
public static class StringFormatExtensions {

	/// <summary>
	/// Formats a string using Smart.Format with invariant culture, replacing format items with 
	/// formatted representations of the provided arguments. Supports advanced formatting features 
	/// beyond standard string.Format.
	/// </summary>
	/// <param name="format">A composite format string.</param>
	/// <param name="args">An object array that contains zero or more objects to format.</param>
	/// <returns>
	/// A new string in which the format items in the original format string have been replaced with
	/// the formatted string representations of the corresponding objects in args. Supports advanced
	/// formatting features beyond standard string.Format, including conditional formatting, pluralization,
	/// and nested object property access.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Leverages the <see cref="Smart.Format(IFormatProvider, string, object?[])"/> to process
	/// any special formatting rules contained in the format string.
	/// </para>
	/// <para>
	/// Example:
	/// <code>
	/// var r = "{0} {0:N2} {1:yyyy-MM-dd}".Format(5, new DateTime(1900, 12, 31));
	/// </code>
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// format or args is null.
	/// </exception>
	/// <exception cref="FormatException">
	/// format is invalid. -or- The index of a format item is less than zero, or greater
	/// than or equal to the length of the args array.
	/// </exception>
	public static string Format(this string format, params object[] args) {
		return Smart.Format(Globalization.CultureInfo.InvariantCulture, format, args);
	}

	/// <summary>
	/// Formats a string using Smart.Format with the specified culture, replacing format items with 
	/// formatted representations of the provided arguments. Supports advanced formatting features 
	/// beyond standard string.Format.
	/// </summary>
	/// <param name="provider">An object that supplies culture-specific formatting information.</param>
	/// <param name="format">A composite format string.</param>
	/// <param name="args">An object array that contains zero or more objects to format.</param>
	/// <returns>
	/// A new string in which the format items in the original format string have been replaced with
	/// the formatted string representations of the corresponding objects in args. Supports advanced
	/// formatting features beyond standard string.Format, including conditional formatting, pluralization,
	/// and nested object property access.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Leverages the <see cref="Smart.Format(IFormatProvider, string, object?[])"/> to process
	/// any special formatting rules contained in the format string.
	/// </para>
	/// <para>
	/// Example:
	/// <code>
	/// var r = "{0} {0:N2} {1:yyyy-MM-dd}".Format(Globalization.CultureInfo.InvariantCulture, 5, new DateTime(1900, 12, 31));
	/// </code>
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// format or args is null.
	/// </exception>
	/// <exception cref="FormatException">
	/// format is invalid. -or- The index of a format item is less than zero, or greater
	/// than or equal to the length of the args array.
	/// </exception>
	public static string Format(this string format, IFormatProvider provider, params object[] args) {
		return Smart.Format(provider, format, args);
	}

}