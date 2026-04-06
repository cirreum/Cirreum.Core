namespace Cirreum.Authorization.Grants;

using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Per-type cache of <see cref="GrantDomainAttribute"/> lookups. Reads the attribute from
/// a TDomain marker interface once and memoizes the result. Throws
/// <see cref="InvalidOperationException"/> when the attribute is missing.
/// </summary>
internal static class GrantDomainCache {

	private static readonly ConcurrentDictionary<Type, GrantDomainAttribute> Cache = new();

	/// <summary>
	/// Returns the <see cref="GrantDomainAttribute"/> for the given domain marker type.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <paramref name="domainType"/> does not have <c>[GrantDomain]</c> applied.
	/// </exception>
	internal static GrantDomainAttribute GetFor(Type domainType) {
		ArgumentNullException.ThrowIfNull(domainType);
		return Cache.GetOrAdd(domainType, static t => {
			var attr = t.GetCustomAttribute<GrantDomainAttribute>();
			if (attr is null) {
				throw new InvalidOperationException(
					$"Domain marker '{t.Name}' is missing [GrantDomain]. " +
					$"Apply [GrantDomain(\"namespace\")] to define its permission namespace.");
			}
			return attr;
		});
	}

	/// <summary>
	/// Returns the <see cref="GrantDomainAttribute"/> for <typeparamref name="TDomain"/>.
	/// </summary>
	internal static GrantDomainAttribute GetFor<TDomain>() => GetFor(typeof(TDomain));

	/// <summary>
	/// Returns the <see cref="GrantDomainAttribute"/> for the given domain marker type,
	/// or <see langword="null"/> when the attribute is not present.
	/// </summary>
	/// <remarks>
	/// Safe variant for discovery/analysis scenarios where the type may not be a
	/// grant domain marker. Does not throw.
	/// </remarks>
	internal static GrantDomainAttribute? TryGetFor(Type domainType) {
		ArgumentNullException.ThrowIfNull(domainType);
		if (Cache.TryGetValue(domainType, out var cached)) {
			return cached;
		}
		var attr = domainType.GetCustomAttribute<GrantDomainAttribute>();
		if (attr is not null) {
			Cache.TryAdd(domainType, attr);
		}
		return attr;
	}
}
