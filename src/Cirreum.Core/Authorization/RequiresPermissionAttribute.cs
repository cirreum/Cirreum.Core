namespace Cirreum.Authorization;

/// <summary>
/// Declares a permission that an operation requires on the target owner. Consumed by Stage 1
/// (grant resolution filters grants by these permissions), Stage 2 (resource authorizers can
/// write permission-aware rules via <c>ctx.Permissions</c>), and Stage 3 (policy
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
///     <b>Name-only</b> (<c>[RequiresPermission("delete")]</c>) — the permission feature
///     is auto-resolved from the resource type's namespace convention via
///     <see cref="DomainFeatureResolver"/>. Use this for granted resources.
///   </description></item>
///   <item><description>
///     <b>Feature + operation</b> (<c>[RequiresPermission("issues", "delete")]</c>) — explicit
///     feature. Validated against the domain feature for granted resources.
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
/// public sealed record DeleteIssue(string Id) : IGrantedCommand {
///     public string? OwnerId { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute {

	/// <summary>
	/// Declares a permission by operation name only. The feature is resolved at pipeline setup
	/// from the resource type's namespace convention via <see cref="DomainFeatureResolver"/>.
	/// </summary>
	/// <param name="name">The operation name (e.g., <c>"read"</c>, <c>"write"</c>, <c>"delete"</c>).</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown at pipeline setup if the resource type's namespace does not follow the
	/// <c>*.Domain.*</c> convention required for feature resolution.
	/// </exception>
	public RequiresPermissionAttribute(string name) {
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		this.UnresolvedName = name.ToLowerInvariant();
	}

	/// <summary>
	/// Declares a permission with an explicit feature and operation.
	/// </summary>
	/// <param name="feature">The domain feature area (typically the bounded context — e.g., <c>"issues"</c>).</param>
	/// <param name="operation">The operation verb (e.g., <c>"read"</c>, <c>"write"</c>, <c>"delete"</c>).</param>
	public RequiresPermissionAttribute(string feature, string operation) {
		ArgumentException.ThrowIfNullOrWhiteSpace(feature);
		ArgumentException.ThrowIfNullOrWhiteSpace(operation);
		this.Permission = new Permission(feature, operation);
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
	/// overload until resolved by <see cref="PermissionSetCache"/>.
	/// </summary>
	public Permission? Permission { get; internal set; }

	/// <summary>
	/// The raw permission name when constructed with the single-arg (name-only) overload.
	/// <see langword="null"/> when the full <see cref="Permission"/> was provided at construction.
	/// </summary>
	internal string? UnresolvedName { get; }

	/// <summary>
	/// <see langword="true"/> when this attribute needs feature resolution from the
	/// resource type's namespace convention via <see cref="DomainFeatureResolver"/>.
	/// </summary>
	internal bool NeedsNamespaceResolution => this.UnresolvedName is not null;
}