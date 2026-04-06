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
/// onto <see cref="AuthorizationContext{TResource}.RequiredPermissions"/>. Every stage — the
/// Stage 1 gate, Stage 2 resource authorizers, Stage 3 policy validators — then reads
/// permissions from the context without doing its own reflection.
/// </para>
/// <para>
/// For granted resources (those implementing <see cref="IGrantedCommand{TDomain}"/>,
/// <see cref="IGrantedRead{TDomain, TResponse}"/>, etc.), this cache also:
/// </para>
/// <list type="bullet">
///   <item><description>Resolves name-only <c>[RequiresPermission("delete")]</c> attributes
///   by reading the permission namespace from the TDomain marker's
///   <see cref="GrantDomainAttribute"/>.</description></item>
///   <item><description>Validates that all explicit permission namespaces match the domain
///   namespace — cross-domain permissions are a misconfiguration and cause a runtime
///   error.</description></item>
/// </list>
/// <para>
/// Permissions are deduplicated (by <see cref="Permission"/> equality) to keep AND-semantics
/// meaningful when inheritance causes the same attribute to appear twice.
/// </para>
/// </remarks>
public static class RequiredPermissionsCache {

	private static readonly ConcurrentDictionary<Type, IReadOnlyList<Permission>> Cache = new();
	private static readonly IReadOnlyList<Permission> Empty = [];

	private static readonly Type[] GrantedOpenGenerics = [
		typeof(IGrantedCommand<>),
		typeof(IGrantedCommand<,>),
		typeof(IGrantedRead<,>),
		typeof(IGrantedList<,>),
		typeof(IGrantedCacheableRead<,>),
	];

	/// <summary>
	/// Returns the distinct set of permissions declared on <paramref name="resourceType"/> via
	/// <see cref="RequiresPermissionAttribute"/>, computing and caching the result on first lookup.
	/// For granted resources, resolves name-only permissions from the TDomain's
	/// <see cref="GrantDomainAttribute"/> and validates namespace consistency.
	/// </summary>
	/// <param name="resourceType">The resource type to inspect.</param>
	/// <returns>
	/// The declared required permissions. Returns an empty list when no attributes are present.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when a name-only permission is used on a non-granted resource, or when an
	/// explicit permission namespace does not match the domain namespace.
	/// </exception>
	public static IReadOnlyList<Permission> GetFor(Type resourceType) {
		ArgumentNullException.ThrowIfNull(resourceType);
		return Cache.GetOrAdd(resourceType, static t => {
			var attrs = t.GetCustomAttributes<RequiresPermissionAttribute>(inherit: true).ToArray();
			if (attrs.Length == 0) {
				return Empty;
			}

			var domainNamespace = ResolveDomainNamespace(t);

			var seen = new HashSet<Permission>();
			List<Permission>? list = null;

			foreach (var attr in attrs) {
				Permission permission;

				if (attr.NeedsNamespaceResolution) {
					if (domainNamespace is null) {
						throw new InvalidOperationException(
							$"[RequiresPermission(\"{attr.UnresolvedName}\")] on '{t.Name}' uses the " +
							$"name-only form but the type does not implement a Granted interface with " +
							$"a [GrantDomain] marker. Use [RequiresPermission(\"namespace\", \"name\")] " +
							$"or add [GrantDomain] to the domain marker.");
					}
					permission = new Permission(domainNamespace, attr.UnresolvedName!);
					attr.Permission = permission;
				} else {
					permission = attr.Permission!;
					if (domainNamespace is not null &&
						!string.Equals(permission.Namespace, domainNamespace, StringComparison.OrdinalIgnoreCase)) {
						throw new InvalidOperationException(
							$"[RequiresPermission(\"{permission}\")] on '{t.Name}' uses namespace " +
							$"'{permission.Namespace}' which does not match the domain namespace " +
							$"'{domainNamespace}' from [GrantDomain]. All permissions on a granted " +
							$"resource must use the domain's namespace. Cross-cutting concerns " +
							$"belong in Stage 2 resource authorizers or Stage 3 policy validators.");
					}
				}

				if (seen.Add(permission)) {
					(list ??= []).Add(permission);
				}
			}

			return list is null ? Empty : list;
		});
	}

	/// <summary>
	/// Returns the distinct set of permissions declared on <typeparamref name="TResource"/>.
	/// </summary>
	public static IReadOnlyList<Permission> GetFor<TResource>() => GetFor(typeof(TResource));

	/// <summary>
	/// Resolves the domain permission namespace from the resource type's Granted interface
	/// hierarchy. Returns <see langword="null"/> when the type is not a granted resource.
	/// </summary>
	internal static string? ResolveDomainNamespace(Type resourceType) {
		foreach (var iface in resourceType.GetInterfaces()) {
			if (!iface.IsGenericType) {
				continue;
			}
			var def = iface.GetGenericTypeDefinition();
			for (var i = 0; i < GrantedOpenGenerics.Length; i++) {
				if (def == GrantedOpenGenerics[i]) {
					var domainType = iface.GetGenericArguments()[0];
					return GrantDomainCache.GetFor(domainType).Namespace;
				}
			}
		}
		return null;
	}
}
