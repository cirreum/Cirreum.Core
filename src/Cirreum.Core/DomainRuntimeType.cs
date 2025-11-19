namespace Cirreum;

/// <summary>
/// The support runtime types.
/// </summary>
public enum DomainRuntimeType {
	/// <summary>
	/// A Blazor WASM client application.
	/// </summary>
	BlazorWasm,
	/// <summary>
	/// A MAUI Blazer Hybrid client application.
	/// </summary>
	MauiHybrid,
	/// <summary>
	/// A Console Client application - CLI apps, scripts, admin tools
	/// </summary>
	Console,
	/// <summary>
	/// A server that hosts a Web API (Jwt/Authorization)
	/// </summary>
	WebApi,
	/// <summary>
	/// A server that hosts a Web APP (OIDC/Authentication)
	/// </summary>
	WebApp,
	/// <summary>
	/// A serverless function application (Azure Functions, AWS Lambda, etc.)
	/// </summary>
	Function,
	/// <summary>
	/// A unit test runtime.
	/// </summary>
	UnitTest
}