namespace Cirreum.RemoteServices;

/// <summary>
/// Represents configuration settings for HTTP Authorization headers.
/// </summary>
/// <remarks>
/// <para>
/// This class contains properties to define both the authentication scheme (e.g., "Bearer", "Basic")
/// and its corresponding value for use in HTTP Authorization headers.
/// </para>
/// <para>
/// Use <see cref="None"/> to explicitly indicate that no authorization header should be sent,
/// bypassing any default OIDC/OAuth token acquisition.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Pre-shared bearer token
/// options.AuthorizationHeader = new AuthorizationHeaderSettings {
///     Scheme = "Bearer",
///     Value = "my-api-token"
/// };
/// 
/// // No authorization (public API)
/// options.AuthorizationHeader = AuthorizationHeaderSettings.None;
/// </code>
/// </example>
public class AuthorizationHeaderSettings {

	/// <summary>
	/// Gets or sets the authorization scheme to be used in the HTTP Authorization header.
	/// </summary>
	/// <value>
	/// The authentication scheme (e.g., "Bearer", "Basic"). Defaults to an empty string.
	/// </value>
	/// <remarks>
	/// Common schemes include "Bearer" for token-based auth and "Basic" for base64-encoded credentials.
	/// See <see href="https://www.iana.org/assignments/http-authschemes/http-authschemes.xhtml">IANA HTTP Authentication Scheme Registry</see>
	/// for the full list of registered schemes.
	/// </remarks>
	public string Scheme { get; set; } = "";

	/// <summary>
	/// Gets or sets the authorization value to be used in the HTTP Authorization header.
	/// </summary>
	/// <value>
	/// The authentication credentials or token value. Defaults to an empty string.
	/// </value>
	public string Value { get; set; } = "";

	/// <summary>
	/// Gets a value indicating whether this instance has a non-empty authorization value.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Value"/> is not null or empty; otherwise, <see langword="false"/>.
	/// </value>
	public bool HasValue => !string.IsNullOrEmpty(this.Value);

	/// <summary>
	/// Gets an <see cref="AuthorizationHeaderSettings"/> instance representing no authorization.
	/// </summary>
	/// <value>
	/// A singleton instance with empty <see cref="Scheme"/> and <see cref="Value"/>.
	/// </value>
	/// <remarks>
	/// Use this to explicitly indicate that no authorization header should be sent.
	/// When set on <see cref="RemoteServiceOptions.AuthorizationHeader"/>, this bypasses
	/// the default OIDC/OAuth token acquisition via <c>AuthorizationMessageHandler</c>.
	/// </remarks>
	public static AuthorizationHeaderSettings None { get; } = new();

}