namespace Cirreum.Authorization;

using System;

/// <summary>
/// Represents a role in the system with a defined namespace and name.
/// Roles follow the format {namespace}:{name}.
/// </summary>
/// <remarks>
/// <para>
/// NOTE: both <see cref="Namespace"/> and <see cref="Name"/> are changed to
/// lower-invariant regardless of their original casing during construction.
/// E.g., mynamespace:name
/// </para>
/// </remarks>
public record Role : IComparable<Role> {

	/// <summary>
	/// The default namespace for built-in Application roles.
	/// </summary>
	public const string AppNamespace = "app";

	/// <summary>
	/// Is this Role a reservered namespaced {app:} role.
	/// </summary>
	public bool IsApplicationRole => this.Namespace == AppNamespace;

	/// <summary>
	/// Gets the Namespace of the role.
	/// </summary>
	public string Namespace { get; }

	/// <summary>
	/// Gets the Name of the role.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Internal constructor for application roles.
	/// Only accessible to the core security assembly.
	/// </summary>
	/// <param name="name">The application role name.</param>
	/// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
	internal Role(string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		this.Namespace = AppNamespace;
		this.Name = name.ToLowerInvariant();
	}

	/// <summary>
	/// Public constructor for custom namespace roles.
	/// </summary>
	/// <param name="namespace">The namespace of the role.</param>
	/// <param name="name">The role name.</param>
	/// <remarks>
	/// <para>
	/// NOTE: both <paramref name="namespace"/> and <paramref name="name"/> are changed to
	/// lower-invariant regardless of their original casing.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown when namespace or name is null or empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the namespace is "app".</exception>
	public Role(string @namespace, string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		// Validate and normalize
		@namespace = @namespace.ToLowerInvariant();
		name = name.ToLowerInvariant();

		// Prevent namespaces from using "app" namespace
		if (@namespace == AppNamespace) {
			throw new InvalidOperationException(
				$"Scoped roles cannot use the '{AppNamespace}' role Namespace. Use your domain or other meaning for your namespace instead.");
		}

		this.Namespace = @namespace;
		this.Name = name;

	}

	/// <summary>
	/// Creates a role.
	/// </summary>
	/// <param name="namespace">The namespace of the role.</param>
	/// <param name="name">The role name.</param>
	/// <remarks>
	/// <para>
	/// NOTE: both <paramref name="namespace"/> and <paramref name="name"/> are changed to
	/// lower-invariant regardless of their original casing.
	/// </para>
	/// </remarks>
	/// <returns>A new <see cref="Role"/>.</returns>
	public static Role ForNamespace(string @namespace, string name) => new(@namespace, name);

	/// <summary>
	/// Creates an Application namespaced role.
	/// Only available inside the authorized security assembly.
	/// </summary>
	/// <param name="name">The application role name.</param>
	/// <returns>A new application-Namespaced role.</returns>
	internal static Role ForApp(string name) => new(name);

	/// <summary>
	/// Gets the string representation of the role in {namespace}:{name} format.
	/// </summary>
	public override string ToString() => $"{this.Namespace}:{this.Name}";

	/// <summary>
	/// Compares roles alphabetically based on {namespace}:{name}.
	/// </summary>
	public int CompareTo(Role? other) {
		if (other is null) {
			return 1;
		}

		return string.Compare(this.ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
	}


	// -------------- 🔹 String Comparison Support Below 🔹 --------------

	/// <summary>
	/// Implicitly converts a Role to a string in "{namespace}:{name}" format.
	/// </summary>
	public static implicit operator string(Role role) => role.ToString();

	/// <summary>
	/// Allows direct comparison between a Role and a string.
	/// </summary>
	public static bool operator ==(Role? role, string? str) {
		return role?.ToString().Equals(str, StringComparison.OrdinalIgnoreCase) ?? str is null;
	}

	/// <summary>
	/// Allows direct comparison between a string and a Role.
	/// </summary>
	public static bool operator ==(string? str, Role? role) => role == str;

	/// <summary>
	/// Allows direct inequality comparison between a Role and a string.
	/// </summary>
	public static bool operator !=(Role? role, string? str) => !(role == str);

	/// <summary>
	/// Allows direct inequality comparison between a string and a Role.
	/// </summary>
	public static bool operator !=(string? str, Role? role) => !(role == str);

}