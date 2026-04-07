namespace Cirreum.Authorization;

using Cirreum.Authorization.Grants;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Per-type cache of <see cref="RequiresPermissionAttribute"/> declarations. Reads all
/// <c>[RequiresPermission]</c> attributes on a resource type (including stacked and inherited
/// declarations) exactly once, memoizes the result, and returns the same immutable list on
/// every subsequent lookup.
/// </summary>
/// <remarks>
/// <para>
/// The cache is consulted by the authorization pipeline setup to hoist required permissions
/// onto <see cref="AuthorizationContext{TResource}.Permissions"/>. Every stage — the
/// Stage 1 gate, Stage 2 resource authorizers, Stage 3 policy validators — then reads
/// permissions from the context without doing its own reflection.
/// </para>
/// <para>
/// For granted resources (those implementing <see cref="IGrantedCommandBase"/>,
/// <see cref="IGrantedReadBase"/>, <see cref="IGrantedListBase"/>), this cache also:
/// </para>
/// <list type="bullet">
///   <item><description>Resolves name-only <c>[RequiresPermission("delete")]</c> attributes
///   by deriving the domain feature from the resource type's namespace convention via
///   <see cref="DomainFeatureResolver"/>.</description></item>
///   <item><description>Validates that all explicit permission features match the domain
///   feature — cross-domain permissions are a misconfiguration and cause a runtime
///   error.</description></item>
/// </list>
/// <para>
/// Permissions are deduplicated (by <see cref="Permission"/> equality) to keep AND-semantics
/// meaningful when inheritance causes the same attribute to appear twice.
/// </para>
/// </remarks>
public static class PermissionSetCache {

	private static readonly ConcurrentDictionary<Type, PermissionSet> Cache = new();

	/// <summary>
	/// Returns the distinct set of permissions declared on <paramref name="resourceType"/> via
	/// <see cref="RequiresPermissionAttribute"/>, computing and caching the result on first lookup.
	/// For granted resources, resolves name-only permissions from the type's namespace convention
	/// and validates feature consistency.
	/// </summary>
	/// <param name="resourceType">The resource type to inspect.</param>
	/// <returns>
	/// The declared required permissions. Returns an empty list when no attributes are present.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when a name-only permission is used on a type without a <c>*.Domain.*</c>
	/// namespace, or when an explicit permission feature does not match the domain feature.
	/// </exception>
	public static PermissionSet GetFor(Type resourceType) {
		ArgumentNullException.ThrowIfNull(resourceType);
		return Cache.GetOrAdd(resourceType, static t => {
			var attrs = t.GetCustomAttributes<RequiresPermissionAttribute>(inherit: true).ToArray();
			if (attrs.Length == 0) {
				return PermissionSet.Empty;
			}

			var domainFeature = DomainFeatureResolver.Resolve(t);

			var seen = new HashSet<Permission>();
			List<Permission>? list = null;

			foreach (var attr in attrs) {
				Permission permission;

				if (attr.NeedsNamespaceResolution) {
					if (domainFeature is null) {
						throw new InvalidOperationException(
							$"[RequiresPermission(\"{attr.UnresolvedName}\")] on '{t.Name}' uses the " +
							$"name-only form but the type's namespace does not follow the " +
							$"*.Domain.* convention required for feature resolution. Use " +
							$"[RequiresPermission(\"feature\", \"operation\")] with an explicit feature.");
					}
					permission = new Permission(domainFeature, attr.UnresolvedName!);
					attr.Permission = permission;
				} else {
					permission = attr.Permission!;
					if (domainFeature is not null &&
						!string.Equals(permission.Feature, domainFeature, StringComparison.OrdinalIgnoreCase)) {
						throw new InvalidOperationException(
							$"[RequiresPermission(\"{permission}\")] on '{t.Name}' uses feature " +
							$"'{permission.Feature}' which does not match the domain feature " +
							$"'{domainFeature}'. All permissions on a granted " +
							$"resource must use the domain's feature. Cross-cutting concerns " +
							$"belong in Stage 2 resource authorizers or Stage 3 policy validators.");
					}
				}

				if (seen.Add(permission)) {
					(list ??= []).Add(permission);
				}
			}

			return list is null ? PermissionSet.Empty : new PermissionSet(list);
		});
	}

	/// <summary>
	/// Returns the distinct set of permissions declared on <typeparamref name="TResource"/>.
	/// </summary>
	public static PermissionSet GetFor<TResource>() => GetFor(typeof(TResource));

	/// <summary>
	/// Resolves the domain feature from the resource type's namespace convention.
	/// Returns <see langword="null"/> when the type does not follow the convention.
	/// </summary>
	internal static string? ResolveDomainNamespace(Type resourceType) =>
		DomainFeatureResolver.Resolve(resourceType);
}
