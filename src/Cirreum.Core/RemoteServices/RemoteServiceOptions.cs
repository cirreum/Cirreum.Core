namespace Cirreum.RemoteServices;

/// <summary>
/// Configured options for a given remote service.
/// </summary>
/// <remarks>
/// <para>
/// Uses the entry assembly's name as the <see cref="ApplicationName"/> if not provided by
/// the caller.
/// </para>
/// <para>
/// And by default, the "Authorization" is redacted from logging. See <see cref="RedactedHeaders"/>
/// </para>
/// </remarks>
public class RemoteServiceOptions : IEquatable<RemoteServiceOptions> {

	/// <summary>
	/// The default header to redact from logging. Default: ["Authorization"]
	/// </summary>
	public static readonly string[] DefaultRedactedHeaders = ["Authorization"];

	/// <summary>
	/// The header(s) to redact from logging. Default: RemoteServiceOptions.DefaultRedactedHeaders
	/// </summary>
	public readonly List<string> RedactedHeaders = [.. DefaultRedactedHeaders];

	/// <summary>
	/// Default constructor.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Uses the entry assembly's name as the application name.
	/// </para>
	/// </remarks>
	public RemoteServiceOptions() {
		var asm = System.Reflection.Assembly.GetEntryAssembly();
		this.ApplicationName = asm?.GetName()?.Name ?? "RemoteClient";
	}

	/// <summary>
	/// Default constructor.
	/// </summary>
	/// <param name="applicationName">The name of the client application making the call to the remote service.</param>
	public RemoteServiceOptions(string applicationName) {
		this.ApplicationName = applicationName;
	}
	/// <summary>
	/// Minimal constructor.
	/// </summary>
	/// <param name="applicationName">The name of the client application making the call to the remote service.</param>
	/// <param name="serviceUri">The base Uri for the remote service</param>
	public RemoteServiceOptions(string applicationName, Uri serviceUri) {
		this.ApplicationName = applicationName;
		this.ServiceUri = serviceUri;
	}

	/// <summary>
	/// Gets or sets the name of the client application making the call to the remote service.
	/// </summary>
	/// <remarks>
	/// If provided, will be forwarded in the <see cref="RemoteIdentityConstants.AppNameHeader"/>
	/// (X-Cirreum-App-Name) header. And will require your remote server, if using CORS, to allow the
	/// header
	/// </remarks>
	public string ApplicationName { get; set; } = "";
	/// <summary>
	/// Get or set the base Uri for the remote service
	/// </summary>
	/// <remarks>
	/// <para>
	/// This should be an absolute uri.
	/// </para>
	/// </remarks>
	public Uri ServiceUri { get; set; } = new("", UriKind.RelativeOrAbsolute);
	/// <summary>
	/// Optional Authorization Scopes for the remote service.
	/// </summary>
	public List<string> ServiceScopes { get; set; } = [];
	/// <summary>
	/// Gets or sets the optional Authorization Header scheme and value.
	/// </summary>
	public AuthorizationHeaderSettings? AuthorizationHeader { get; set; }

	/// <summary>
	/// Determines whether the specified <see cref="RemoteServiceOptions"/> is equal to the current instance.
	/// </summary>
	/// <param name="other">The <see cref="RemoteServiceOptions"/> to compare with the current instance.</param>
	/// <returns>
	/// <see langword="true"/> if the specified <see cref="RemoteServiceOptions"/> is equal to the current instance; 
	/// otherwise, <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// Two <see cref="RemoteServiceOptions"/> instances are considered equal if all their properties are equal.
	/// The <see cref="ServiceScopes"/> and <see cref="RedactedHeaders"/> collections are compared using case-insensitive 
	/// string comparison, but order is not considered (collections are treated as sets).
	/// </remarks>
	public bool Equals(RemoteServiceOptions? other) {
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return this.ApplicationName == other.ApplicationName &&
			   this.ServiceUri.Equals(other.ServiceUri) &&
			   this.ServiceScopes.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(other.ServiceScopes) &&
			   this.RedactedHeaders.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(other.RedactedHeaders) &&
			   Equals(this.AuthorizationHeader, other.AuthorizationHeader);
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current instance.
	/// </summary>
	/// <param name="obj">The object to compare with the current instance.</param>
	/// <returns>
	/// <see langword="true"/> if the specified object is equal to the current instance; 
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals(object? obj) {
		return this.Equals(obj as RemoteServiceOptions);
	}

	/// <summary>
	/// Returns a hash code for the current instance.
	/// </summary>
	/// <returns>
	/// A hash code for the current instance, suitable for use in hashing algorithms and data structures
	/// like a hash table.
	/// </returns>
	/// <remarks>
	/// The hash code is computed from all properties of the instance. For collections
	/// (<see cref="ServiceScopes"/> and <see cref="RedactedHeaders"/>), the hash code is order-independent
	/// and case-insensitive to match the equality semantics.
	/// </remarks>
	public override int GetHashCode() {
		var hash = new HashCode();
		hash.Add(this.ApplicationName);
		hash.Add(this.ServiceUri);
		hash.Add(this.AuthorizationHeader);

		// Use case-insensitive hash codes to match equality behavior
		var serviceScopesHash = this.ServiceScopes.Aggregate(0, (acc, scope) =>
			acc ^ (scope?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0));
		var redactedHeadersHash = this.RedactedHeaders.Aggregate(0, (acc, header) =>
			acc ^ (header?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0));

		hash.Add(serviceScopesHash);
		hash.Add(redactedHeadersHash);

		return hash.ToHashCode();
	}

	/// <summary>
	/// Determines whether two specified <see cref="RemoteServiceOptions"/> instances are equal.
	/// </summary>
	/// <param name="left">The first <see cref="RemoteServiceOptions"/> to compare.</param>
	/// <param name="right">The second <see cref="RemoteServiceOptions"/> to compare.</param>
	/// <returns>
	/// <see langword="true"/> if the two instances are equal; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator ==(RemoteServiceOptions? left, RemoteServiceOptions? right) {
		return Equals(left, right);
	}

	/// <summary>
	/// Determines whether two specified <see cref="RemoteServiceOptions"/> instances are not equal.
	/// </summary>
	/// <param name="left">The first <see cref="RemoteServiceOptions"/> to compare.</param>
	/// <param name="right">The second <see cref="RemoteServiceOptions"/> to compare.</param>
	/// <returns>
	/// <see langword="true"/> if the two instances are not equal; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator !=(RemoteServiceOptions? left, RemoteServiceOptions? right) {
		return !Equals(left, right);
	}
}