namespace Cirreum.Authorization.Grants;

using System.Collections.Concurrent;

/// <summary>
/// Per-scope selector that matches a resource type to the single
/// <see cref="IAccessReachResolver"/> that claims it. Enforces the 1:1 contract by
/// throwing <see cref="InvalidOperationException"/> when two resolvers claim the same
/// resource type.
/// </summary>
/// <remarks>
/// <para>
/// The lookup is memoized per resource type. Selection calls
/// <see cref="IAccessReachResolver.Handles"/> on every registered resolver the first
/// time a resource type is seen; subsequent calls return the cached match (or miss).
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
	/// Returns the resolver that claims <paramref name="resourceType"/>, or <see langword="null"/>
	/// when no resolver claims it (the gate falls back to built-in single-owner policy).
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when more than one resolver claims <paramref name="resourceType"/>.
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
					$"Multiple IAccessReachResolver instances claim resource type " +
					$"'{resourceType.FullName}': '{match.GetType().Name}' and '{candidate.GetType().Name}'. " +
					$"Each owner-scoped resource type must be claimed by at most one resolver.");
			}
			match = candidate;
		}
		return match;
	}
}
