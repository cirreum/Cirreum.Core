namespace Cirreum;

/// <summary>
/// The support runtime types.
/// </summary>
public enum ApplicationRuntimeType {
	/// <summary>
	/// A Spa base front-end such as Blazor, React etc.
	/// </summary>
	Spa,
	/// <summary>
	/// A server that hosts a Web API (Jwt/Authorization)
	/// </summary>
	WebApi,
	/// <summary>
	/// A server that hosts a Web APP (OIDC/Authentication)
	/// </summary>
	WebApp,
	/// <summary>
	/// A daemon client application
	/// </summary>
	Function
}