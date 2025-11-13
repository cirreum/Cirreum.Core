namespace Cirreum;

using System.ComponentModel;

/// <summary>
/// The supported authentication library types for use with Blazor javascript initialization.
/// </summary>
/// <remarks>
/// See <see cref="IdentityProviderType"/> for various supported implementations.
/// </remarks>
public enum AuthenticationLibraryType {
	/// <summary>
	/// No authentication library configured (anonymous only)
	/// </summary>
	None = 0,
	/// <summary>
	/// Microsoft Authentication Library
	/// </summary>
	[Description("msal")]
	MSAL = 1,
	/// <summary>
	/// Standard OpenID Connect Library
	/// </summary>
	[Description("oidc")]
	OIDC = 2
}