namespace Cirreum.RemoteServices;

/// <summary>
/// Constants used for identifying and authenticating remote services and clients,
/// including HTTP headers and claim type identifiers.
/// </summary>
public static class RemoteIdentityConstants {

	/// <summary>
	/// The HTTP header name that contains the application name
	/// of the confidential client making the request.
	/// </summary>
	public const string AppNameHeader = "X-Cirreum-App-Name";

	/// <summary>
	/// The claim type URI for storing the application name
	/// of the confidential client in the identity context.
	/// </summary>
	public const string AppNameClaimType = "http://corracing.com/identity/claims/2025/application/name";

}