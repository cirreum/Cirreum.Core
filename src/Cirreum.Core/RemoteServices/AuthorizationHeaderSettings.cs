namespace Cirreum.RemoteServices;

/// <summary>
/// Represents configuration settings for HTTP Authorization headers.
/// </summary>
/// <remarks>
/// This class contains properties to define both the authentication scheme (e.g., "Bearer", "Basic")
/// and its corresponding value for use in HTTP Authorization headers.
/// </remarks>
public class AuthorizationHeaderSettings {
	/// <summary>
	/// Gets or sets the authorization scheme to be used in the HTTP Authorization header.
	/// </summary>
	/// <value>
	/// The authentication scheme (e.g., "Bearer", "Basic"). Defaults to an empty string.
	/// </value>
	public string Scheme { get; set; } = "";

	/// <summary>
	/// Gets or sets the authorization value to be used in the HTTP Authorization header.
	/// </summary>
	/// <value>
	/// The authentication credentials or token value. Defaults to an empty string.
	/// </value>
	public string Value { get; set; } = "";

	/// <summary>
	/// Gets an <see cref="AuthorizationHeaderSettings"/> instance representing an anonymous authorization header.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Scheme = "Anonymous" and Value = ""
	/// </para>
	/// </remarks>
	public static AuthorizationHeaderSettings Anonymous { get; } = new AuthorizationHeaderSettings() {
		Scheme = "Anonymous",
		Value = ""
	};

}