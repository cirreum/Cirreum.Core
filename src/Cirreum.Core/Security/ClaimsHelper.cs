namespace Cirreum.Security;

using System.Security.Claims;

/// <summary>
/// Helper class for working with identity claims across different identity providers.
/// </summary>
public static class ClaimsHelper {

	static class EntraClaimTypes {
		public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";
		public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
		public const string IdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";
	}

	/// <summary>
	/// Detects the identity provider based on the issuer claim from the <see cref="ClaimsPrincipal"/>.
	/// </summary>
	public static IdentityProviderType ResolveProvider(ClaimsPrincipal principal) {
		ArgumentNullException.ThrowIfNull(principal);

		// Handle AnonymousUser specifically
		if (principal == AnonymousUser.Shared ||
			principal.FindFirst(ClaimTypes.Anonymous)?.Value == "true") {
			return IdentityProviderType.None;
		}

		var issuer = principal.FindFirst("iss")?.Value?.ToLowerInvariant();
		if (string.IsNullOrEmpty(issuer)) {
			return IdentityProviderType.Unknown;
		}

		return ResolveProviderFromIssuer(issuer);

	}
	/// <summary>
	/// Detects the identity provider based on the issuer claim from the <see cref="ClaimsIdentity"/>.
	/// </summary>
	public static IdentityProviderType ResolveProvider(ClaimsIdentity identity) {
		ArgumentNullException.ThrowIfNull(identity);

		// Handle AnonymousUser specifically
		if (identity.FindFirst(ClaimTypes.Anonymous)?.Value == "true") {
			return IdentityProviderType.None;
		}

		var issuer = identity.FindFirst("iss")?.Value?.ToLowerInvariant();
		if (string.IsNullOrEmpty(issuer)) {
			return IdentityProviderType.Unknown;
		}

		return ResolveProviderFromIssuer(issuer);

	}
	private static IdentityProviderType ResolveProviderFromIssuer(string issuer) {
		return issuer switch {
			// Microsoft Entra ID (formerly Azure AD)
			var i when i.Contains("login.microsoftonline.com") => IdentityProviderType.Entra,

			// Microsoft Entra External ID (formerly Azure B2C)
			var i when i.Contains("ciamlogin.com") => IdentityProviderType.EntraExt,

			// Auth0
			var i when i.Contains("auth0.com") => IdentityProviderType.Auth0,

			// Okta
			var i when i.Contains("okta.com") || i.Contains("oktapreview.com") => IdentityProviderType.Okta,

			// Ping Identity
			var i when i.Contains("pingidentity.com") || i.Contains("ping-eng.com") => IdentityProviderType.PingIdentity,

			// Descope
			var i when i.Contains("descope.com") || i.Contains("descope.io") => IdentityProviderType.Descope,

			// Keycloak
			var i when i.Contains("auth.keycloak.org") || i.Contains("/auth/realms/") => IdentityProviderType.Keycloak,

			// Akamai Identity Cloud
			var i when i.Contains("secureidentity.akamai.com") || i.Contains("identity.akamai.com") => IdentityProviderType.Akamai,

			// Authlete
			var i when i.Contains("authlete.com") || i.Contains("authlete.net") => IdentityProviderType.Authlete,

			// IBM Security Verify (formerly IBM Cloud Identity)
			var i when i.Contains("verify.ibm.com") || i.Contains("cloudidentity.com") => IdentityProviderType.IBM,

			// Duende IdentityServer
			var i when i.Contains("duendesoftware.com") ||
					 i.Contains("identityserver.io") ||
					 i.Contains("identityserver4.io") => IdentityProviderType.Duende,

			// Google
			var i when i.Contains("accounts.google.com") => IdentityProviderType.Google,

			// Facebook
			var i when i.Contains("facebook.com") || i.Contains("graph.facebook.com") => IdentityProviderType.Facebook,

			// Apple
			var i when i.Contains("appleid.apple.com") => IdentityProviderType.Apple,

			// GitHub
			var i when i.Contains("github.com") => IdentityProviderType.GitHub,

			// LinkedIn
			var i when i.Contains("linkedin.com") => IdentityProviderType.LinkedIn,

			// Salesforce
			var i when i.Contains("salesforce.com") || i.Contains("force.com") => IdentityProviderType.Salesforce,

			// Twitter (X)
			var i when i.Contains("twitter.com") || i.Contains("api.twitter.com") => IdentityProviderType.Twitter,

			// Slack
			var i when i.Contains("slack.com") => IdentityProviderType.Slack,

			// AWS Cognito
			var i when i.Contains("cognito-idp") || i.Contains("amazonaws.com") => IdentityProviderType.AWS_Cognito,

			// Default case for unknown providers
			_ => IdentityProviderType.Unknown
		};
	}

	/// <summary>
	/// Resolves the most stable user identifier from the principal's claims.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Resolution order (first non-null wins):
	/// </para>
	/// <list type="number">
	///   <item><description><c>oid</c> — Entra ID object identifier (tenant-wide, stable across apps).</description></item>
	///   <item><description><c>http://schemas.microsoft.com/identity/claims/objectidentifier</c> — long-form Entra OID claim.</description></item>
	///   <item><description><c>sub</c> — OIDC subject identifier. May be pairwise (per-app) with some providers.</description></item>
	///   <item><description><c>http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier</c> —
	///     ASP.NET's <see cref="ClaimTypes.NameIdentifier"/>. The OIDC middleware maps <c>sub</c>
	///     to this URI by default when <c>MapInboundClaims</c> is enabled.</description></item>
	///   <item><description><c>user_id</c> — Firebase / custom IdP fallback.</description></item>
	/// </list>
	/// <para>
	/// For Entra-backed applications, <c>oid</c> is preferred over <c>sub</c> because Entra's
	/// subject claim can vary per application (pairwise), while <c>oid</c> is consistent across
	/// all applications within the same tenant.
	/// </para>
	/// </remarks>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The resolved identifier, or <see langword="null"/> if no matching claim is found.</returns>
	public static string? ResolveId(ClaimsPrincipal principal) =>
		principal.FindFirst("oid")?.Value
		?? principal.FindFirst(EntraClaimTypes.ObjectId)?.Value
		?? principal.FindFirst("sub")?.Value
		?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
		?? principal.FindFirst("user_id")?.Value;

	/// <inheritdoc cref="ResolveId(ClaimsPrincipal)"/>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	public static string? ResolveId(ClaimsIdentity identity) =>
		identity.FindFirst("oid")?.Value
		?? identity.FindFirst(EntraClaimTypes.ObjectId)?.Value
		?? identity.FindFirst("sub")?.Value
		?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
		?? identity.FindFirst("user_id")?.Value;

	/// <summary>
	/// Attempt to determine the User Name of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Name if resolved otherwise null.</returns>
	public static string? ResolveName(ClaimsPrincipal principal) {

		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveName(identity);
		}

		var name = principal.FindFirst("name")?.Value;
		if (name.HasValue()) {
			return name;
		}

		name = principal.Identity?.Name;
		if (name.HasValue()) {
			return name;
		}

		return name;

	}
	/// <summary>
	/// Attempt to determine the User Name of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Name if resolved otherwise null.</returns>
	public static string? ResolveName(ClaimsIdentity identity) {

		var name = identity.FindFirst("name")?.Value;
		if (name.HasValue()) {
			return name;
		}

		name = identity.Name;
		if (name.HasValue()) {
			return name;
		}

		name = identity.FindFirst(identity.NameClaimType)?.Value;
		if (name.HasValue()) {
			return name;
		}

		return name;

	}


	/// <summary>
	/// Attempt to determine the Roles of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Roles.</returns>
	public static List<string> ResolveRoles(ClaimsPrincipal principal) {

		// Collect into a HashSet for uniqueness, then return a List
		var set = new HashSet<string>(RoleComparer);

		if (principal.Identities.Any()) {
			foreach (var id in principal.Identities.OfType<ClaimsIdentity>()) {
				AddRolesFromIdentity(id, set);
			}
		} else {
			// Fallback: look for common role claim names on the principal directly
			foreach (var c in principal.Claims) {
				if (IsRoleClaim(c, roleClaimType: null)) {
					set.Add(c.Value);
				}
			}
		}

		return [.. set];

	}
	/// <summary>
	/// Attempt to determine the Roles of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Roles.</returns>
	public static List<string> ResolveRoles(ClaimsIdentity identity) {
		var set = new HashSet<string>(RoleComparer);
		AddRolesFromIdentity(identity, set);
		return [.. set];
	}

	// Use OrdinalIgnoreCase if that's your policy across the app
	private static readonly StringComparer RoleComparer = StringComparer.OrdinalIgnoreCase;
	private static void AddRolesFromIdentity(ClaimsIdentity identity, HashSet<string> sink) {
		var roleClaimType = identity.RoleClaimType; // often ClaimTypes.Role
		foreach (var c in identity.Claims) {
			if (IsRoleClaim(c, roleClaimType)) {
				sink.Add(c.Value);
			}
		}
	}
	private static bool IsRoleClaim(Claim c, string? roleClaimType)
		=> c.Type == roleClaimType
		|| c.Type == "roles"
		|| c.Type == "role";

	/// <summary>
	/// Attempt to determine the Oid of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Oid if resolved otheriwise null.</returns>
	public static string? ResolveOid(ClaimsPrincipal principal) {

		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveOid(identity);
		}

		var oid = principal.FindFirst("oid")?.Value;
		if (oid.HasValue()) {
			return oid;
		}

		oid = principal.FindFirst(EntraClaimTypes.ObjectId)?.Value;
		if (oid.HasValue()) {
			return oid;
		}

		return oid;

	}
	/// <summary>
	/// Attempt to determine the Oid of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Oid if resolved otheriwise null.</returns>
	public static string? ResolveOid(ClaimsIdentity identity) {

		var oid = identity.FindFirst("oid")?.Value;
		if (oid.HasValue()) {
			return oid;
		}

		oid = identity.FindFirst(EntraClaimTypes.ObjectId)?.Value;
		if (oid.HasValue()) {
			return oid;
		}

		return oid;

	}

	/// <summary>
	/// Attempt to determine the Tid of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Tid if resolved otheriwise null.</returns>
	public static string? ResolveTid(ClaimsPrincipal principal) {

		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveTid(identity);
		}

		var tid = principal.FindFirst("tid")?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		tid = principal.FindFirst(EntraClaimTypes.TenantId)?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		tid = principal.FindFirst("org")?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		tid = principal.FindFirst("org_id")?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		return tid;

	}
	/// <summary>
	/// Attempt to determine the Tid of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Tid if resolved otheriwise null.</returns>
	public static string? ResolveTid(ClaimsIdentity identity) {

		var tid = identity.FindFirst("tid")?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		tid = identity.FindFirst(EntraClaimTypes.TenantId)?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		tid = identity.FindFirst("org")?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		tid = identity.FindFirst("org_id")?.Value;
		if (tid.HasValue()) {
			return tid;
		}

		return tid;

	}

}