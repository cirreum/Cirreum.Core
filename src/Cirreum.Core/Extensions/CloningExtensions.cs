namespace Cirreum;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Extension to support cloning of objects.
/// </summary>
public static class CloningExtensions {

	private static JsonSerializerOptions? defaultOptions;
	private static JsonSerializerOptions? enumConverterOptions;
	private static JsonSerializerOptions? indentedOptions;
	private static JsonSerializerOptions? indentedEnumOptions;

	private static JsonSerializerOptions GetOptions(bool indented, bool enumNames) {
		return (indented, enumNames) switch {
			(true, true) => indentedEnumOptions ??= new JsonSerializerOptions {
				Converters = { new JsonStringEnumConverter() },
				WriteIndented = true
			},
			(false, true) => enumConverterOptions ??= new JsonSerializerOptions {
				Converters = { new JsonStringEnumConverter() }
			},
			(true, false) => indentedOptions ??= new JsonSerializerOptions {
				WriteIndented = true
			},
			(false, false) => defaultOptions ??= JsonSerializerOptions.Default
		};
	}

	/// <summary>
	/// Perform a deep copy of the object using JSON serialization.
	/// </summary>
	/// <typeparam name="T">The type of object being copied.</typeparam>
	/// <param name="source">The object instance to copy.</param>
	/// <returns>The copied object.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static T JsonClone<T>(this T source) {

		// Don't serialize a null objects
		var obj = Check.NotNull(source);

		var newVal = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));

		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");

	}

	/// <summary>
	/// Perform a deep copy of the object using JSON serialization.
	/// </summary>
	/// <typeparam name="T">The type of object being copied.</typeparam>
	/// <param name="source">The object instance to copy.</param>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
	/// <returns>The copied object.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="options"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static T JsonClone<T>(this T source, JsonSerializerOptions options) {

		// Don't serialize a null objects
		var obj = Check.NotNull(source);
		var opt = Check.NotNull(options);

		var newVal = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj, opt), opt);

		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");

	}

	/// <summary>
	/// Converts the specified <paramref name="source"/> object using Json serialization
	/// to a deserialized type of <typeparamref name="TResult"/>.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> to deserialize to.</typeparam>
	/// <param name="source">The source object to convert.</param>
	/// <returns>The <typeparamref name="TResult"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static TResult JsonConvertTo<TResult>(this object source) {

		// Don't serialize a null objects
		var obj = Check.NotNull(source);

		var data = JsonSerializer.Serialize(obj);

		var newVal = JsonSerializer.Deserialize<TResult>(data);
		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");

	}

	/// <summary>
	/// Converts the specified <paramref name="source"/> object using Json serialization, and
	/// to a deserialized type of <typeparamref name="TResult"/>.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> to deserialize to.</typeparam>
	/// <param name="source">The source object to convert.</param>
	/// <param name="resultTypeInfo">The <see cref="JsonTypeInfo{TResult}"/> used for deserialization.</param>
	/// <returns>The <typeparamref name="TResult"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static TResult JsonConvertTo<TResult>(this object source, JsonTypeInfo<TResult> resultTypeInfo) {

		// Don't serialize a null objects
		var obj = Check.NotNull(source);

		var data = JsonSerializer.Serialize(obj);

		var newVal = JsonSerializer.Deserialize(data, resultTypeInfo);

		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");

	}

	/// <summary>
	/// Converts the specified <paramref name="source"/> object using Json serialization, and
	/// to a deserialized type of <typeparamref name="TResult"/>.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> to deserialize to.</typeparam>
	/// <param name="source">The source object to convert.</param>
	/// <param name="sourceType">The source object's <see cref="Type"/>.</param>
	/// <param name="resultTypeInfo">The <see cref="JsonTypeInfo{TResult}"/> used for deserialization.</param>
	/// <returns>The <typeparamref name="TResult"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static TResult JsonConvertTo<TResult>(this object source, Type sourceType, JsonTypeInfo<TResult> resultTypeInfo) {

		// Don't serialize a null objects
		var obj = Check.NotNull(source);

		var data = JsonSerializer.Serialize(obj, sourceType);

		var newVal = JsonSerializer.Deserialize(data, resultTypeInfo);

		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");

	}

	/// <summary>
	/// Serializes the specified <paramref name="source"/> object to a Json string.
	/// </summary>
	/// <typeparam name="T">The <see cref="Type"/> of the source object being serialized.</typeparam>
	/// <param name="source">The source object to serialize.</param>
	/// <param name="indented">Set to <see langword="true"/> to use indented formatting; otherwise <see langword="false"/>. Default: <see langword="false"/></param>
	/// <param name="enumNames"><see langword="true"/> to use the <see cref="JsonStringEnumConverter"/>; otherwise <see langword="false"/>. Default: <see langword="false"/>.</param>
	/// <returns>
	/// A string of Json text representing the Serialized object, or an empty
	/// string if <paramref name="source"/> is <see langword="null"/>.
	/// </returns>
	public static string ToJson<T>(this T source, bool indented = false, bool enumNames = false) {
		return source.ToJson(GetOptions(indented, enumNames));
	}

	/// <summary>
	/// Serializes the specified <paramref name="source"/> object to a Json string.
	/// </summary>
	/// <typeparam name="T">The <see cref="Type"/> of the source object being serialized.</typeparam>
	/// <param name="source">The source object to serialize.</param>
	/// <param name="serializerOptions">The specified <see cref="JsonSerializerOptions"/> to use.</param>
	/// <returns>
	/// A string of Json text representing the Serialized object, or an empty
	/// string if <paramref name="source"/> is <see langword="null"/>.
	/// </returns>
	public static string ToJson<T>(this T source, JsonSerializerOptions serializerOptions) {

		// Don't serialize a null object, simply return an empty string
		if (source == null) {
			return string.Empty;
		}

		return JsonSerializer.Serialize(source, serializerOptions);

	}

	/// <summary>
	/// Deserializes the specified <paramref name="json"/> string to the desired <see cref="Type"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="Type"/> to deserialize the <paramref name="json"/> to.</typeparam>
	/// <param name="json">The JSON string to deserialize from.</param>
	/// <param name="enumNames">Set to <see langword="true"/> to use the <see cref="JsonStringEnumConverter"/>; otherwise <see langword="false"/>. Default: <see langword="false"/></param>
	/// <returns>An instance of <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static T FromJson<T>(this string json, bool enumNames = false) {
		var text = Check.NotEmpty(json);
		if (enumNames) {
			var newVal1 = JsonSerializer.Deserialize<T>(text, GetOptions(false, enumNames));
			return newVal1 ?? throw new InvalidOperationException("Deserialization returned null.");
		}
		var newVal2 = JsonSerializer.Deserialize<T>(text);
		return newVal2 ?? throw new InvalidOperationException("Deserialization returned null.");
	}

	/// <summary>
	/// Deserializes the specified <paramref name="json"/> string to the desired <see cref="Type"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="Type"/> to deserialize the <paramref name="json"/> to.</typeparam>
	/// <param name="json">The JSON string to deserialize from.</param>
	/// <param name="serializerOptions">The specified <see cref="JsonSerializerOptions"/> to use.</param>
	/// <returns>An instance of <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static T FromJson<T>(this string json, JsonSerializerOptions serializerOptions) {
		var text = Check.NotEmpty(json);
		var newVal = JsonSerializer.Deserialize<T>(text, serializerOptions);
		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");
	}

	/// <summary>
	/// Deserializes the specified <paramref name="json"/> string to the desired <see cref="Type"/>.
	/// </summary>
	/// <typeparam name="T">The <see cref="Type"/> to deserialize the <paramref name="json"/> to.</typeparam>
	/// <param name="json">The JSON string to deserialize from.</param>
	/// <param name="typeInfo">The <see cref="JsonTypeInfo{TResult}"/> used for deserialization.</param>
	/// <returns>An instance of <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
	public static T FromJson<T>(this string json, JsonTypeInfo<T> typeInfo) {
		var text = Check.NotEmpty(json);
		var newVal = JsonSerializer.Deserialize<T>(text, typeInfo);
		return newVal ?? throw new InvalidOperationException("Deserialization returned null.");
	}

	/// <summary>
	/// Converts the input instance to Type <typeparamref name="T"/>.
	/// Supports primitive types, enums, and types with TypeConverters.
	/// </summary>
	/// <typeparam name="T">The type to convert to.</typeparam>
	/// <param name="input">Input that will be converted to specified type.</param>
	/// <param name="defaultValue">Value returned when input is null or DBNull.</param>
	/// <returns>The converted instance of <paramref name="input"/> to <typeparamref name="T"/>, otherwise <paramref name="defaultValue"/>.</returns>
	/// <exception cref="InvalidCastException">Thrown when the conversion is not supported.</exception>
	/// <exception cref="FormatException">Thrown when the input format is invalid for the target type.</exception>
	/// <exception cref="OverflowException">Thrown when the input value is outside the range of the target type.</exception>
	public static T To<T>(this object? input, T defaultValue) {

		if (input == null || input == DBNull.Value) {
			return defaultValue;
		}

		if (typeof(T).IsEnum) {
			var underlyingType = Enum.GetUnderlyingType(typeof(T));
			var underlyingValue = Convert.ChangeType(input, underlyingType);
			return (T)Enum.ToObject(typeof(T), underlyingValue);
		}

		return (T)Convert.ChangeType(input, typeof(T));

	}

}