namespace Cirreum.Authorization;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a fine-grained permission in the system with a defined feature and operation.
/// Permissions follow the format <c>{feature}:{operation}</c> (e.g., <c>issues:delete</c>,
/// <c>documents:browse</c>, <c>data:loans</c>).
/// </summary>
/// <remarks>
/// <para>
/// Permissions are capabilities — "what you can do." The <see cref="Feature"/> is the
/// domain feature area (derived from namespace convention), and the <see cref="Operation"/>
/// is the verb (e.g., read, write, delete).
/// </para>
/// <para>
/// Both <see cref="Feature"/> and <see cref="Operation"/> are normalized to lower-invariant
/// during construction, matching <see cref="Role"/> behavior.
/// </para>
/// </remarks>
[JsonConverter(typeof(PermissionJsonConverter))]
public sealed record Permission : IComparable<Permission> {

	/// <summary>Gets the domain feature area of the permission (e.g., "issues", "documents", "data").</summary>
	public string Feature { get; }

	/// <summary>Gets the operation of the permission (e.g., "read", "write", "delete").</summary>
	public string Operation { get; }

	/// <summary>
	/// Creates a new permission with the specified feature and operation.
	/// </summary>
	/// <param name="feature">The domain feature area (e.g., "issues", "documents", "data").</param>
	/// <param name="operation">The operation verb (e.g., "read", "write", "delete").</param>
	public Permission(string feature, string operation) {
		ArgumentException.ThrowIfNullOrWhiteSpace(feature);
		ArgumentException.ThrowIfNullOrWhiteSpace(operation);
		this.Feature = feature.ToLowerInvariant();
		this.Operation = operation.ToLowerInvariant();
	}

	/// <summary>
	/// Parses a permission from its string representation (<c>feature:operation</c>).
	/// </summary>
	public static Permission Parse(string value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(value);
		var parts = value.Split(':', 2);
		if (parts.Length != 2) {
			throw new FormatException($"Permission must be in 'feature:operation' format. Got: '{value}'");
		}
		return new Permission(parts[0], parts[1]);
	}

	/// <summary>
	/// Attempts to parse a permission from its string representation.
	/// </summary>
	public static bool TryParse(string? value, out Permission? permission) {
		permission = null;
		if (string.IsNullOrWhiteSpace(value)) {
			return false;
		}
		var parts = value.Split(':', 2);
		if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) {
			return false;
		}
		permission = new Permission(parts[0], parts[1]);
		return true;
	}

	/// <summary>
	/// Gets the string representation in <c>feature:operation</c> format.
	/// </summary>
	public override string ToString() => $"{this.Feature}:{this.Operation}";

	/// <summary>Compares permissions alphabetically.</summary>
	public int CompareTo(Permission? other) {
		if (other is null) {
			return 1;
		}
		return string.Compare(this.ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Implicitly converts a Permission to its string representation.</summary>
	public static implicit operator string(Permission permission) => permission.ToString();

	/// <summary>Allows direct comparison between a Permission and a string.</summary>
	public static bool operator ==(Permission? permission, string? str) =>
		permission?.ToString().Equals(str, StringComparison.OrdinalIgnoreCase) ?? str is null;

	/// <summary>Allows direct comparison between a string and a Permission.</summary>
	public static bool operator ==(string? str, Permission? permission) => permission == str;

	/// <summary>Allows direct inequality comparison between a Permission and a string.</summary>
	public static bool operator !=(Permission? permission, string? str) => !(permission == str);

	/// <summary>Allows direct inequality comparison between a string and a Permission.</summary>
	public static bool operator !=(string? str, Permission? permission) => !(permission == str);

	public bool Equals(Permission? other) =>
		other is not null &&
		string.Equals(this.Feature, other.Feature, StringComparison.OrdinalIgnoreCase) &&
		string.Equals(this.Operation, other.Operation, StringComparison.OrdinalIgnoreCase);

	public override int GetHashCode() =>
		HashCode.Combine(
			this.Feature.ToLowerInvariant(),
			this.Operation.ToLowerInvariant());
}

/// <summary>
/// JSON converter that serializes <see cref="Permission"/> as a simple <c>"namespace:name"</c> string.
/// </summary>
public sealed class PermissionJsonConverter : JsonConverter<Permission> {

	public override Permission? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var value = reader.GetString();
		return value is null ? null : Permission.Parse(value);
	}

	public override void Write(Utf8JsonWriter writer, Permission value, JsonSerializerOptions options) {
		writer.WriteStringValue(value.ToString());
	}
}
