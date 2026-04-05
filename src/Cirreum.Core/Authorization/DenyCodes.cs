namespace Cirreum.Authorization;

/// <summary>
/// Stable machine-readable codes for authorization denials.
/// Emitted on telemetry (<c>cirreum.authz.reason</c>) and in
/// <see cref="AuthorizationDenial.Code"/>.
/// </summary>
public static class DenyCodes {

	/// <summary>Caller is not authenticated.</summary>
	public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";

	/// <summary>Caller's application user is disabled.</summary>
	public const string UserDisabled = "USER_DISABLED";

	/// <summary>
	/// OwnerId is required on the resource but was not supplied. Emitted on Global-scope
	/// write attempts that did not name a target tenant.
	/// </summary>
	public const string OwnerIdRequired = "OWNER_ID_REQUIRED";

	/// <summary>
	/// OwnerId is required for cacheable owner-scoped reads but was not supplied.
	/// Emitted on Global-scope reads of <c>IAuthorizableOwnerScopedCacheableQuery&lt;T&gt;</c>
	/// that did not name a target tenant — required to keep the cache keyed per-tenant.
	/// </summary>
	public const string CacheableReadOwnerIdRequired = "CACHEABLE_READ_OWNER_ID_REQUIRED";

	/// <summary>Resource OwnerId does not match the caller's tenant.</summary>
	public const string OwnerIdMismatch = "OWNER_ID_MISMATCH";

	/// <summary>Tenant ID could not be resolved from the caller's context.</summary>
	public const string TenantUnresolvable = "TENANT_UNRESOLVABLE";

	/// <summary>Caller's access scope is not permitted for this resource.</summary>
	public const string ScopeNotPermitted = "SCOPE_NOT_PERMITTED";
}
