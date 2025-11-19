namespace Cirreum.RemoteServices;

/// <summary>
/// Constants for identifying and authenticating remote services and clients,
/// including HTTP headers and claim type identifiers.
/// </summary>
public static class RemoteIdentityConstants {
	/// <summary>
	/// HTTP header name containing the application name of the confidential client making the request.
	/// </summary>
	public const string AppNameHeader = "X-Cirreum-App-Name";

	/// <summary>
	/// Claim type URI for the application name of the confidential client in the identity context.
	/// </summary>
	public const string AppNameClaimType = "http://github.com/cirreum/identity/claims/2025/application/name";
}