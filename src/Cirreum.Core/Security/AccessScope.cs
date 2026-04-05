namespace Cirreum.Security;

/// <summary>
/// The coarse authorization dimension describing which IdP scope authenticated the caller.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Global"/> callers authenticated via the application operator's own IdP
/// (the designated <c>PrimaryScheme</c> in authorization configuration). These are typically
/// operator staff acting across tenants on behalf of the platform.
/// </para>
/// <para>
/// <see cref="Tenant"/> callers authenticated via a customer/tenant IdP scheme — Entra
/// External ID, BYOID, per-customer OIDC, API keys, signed requests, etc. These are
/// consumers of the platform scoped to a single tenant.
/// </para>
/// <para>
/// <see cref="None"/> is used for anonymous callers and for platforms that have not
/// registered an <see cref="IAccessScopeResolver"/> (in which case scope is unknown).
/// </para>
/// </remarks>
public enum AccessScope {

	/// <summary>
	/// No access scope has been resolved — the caller is anonymous, or no
	/// <see cref="IAccessScopeResolver"/> is registered in the host.
	/// </summary>
	None = 0,

	/// <summary>
	/// The caller authenticated via the application operator's own IdP (the
	/// configured <c>PrimaryScheme</c>). Typically cross-tenant operator staff.
	/// </summary>
	Global,

	/// <summary>
	/// The caller authenticated via a customer/tenant IdP scheme that is not the
	/// <c>PrimaryScheme</c>. Typically end-customer users of the platform.
	/// </summary>
	Tenant
}
