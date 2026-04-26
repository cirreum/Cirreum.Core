namespace Cirreum.Authorization;

using Cirreum.Authorization.Operations.Grants;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Per-type cache of <see cref="RequiresGrantAttribute"/> declarations. Reads all
/// <c>[RequiresGrant]</c> attributes on an authorizable object type (including stacked and inherited
/// declarations) exactly once, memoizes the result, and returns the same immutable list on
/// every subsequent lookup.
/// </summary>
/// <remarks>
/// <para>
/// The cache is consulted by the authorization pipeline setup to hoist required grant permissions
/// onto <see cref="AuthorizationContext{TAuthorizableObject}.RequiredGrants"/>. Stage 1 (the
/// grant gate) enforces them; Stage 2 resource authorizers and Stage 3 policy validators may
/// inspect the same set as read-only context.
/// </para>
/// <para>
/// For granted authorizable objects (those implementing <see cref="IGrantableMutateBase"/>,
/// <see cref="IGrantableLookupBase"/>, <see cref="IGrantableSearchBase"/>), this cache also:
/// </para>
/// <list type="bullet">
///   <item><description>Resolves name-only <c>[RequiresGrant("delete")]</c> attributes
///   by deriving the domain feature from the authorizable object type's namespace convention via
///   <see cref="DomainFeatureResolver"/>.</description></item>
///   <item><description>Validates that all explicit permission features match the domain
///   feature — cross-feature permissions are a misconfiguration and cause a runtime
///   error.</description></item>
/// </list>
/// <para>
/// Permissions are deduplicated (by <see cref="Permission"/> equality) to keep AND-semantics
/// meaningful when inheritance causes the same attribute to appear twice.
/// </para>
/// </remarks>
public static class RequiredGrantCache {

	private static readonly ConcurrentDictionary<Type, PermissionSet> Cache = new();

	/// <summary>
	/// Returns the distinct set of grant permissions declared on <paramref name="resourceType"/> via
	/// <see cref="RequiresGrantAttribute"/>, computing and caching the result on first lookup.
	/// For granted authorizable objects, resolves name-only permissions from the type's namespace convention
	/// and validates feature consistency.
	/// </summary>
	/// <param name="resourceType">The authorizable object type to inspect.</param>
	/// <returns>
	/// The declared required grant permissions. Returns an empty set when no attributes are present.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when a name-only permission is used on a type without a <c>*.Domain.*</c>
	/// namespace, or when an explicit permission feature does not match the domain feature.
	/// </exception>
	public static PermissionSet GetFor(Type resourceType) {
		ArgumentNullException.ThrowIfNull(resourceType);
		return Cache.GetOrAdd(resourceType, static t => {
			var attrs = t.GetCustomAttributes<RequiresGrantAttribute>(inherit: true).ToArray();
			if (attrs.Length == 0) {
				return PermissionSet.Empty;
			}

			var domainFeature = DomainFeatureResolver.Resolve(t);

			var seen = new HashSet<Permission>();
			List<Permission>? list = null;

			foreach (var attr in attrs) {
				Permission permission;

				if (attr.NeedsFeatureResolution) {
					if (domainFeature is null) {
						throw new InvalidOperationException(
							$"[RequiresGrant(\"{attr.UnresolvedOperation}\")] on '{t.Name}' uses the " +
							$"operation-only form but the type's namespace does not follow the " +
							$"*.Domain.* convention required for feature resolution. Use " +
							$"[RequiresGrant(\"feature\", \"operation\")] with an explicit feature.");
					}
					permission = new Permission(domainFeature, attr.UnresolvedOperation!);
					attr.Permission = permission;
				} else {
					permission = attr.Permission!;
					if (domainFeature is not null &&
						!string.Equals(permission.Feature, domainFeature, StringComparison.OrdinalIgnoreCase)) {
						throw new InvalidOperationException(
							$"[RequiresGrant(\"{permission}\")] on '{t.Name}' uses feature " +
							$"'{permission.Feature}' which does not match the domain feature " +
							$"'{domainFeature}'. All grant requirements on a granted " +
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
	/// Returns the distinct set of grant permissions declared on <typeparamref name="TAuthorizableObject"/>.
	/// </summary>
	public static PermissionSet GetFor<TAuthorizableObject>() => GetFor(typeof(TAuthorizableObject));

	/// <summary>
	/// Resolves the domain feature from the authorizable object type's namespace convention.
	/// Returns <see langword="null"/> when the type does not follow the convention.
	/// </summary>
	internal static string? ResolveDomainFeature(Type resourceType) =>
		DomainFeatureResolver.Resolve(resourceType);
}
