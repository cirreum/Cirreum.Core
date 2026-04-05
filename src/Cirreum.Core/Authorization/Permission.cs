namespace Cirreum.Authorization;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a fine-grained permission in the system with a defined namespace and name.
/// Permissions follow the format <c>{namespace}:{name}</c> (e.g., <c>document:browse</c>,
/// <c>folder:delete</c>, <c>data:loans</c>).
/// </summary>
/// <remarks>
/// <para>
/// Permissions are capabilities — "what you can do." They are paired with
/// <see cref="Role"/> ("who you are") in a <see cref="PermissionGrant"/> to express
/// "this role can do these things."
/// </para>
/// <para>
/// Both <see cref="Namespace"/> and <see cref="Name"/> are normalized to lower-invariant
/// during construction, matching <see cref="Role"/> behavior.
/// </para>
/// </remarks>
[JsonConverter(typeof(PermissionJsonConverter))]
public sealed record Permission : IComparable<Permission> {

	/// <summary>Gets the namespace of the permission (e.g., "document", "folder", "data").</summary>
	public string Namespace { get; }

	/// <summary>Gets the name of the permission (e.g., "browse", "upload", "loans").</summary>
	public string Name { get; }

	/// <summary>
	/// Creates a new permission with the specified namespace and name.
	/// </summary>
	/// <param name="namespace">The permission namespace (e.g., "document", "folder", "data").</param>
	/// <param name="name">The permission name (e.g., "browse", "upload", "loans").</param>
	public Permission(string @namespace, string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		this.Namespace = @namespace.ToLowerInvariant();
		this.Name = name.ToLowerInvariant();
	}

	/// <summary>
	/// Parses a permission from its string representation (<c>namespace:name</c>).
	/// </summary>
	public static Permission Parse(string value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(value);
		var parts = value.Split(':', 2);
		if (parts.Length != 2) {
			throw new FormatException($"Permission must be in 'namespace:name' format. Got: '{value}'");
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
	/// Gets the string representation in <c>namespace:name</c> format.
	/// </summary>
	public override string ToString() => $"{this.Namespace}:{this.Name}";

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
		string.Equals(this.Namespace, other.Namespace, StringComparison.OrdinalIgnoreCase) &&
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);

	public override int GetHashCode() =>
		HashCode.Combine(
			this.Namespace.ToLowerInvariant(),
			this.Name.ToLowerInvariant());
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
