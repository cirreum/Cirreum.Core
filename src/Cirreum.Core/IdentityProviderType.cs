namespace Cirreum;

/// <summary>
/// Represents supported identity providers with their specific claim mappings.
/// </summary>
/// <remarks>
/// These providers implement OpenID Connect (OIDC) or OAuth 2.0 protocols.
/// This enum is used to identify which identity provider is configured for authentication.
/// </remarks>
public enum IdentityProviderType {
	/// <summary>
	/// No identity provider - used for anonymous access.
	/// </summary>
	None = 0,

	/// <summary>
	/// Microsoft Entra ID (formerly Azure Active Directory).
	/// </summary>
	Entra,

	/// <summary>
	/// Microsoft Entra External ID (for external users).
	/// </summary>
	EntraExt,

	/// <summary>
	/// Okta identity provider.
	/// </summary>
	Okta,

	/// <summary>
	/// Keycloak identity provider.
	/// </summary>
	Keycloak,

	/// <summary>
	/// Descope identity provider.
	/// </summary>
	Descope,

	/// <summary>
	/// Ping Identity provider.
	/// </summary>
	PingIdentity,

	/// <summary>
	/// Akamai Identity Cloud.
	/// </summary>
	Akamai,

	/// <summary>
	/// Auth0 identity provider.
	/// </summary>
	Auth0,

	/// <summary>
	/// Authlete identity provider.
	/// </summary>
	Authlete,

	/// <summary>
	/// IBM identity provider.
	/// </summary>
	IBM,

	/// <summary>
	/// Duende IdentityServer (self-hosted OIDC server).
	/// </summary>
	Duende,

	/// <summary>
	/// Google identity provider.
	/// </summary>
	Google,

	/// <summary>
	/// Facebook identity provider.
	/// </summary>
	Facebook,

	/// <summary>
	/// Apple identity provider (Sign in with Apple).
	/// </summary>
	Apple,

	/// <summary>
	/// GitHub identity provider.
	/// </summary>
	GitHub,

	/// <summary>
	/// LinkedIn identity provider.
	/// </summary>
	LinkedIn,

	/// <summary>
	/// Salesforce identity provider.
	/// </summary>
	Salesforce,

	/// <summary>
	/// Twitter (X) identity provider.
	/// </summary>
	Twitter,

	/// <summary>
	/// Slack identity provider.
	/// </summary>
	Slack,

	/// <summary>
	/// AWS Cognito identity provider.
	/// </summary>
	AWS_Cognito,

	/// <summary>
	/// Unknown or unsupported identity provider.
	/// </summary>
	Unknown
}