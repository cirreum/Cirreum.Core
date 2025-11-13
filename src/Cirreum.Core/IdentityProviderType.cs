namespace Cirreum;

/// <summary>
/// Represents supported identity providers with their specific claim mappings.
/// </summary>
public enum IdentityProviderType {
	/// <summary>
	/// No identity provider - used for anonymous access
	/// </summary>
	None = 0,

	Entra,
	EntraExt,
	Okta,
	Keycloak,
	Descope,
	PingIdentity,
	Akamai,
	Auth0,
	Authlete,
	IBM,
	Duende,
	Google,
	Facebook,
	Unknown
}