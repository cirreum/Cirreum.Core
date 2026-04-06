namespace Cirreum.Authorization.Grants.Caching;

/// <summary>
/// Deterministic key and tag composition for the reach cache. Cache keys encode the
/// version, caller, domain namespace, and sorted permission signature so that two
/// actions with the same permission set share a cache entry.
/// </summary>
/// <remarks>
/// Key format: <c>reach:v{version}:{callerId}:{domain}:{permissionSignature}</c>
/// <para>
/// Examples:
/// <c>reach:v1:user-123:issues:delete</c>,
/// <c>reach:v1:user-123:issues:delete+write</c>
/// </para>
/// </remarks>
internal static class ReachCacheKeys {

	/// <summary>
	/// Builds the full L2 cache key from the caller, domain, and resolved permissions.
	/// </summary>
	internal static string BuildKey(
		int version,
		string callerId,
		string domain,
		IReadOnlyList<Permission> permissions) {

		var sig = BuildPermissionSignature(permissions);
		return $"reach:v{version}:{callerId}:{domain}:{sig}";
	}

	/// <summary>
	/// Builds a sorted, <c>+</c>-joined signature from permission names.
	/// </summary>
	/// <remarks>
	/// Sorting is required for cache correctness — permissions use AND semantics
	/// (caller must hold all), so evaluation order is irrelevant, but two commands
	/// requiring the same set in different declaration order must produce the same
	/// cache key. Without sorting, <c>["delete","archive"]</c> and
	/// <c>["archive","delete"]</c> would miss each other's cache entries.
	/// </remarks>
	internal static string BuildPermissionSignature(IReadOnlyList<Permission> permissions) {
		if (permissions.Count == 0) {
			return string.Empty;
		}
		if (permissions.Count == 1) {
			return permissions[0].Name;
		}

		var names = new string[permissions.Count];
		for (var i = 0; i < permissions.Count; i++) {
			names[i] = permissions[i].Name;
		}

		//
		// Sort in-place with a stable, ordinal comparer for deterministic keys across cultures.
		// Sorting is required for cache correctness
		//
		Array.Sort(names, StringComparer.Ordinal);

		return string.Join('+', names);
	}

	/// <summary>
	/// Builds the tag set for a cache entry. Used for invalidation.
	/// </summary>
	internal static string[] BuildTags(string callerId, string domain) =>
		[CallerTag(callerId), DomainTag(domain)];

	/// <summary>
	/// Tag for invalidating all entries for a specific caller.
	/// </summary>
	internal static string CallerTag(string callerId) =>
		$"reach:caller:{callerId}";

	/// <summary>
	/// Tag for invalidating all entries for a specific domain.
	/// </summary>
	internal static string DomainTag(string domain) =>
		$"reach:domain:{domain}";
}
