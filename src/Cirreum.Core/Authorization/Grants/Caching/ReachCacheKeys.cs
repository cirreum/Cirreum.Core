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
		PermissionSet permissions) =>
		$"reach:v{version}:{callerId}:{domain}:{permissions.ToSignature()}";

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
