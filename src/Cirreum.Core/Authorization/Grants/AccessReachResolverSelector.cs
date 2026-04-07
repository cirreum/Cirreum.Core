namespace Cirreum.Authorization.Grants;

using System.Collections.Concurrent;

/// <summary>
/// Per-scope selector that finds the <see cref="IAccessReachResolver"/> responsible for
/// a given resource type. With a single universal resolver this is a thin lookup;
/// the multi-resolver guard remains as a defensive check.
/// </summary>
/// <remarks>
/// <para>
/// The lookup is memoized per resource type. Selection calls
/// <see cref="IAccessReachResolver.Handles"/> on every registered resolver the first
/// time a resource type is seen; subsequent calls return the cached result.
/// </para>
/// </remarks>
public sealed class AccessReachResolverSelector {

	private readonly IReadOnlyList<IAccessReachResolver> _resolvers;
	private readonly ConcurrentDictionary<Type, IAccessReachResolver?> _cache = new();

	public AccessReachResolverSelector(IEnumerable<IAccessReachResolver> resolvers) {
		ArgumentNullException.ThrowIfNull(resolvers);
		this._resolvers = [.. resolvers];
	}

	/// <summary>
	/// Returns the resolver that handles <paramref name="resourceType"/>, or <see langword="null"/>
	/// when no resolver handles it (non-granted resources pass through).
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when more than one resolver handles <paramref name="resourceType"/>.
	/// </exception>
	public IAccessReachResolver? SelectFor(Type resourceType) {
		ArgumentNullException.ThrowIfNull(resourceType);
		return this._cache.GetOrAdd(resourceType, this.FindMatch);
	}

	private IAccessReachResolver? FindMatch(Type resourceType) {
		IAccessReachResolver? match = null;
		for (var i = 0; i < this._resolvers.Count; i++) {
			var candidate = this._resolvers[i];
			if (!candidate.Handles(resourceType)) {
				continue;
			}
			if (match is not null) {
				throw new InvalidOperationException(
					$"Multiple IAccessReachResolver instances handle resource type " +
					$"'{resourceType.FullName}': '{match.GetType().Name}' and '{candidate.GetType().Name}'. " +
					$"Only one IAccessReachResolver should be registered via AddAccessGrants<TResolver>().");
			}
			match = candidate;
		}
		return match;
	}
}
