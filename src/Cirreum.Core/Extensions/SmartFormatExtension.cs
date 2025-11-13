namespace Cirreum;

using Cirreum.SmartExtensions;
using SmartFormat;
using SmartFormat.Core.Settings;
using SmartFormat.Extensions;

/// <summary>
/// This class holds the <see cref="Default"/> instance of a <see cref="SmartFormatter"/>.
/// The default instance has all extensions registered.
/// <para>
/// For optimized performance, create a <see cref="SmartFormatter"/> instance and register the
/// particular extensions that are needed for a specific call site.
/// </para>
/// <para>
/// <see cref="Smart"/> methods are not thread safe.
/// </para>
/// </summary>
public static class Smart {

	private static readonly SmartFormatter _formatter = CreateDefaultSmartFormat();

	#region: Smart.Format :

	/// <summary>
	/// Replaces the format items in the specified format string with the string representation or the corresponding object.
	/// </summary>
	/// <param name="format"></param>
	/// <param name="args"></param>
	/// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <i>Format(...)</i> overloads.</remarks>
	/// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
	public static string Format(string format, params object?[] args) {
		return Default.Format(format, args);
	}

	/// <summary>
	/// Replaces the format items in the specified format string with the string representation or the corresponding object.
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="format"></param>
	/// <param name="args"></param>
	/// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <i>Format(...)</i> overloads.</remarks>
	/// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
	public static string Format(IFormatProvider provider, string format, params object?[] args) {
		return Default.Format(provider, format, args);
	}

	#endregion

	#region: Overloads to match the signature of string.Format, and allow support for programming languages that don't support "params" :

	/// <summary>
	/// Replaces the format items in the specified format string with the string representation or the corresponding object.
	/// </summary>
	/// <param name="format"></param>
	/// <param name="arg0"></param>
	/// <param name="arg1"></param>
	/// <param name="arg2"></param>
	/// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <i>Format(...)</i> overloads.</remarks>
	/// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
	public static string Format(string format, object? arg0, object? arg1, object? arg2) {
		return Default.Format(format, arg0, arg1, arg2);
	}

	/// <summary>
	/// Replaces the format items in the specified format string with the string representation or the corresponding object.
	/// </summary>
	/// <param name="format"></param>
	/// <param name="arg0"></param>
	/// <param name="arg1"></param>
	/// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <i>Format(...)</i> overloads.</remarks>
	/// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
	public static string Format(string format, object? arg0, object? arg1) {
		return Default.Format(format, arg0, arg1);
	}

	/// <summary>
	/// Formats 
	/// </summary>
	/// <param name="format"></param>
	/// <param name="arg0"></param>
	/// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <i>Format(...)</i> overloads.</remarks>
	/// <returns></returns>
	public static string Format(string format, object? arg0) {
		return Default.Format(format, arg0);
	}

	#endregion

	#region: Default formatter :

	/// <summary>
	/// Gets or sets the default <see cref="SmartFormatter"/>.
	/// If not set, the <see cref="CreateDefaultSmartFormat"/> will be used.
	/// <para>
	/// Using the <see cref="ThreadStaticAttribute"/>, <see cref="Default"/> returns isolated instances of
	/// the <see cref="SmartFormatter"/> in each thread.
	/// </para>
	/// <para>
	/// It is recommended to set the thread-<see langword="static"/>
	/// <see cref="Default"/> <see cref="SmartFormatter"/> with the extensions that are actually needed.
	/// As <see cref="Default"/> is thread-static, this must be done on each thread.
	/// </para>
	/// </summary>
	public static SmartFormatter Default {
		get => _formatter;
		set => throw new InvalidOperationException("Cannot replace the default SmartFormatter. Create a new instance instead.");
	}

	/// <summary>
	/// Creates a new <see cref="SmartFormatter"/> instance with core extensions registered.
	/// For optimized performance, create a <see cref="SmartFormatter"/> instance and register the
	/// particular extensions that are really needed.
	/// <para>
	/// See <see cref="WellKnownExtensionTypes.Formatters"/> and <see cref="WellKnownExtensionTypes.Sources"/>
	/// for a complete list of well-known types.
	/// </para>
	/// </summary>
	/// <param name="settings">The <see cref="SmartSettings"/> to use, or <see langword="null"/> for default settings.</param>
	/// <returns>A <see cref="SmartFormatter"/> with core extensions registered:
	/// <para>
	/// <see cref="SmartFormat.Core.Extensions.ISource"/>s:
	/// <see cref="StringSource"/>, <see cref="ListFormatter"/>, <see cref="DictionarySource"/>,
	/// <see cref="ValueTupleSource"/>, <see cref="ReflectionSource"/>, <see cref="DefaultSource"/>, <see cref="KeyValuePairSource"/>
	/// </para>
	/// <para>
	/// <see cref="SmartFormat.Core.Extensions.IFormatter"/>s:
	/// <see cref="ListFormatter"/>, <see cref="PluralLocalizationFormatter"/>,
	/// <see cref="ConditionalFormatter"/>, <see cref="IsMatchFormatter"/>, <see cref="NullFormatter"/>,
	/// <see cref="ChooseFormatter"/>, <see cref="SubStringFormatter"/>, <see cref="DefaultFormatter"/>.
	/// </para>
	/// </returns>
	public static SmartFormatter CreateDefaultSmartFormat(SmartSettings? settings = null) {

		settings ??= new SmartSettings();
		settings.Formatter.ErrorAction = FormatErrorAction.MaintainTokens;
		settings.Parser.ErrorAction = ParseErrorAction.MaintainTokens;
		settings.Parser.AddCustomSelectorChars("$".ToCharArray());

		// Register all default extensions here:
		var notSoSmart = SmartFormat.Smart.CreateDefaultSmartFormat(settings);
		notSoSmart
			.InsertExtension(0, new FuncSource())
			.InsertExtension(1, new DnsCommandSource())
			.InsertExtension(2, new EnvironmentCommandSource())
			.InsertExtension(3, new ProcessCommandSource())
			.InsertExtension(4, new GuidCommandSource())
			.AddExtensions(new PathFormatter())
			;

		return notSoSmart;

	}

	#endregion
}