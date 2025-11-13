namespace Cirreum;

using Cirreum.Security;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json.Serialization;

/// <summary>
/// Represents comprehensive user profile information following OIDC standards, optional fields
/// for EntraID, Enterprise attributes, Organization details and a dictionary for unspecified
/// attributes or claims.
/// </summary>
public sealed class UserProfile {

	/// <summary>
	/// Indicates if the profile has been enriched.
	/// </summary>
	[JsonIgnore] public bool IsEnriched { get; internal set; }

	//
	// Required properties and access/authorization information
	//

	/// <summary>
	/// Whatever this means to the system it came from. Not to be confused with <see cref="Oid"/>
	/// </summary>
	[JsonInclude] public string Id { get; private set; } = AnonymousUser.AnonymousUserID;
	/// <summary>
	/// Whatever this means to the system it came from. Not to be confused with <see cref="DisplayName"/>
	/// or <see cref="PreferredUserName"/>
	/// </summary>
	[JsonInclude] public string Name { get; private set; } = AnonymousUser.AnonymousUserName;
	/// <summary>
	/// The collection of roles assigned to the user of the application.
	/// </summary>
	[JsonIgnore]
	public ImmutableHashSet<string> Roles
		=> (this.RolesRaw ?? []).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
	[JsonInclude]
	private List<string>? RolesRaw { get; set; } = [];

	/// <summary>
	/// A predetermined and known provider see <see cref="IdentityProviderType"/>.
	/// </summary>
	[JsonInclude] public IdentityProviderType Provider { get; private set; } = IdentityProviderType.None;

	//
	// Core Profile Claims (profile scope)
	//

	public string? GivenName { get; set; }
	public string? FamilyName { get; set; }
	public string? MiddleName { get; set; }
	public string? Nickname { get; set; }
	/// <summary>
	/// Url to the user's profile picture
	/// </summary>
	public string Picture { get; set; } = "/assets/images/guest-user-icon.svg";
	public string Birthdate { get; set; } = DateTimeOffset.MinValue.ToString();
	public string TimeZone { get; set; } = string.Empty;
	public string DateFormat { get; set; } = string.Empty;
	public string TimeFormat { get; set; } = string.Empty;
	public string Locale { get; set; } = string.Empty;


	// Email Claims (email scope)
	/// <summary>
	/// The configured user email address.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This value isn't guaranteed to be correct and is mutable over time. Never use it for authorization
	/// or to save data for a user.
	/// </para>
	/// </remarks>
	public string? Email { get; set; }
	/// <summary>
	/// Is the <see cref="Email"/> verified.
	/// </summary>
	public bool? EmailVerified { get; set; }

	// Phone Claims (phone scope)
	/// <summary>
	/// The users associated phone number.
	/// </summary>
	public string? PhoneNumber { get; set; }
	/// <summary>
	/// Is the <see cref="PhoneNumber"/> verified.
	/// </summary>
	public bool? PhoneNumberVerified { get; set; }
	/// <summary>
	/// Additional phone numbers
	/// </summary>
	public List<string>? PhoneNumbers { get; set; } = [];

	// Address Claims (address scope)
	public UserProfileAddress? Address { get; set; }

	// Organizations Claims (tenant)
	public UserProfileOrganization Organization { get; set; } = UserProfileOrganization.Empty;

	// Extended Enterprise Claims
	public string? Company { get; set; }
	public string? Department { get; set; }
	public string? JobTitle { get; set; }
	public string? EmployeeId { get; set; }
	public string? EmployeeType { get; set; }
	public string? OfficeLocation { get; set; }
	public string? Manager { get; set; }
	public string? CostCenter { get; set; }
	public string? Division { get; set; }

	// EntraID-specific identifiers
	/// <summary>
	/// The Oid value, typically from EntraID (formally Azure AD)
	/// </summary>
	/// <remarks>
	/// <para>
	/// The immutable identifier for an object, in this case, a user account. This ID uniquely identifies the
	/// user across applications - two different applications signing in the same user receives the same value
	/// in the oid claim.
	/// </para>
	/// <para>
	/// Microsoft Graph returns this ID as the id property for a user account. Because the oid allows multiple
	/// apps to correlate users, the profile scope is required to receive this claim.
	/// </para>
	/// <para>
	/// If a single user exists in multiple tenants, the user contains a different object ID in each tenant - 
	/// they're considered different accounts, even though the user logs into each account with the same
	/// credentials. The oid claim is a GUID and can't be reused.
	/// </para>
	/// </remarks>
	public string? Oid { get; set; }
	/// <summary>
	/// The user principal name (UPN) of the user. Typically from EntraID (formally Azure AD) and
	/// usually is the same as <see cref="PreferredUserName"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The UPN is an Internet-style sign-in name for the user based on the Internet standard RFC 822. By
	/// convention, this value should map to the user's email name. The general format is alias@domain, where
	/// the domain must be present in the tenant's collection of verified domains.
	/// </para>
	/// <para>
	/// Warning: Not a durable identifier for the user and shouldn't be used for authorization or to uniquely
	/// identity user information (for example, as a database key)
	/// </para>
	/// </remarks>
	public string? Upn { get; set; }
	/// <summary>
	/// A custom displayName typically from EntraID via Microsoft Graph.
	/// </summary>
	/// <remarks>
	/// <para>
	/// For standard OIDC, the `name` claim is the natural display name.
	/// </para>
	/// <para>
	/// For EntraID, it could be anything, such 'Dr. John G. Smith, MD'
	/// </para>
	/// </remarks>
	public string? DisplayName { get; set; }
	/// <summary>
	/// Preferred UserName.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Typically sourced from the EntraID `preferred_username` claim.
	/// </para>
	/// <para>
	/// The primary username that represents the user. It could be an email address, phone number, or a generic
	/// username without a specified format. Its value is mutable and might change over time. Since it's mutable,
	/// this value can't be used to make authorization decisions. It can be used for username hints and in
	/// human-readable UI as a username. The profile scope is required to receive this claim.
	/// </para>
	/// </remarks>
	public string? PreferredUserName { get; set; }

	// Metadata
	/// <summary>
	/// The timestamp of when the user profile was created.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This may be the timestamp of when the profile was created at the Idp.
	/// </para>
	/// </remarks>
	public DateTimeOffset CreatedAt { get; set; }
	/// <summary>
	/// The timestamp of when the user profile was last updated.
	/// </summary>
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>
	/// Any extra/unknown claims or profile data.
	/// </summary>
	public Dictionary<string, string> AdditionalData { get; set; } = [];


	[JsonConstructor]
	private UserProfile() {

	}

	/// <summary>
	/// Construct a new UserProfile instance.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> this profile represents.</param>
	/// <param name="timeZoneId">The default TimeZone prior to enrichment.</param>
	public UserProfile(ClaimsPrincipal principal, string timeZoneId) {

		// Defaults:
		this.Id = ClaimsHelper.ResolveId(principal) ?? "unknown";
		this.Name = ClaimsHelper.ResolveName(principal) ?? "unknown";
		this.RolesRaw = ClaimsHelper.ResolveRoles(principal);
		this.Provider = ClaimsHelper.ResolveProvider(principal);
		this.Locale = Thread.CurrentThread.CurrentUICulture.Name;
		this.TimeZone = timeZoneId;
		this.DateFormat = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.ShortDatePattern;
		this.TimeFormat = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.ShortTimePattern;
		this.CreatedAt = DateTimeOffset.Now;

		// TODO: decide if we need this here
		//		if (principal != AnonymousUser.Shared && principal.Identity is ClaimsIdentity identity) {
		//#if DEBUG
		//			const bool captureUnknownClaims = true;
		//#else
		//			const bool captureUnknownClaims = false;
		//#endif
		//			ClaimsUserProfileEnricher.EnrichProfile(this, identity, captureUnknownClaims: captureUnknownClaims);
		//		}

		// optional/common/default:
		this.PreferredUserName ??= principal.FindFirst("preferred_username")?.Value;
		this.Oid ??= ClaimsHelper.ResolveOid(principal);

		if (string.IsNullOrWhiteSpace(this.Organization.OrganizationId)) {
			var tid = ClaimsHelper.ResolveTid(principal) ?? "";
			if (!string.IsNullOrWhiteSpace(tid)) {
				var oldOrg = this.Organization;
				this.Organization = new() {
					OrganizationId = tid,
					OrganizationName = oldOrg.OrganizationName,
					DirectoryGroupsRaw = oldOrg.DirectoryGroupsRaw,
					DirectoryRolesRaw = oldOrg.DirectoryRolesRaw,
					Metadata = oldOrg.Metadata
				};
			}
		}

	}

	public static readonly UserProfile Anonymous = new UserProfile(AnonymousUser.Shared, TimeZoneInfo.Local.Id) {
		Picture = "/assets/images/guest-user-icon.svg",
		CreatedAt = DateTimeOffset.Now
	};

}