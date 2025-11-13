namespace Cirreum.Security;
using System.Security.Claims;

/// <summary>
/// Anonymous ClaimsPrincipal.
/// </summary>
public sealed class AnonymousUser : ClaimsPrincipal {

	/// <summary>
	/// Default Anonymouse User Id (Guid)
	/// </summary>
	public const string AnonymousUserID = "0000000a-a00b-0000-bb1c-1ab000abcd99";

	/// <summary>
	/// Default Anonymouse User Name (Guest)
	/// </summary>
	public const string AnonymousUserName = "Guest";

	static AnonymousUser? _current;
	/// <summary>
	/// Gets the publicly shared AnonymousUser ClaimsPrincipal instance
	/// </summary>
	public static AnonymousUser Shared {
		get {
			_current ??= new AnonymousUser();
			return _current;
		}
	}

	/// <summary>
	/// Construct a new instance.
	/// </summary>
	public AnonymousUser() {

		// Create an anonymous ClaimsPrincipal with a default id and user name
		var claims = new[] {

			new Claim(ClaimTypes.Anonymous, "true", ClaimValueTypes.Boolean),

			new Claim("sub", AnonymousUserID),
			new Claim(ClaimTypes.NameIdentifier, AnonymousUserID),
			new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", AnonymousUserID),
			new Claim("oid", AnonymousUserID),

			new Claim(ClaimTypes.Name, AnonymousUserName),
			new Claim("name", AnonymousUserName),
			new Claim("preferred_username", AnonymousUserName),
			new Claim("upn", AnonymousUserName),
		};

		this._identity = new ClaimsIdentity(claims, null, "name", "roles");
		this.AddIdentity(this._identity);

	}

	private readonly ClaimsIdentity _identity;

	public override ClaimsIdentity Identity => this._identity;

}