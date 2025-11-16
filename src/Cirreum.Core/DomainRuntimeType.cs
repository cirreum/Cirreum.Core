namespace Cirreum;

/// <summary>
/// The support runtime types.
/// </summary>
public enum DomainRuntimeType {
	/// <summary>
	/// A Blazor WASM client.
	/// </summary>
	BlazorWasm,
	/// <summary>
	/// A MAUI Blazer Hybrid client.
	/// </summary>
	MauiHybrid,
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
	Function,
	/// <summary>
	/// A unit test runtime.
	/// </summary>
	UnitTest
}