namespace Cirreum;

/// <summary>
/// The support runtime types.
/// </summary>
public enum ApplicationRuntimeType {
	/// <summary>
	/// A Client such as Blazor WASM.
	/// </summary>
	Client,
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