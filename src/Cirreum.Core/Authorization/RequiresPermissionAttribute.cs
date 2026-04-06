namespace Cirreum.Authorization;

/// <summary>
/// Declares a permission that an operation requires on the target owner. Consumed by Stage 1
/// (grant resolution filters grants by these permissions), Stage 2 (resource authorizers can
/// write permission-aware rules via <c>ctx.RequiredPermissions</c>), and Stage 3 (policy
/// validators can key on required permissions).
/// </summary>
/// <remarks>
/// <para>
/// Stack multiple attributes when an operation needs more than one permission — AND semantics.
/// Grant resolution returns only owners where the caller holds EVERY required permission.
/// </para>
/// <para>
/// Three constructor overloads:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Name-only</b> (<c>[RequiresPermission("delete")]</c>) — the permission namespace
///     is auto-resolved from the resource's TDomain marker via
///     <see cref="Grants.GrantDomainAttribute"/>. Use this for granted resources.
///   </description></item>
///   <item><description>
///     <b>Namespace + name</b> (<c>[RequiresPermission("issues", "delete")]</c>) — explicit
///     namespace. Validated against the domain namespace for granted resources.
///   </description></item>
///   <item><description>
///     <b>Permission object</b> (<c>[RequiresPermission(Permissions.Issues.Delete)]</c>) —
///     pre-constructed <see cref="Permission"/>. Namespace validated against domain.
///   </description></item>
/// </list>
/// <para>
/// Attributes are read once per resource type at pipeline setup and cached. No per-request
/// reflection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresPermission("delete")]
/// public sealed record DeleteIssue(string Id)
///     : IGrantedCommand&lt;IIssueOperation&gt; {
///     public string? OwnerId { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute {

	/// <summary>
	/// Declares a permission by name only. The namespace is resolved at pipeline setup
	/// from the resource's TDomain marker via <see cref="Grants.GrantDomainAttribute"/>.
	/// </summary>
	/// <param name="name">The permission name (e.g., <c>"read"</c>, <c>"write"</c>, <c>"delete"</c>).</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown at pipeline setup if the resource type does not implement a Granted interface
	/// with a TDomain marker that has <c>[GrantDomain]</c> applied.
	/// </exception>
	public RequiresPermissionAttribute(string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		this.UnresolvedName = name.ToLowerInvariant();
	}

	/// <summary>
	/// Declares a permission with an explicit namespace and name.
	/// </summary>
	/// <param name="namespace">The permission namespace (typically the bounded context — e.g., <c>"issues"</c>).</param>
	/// <param name="name">The permission name (typically the verb — e.g., <c>"read"</c>, <c>"write"</c>, <c>"delete"</c>).</param>
	public RequiresPermissionAttribute(string @namespace, string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		this.Permission = new Permission(@namespace, name);
	}

	/// <summary>
	/// Declares a permission from a pre-constructed <see cref="Authorization.Permission"/> value.
	/// </summary>
	/// <param name="permission">The permission that is required.</param>
	public RequiresPermissionAttribute(Permission permission) {
		this.Permission = permission;
	}

	/// <summary>
	/// The declared permission. <see langword="null"/> when constructed with the name-only
	/// overload until resolved by <see cref="RequiredPermissionsCache"/>.
	/// </summary>
	public Permission? Permission { get; internal set; }

	/// <summary>
	/// The raw permission name when constructed with the single-arg (name-only) overload.
	/// <see langword="null"/> when the full <see cref="Permission"/> was provided at construction.
	/// </summary>
	internal string? UnresolvedName { get; }

	/// <summary>
	/// <see langword="true"/> when this attribute needs namespace resolution from the
	/// resource's TDomain via <see cref="Grants.GrantDomainAttribute"/>.
	/// </summary>
	internal bool NeedsNamespaceResolution => this.UnresolvedName is not null;
}