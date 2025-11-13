namespace Cirreum.Security;

using System.Security.Claims;
using System.Text.Json;

/// <summary>
/// Enriches the standard <see cref="UserProfile"/> from standard claims
/// from a <see cref="ClaimsIdentity"/> instance.
/// </summary>
public class ClaimsUserProfileEnricher : IUserProfileEnricher {

	/// <inheritdoc/>
	public Task EnrichProfileAsync(UserProfile profile, ClaimsIdentity identity) {
		ArgumentNullException.ThrowIfNull(profile, nameof(profile));
		ArgumentNullException.ThrowIfNull(identity, nameof(identity));

		EnrichProfile(profile, identity, false);

		return Task.CompletedTask;

	}

	/// <summary>
	/// Common profile enrichment
	/// </summary>
	/// <param name="profile">The <see cref="UserProfile"/> being enriched.</param>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> providing the claims.</param>
	/// <param name="captureUnknownClaims"></param>
	public static void EnrichProfile(UserProfile profile, ClaimsIdentity identity, bool captureUnknownClaims) {

		ArgumentNullException.ThrowIfNull(profile, nameof(profile));
		ArgumentNullException.ThrowIfNull(identity, nameof(identity));

		ProcessStandardClaims(profile, identity);

		ProcessOrganization(profile, identity);

		ProcessOrgGroupClaims(profile, identity);

		ProcessAddressClaims(profile, identity);

		if (captureUnknownClaims) {
			CaptureUnknownClaims(profile, identity);
		}

	}

	private static void ProcessStandardClaims(UserProfile profile, ClaimsIdentity identity) {
		foreach (var claim in identity.Claims) {
			switch (claim.Type.ToLowerInvariant()) {

				// Core Profile Claims
				case "given_name":
					profile.GivenName = claim.Value;
					break;
				case "family_name":
					profile.FamilyName = claim.Value;
					break;
				case "middle_name":
					profile.MiddleName = claim.Value;
					break;
				case "nickname":
					profile.Nickname = claim.Value;
					break;
				case "picture":
					profile.Picture = claim.Value.NullIfWhiteSpace() ?? "/assets/images/guest-user-icon.svg";
					break;
				case "locale":
					profile.Locale = claim.Value;
					break;
				case "zoneinfo":
					profile.TimeZone = claim.Value;
					break;
				case "birthdate":
					profile.Birthdate = claim.Value;
					break;
				case "updated_at":
					if (claim.Value.HasValue() && DateTimeOffset.TryParse(claim.Value, out var updatedAt)) {
						profile.UpdatedAt = updatedAt;
					}
					break;

				// Email Claims
				case "email":
					profile.Email = claim.Value;
					break;
				case "email_verified":
					profile.EmailVerified = bool.TryParse(claim.Value, out var emailVerified) ? emailVerified : null;
					break;

				// Phone Claims
				case "phone_number":
					profile.PhoneNumber = claim.Value;
					break;
				case "phone_number_verified":
					profile.PhoneNumberVerified = bool.TryParse(claim.Value, out var phoneVerified) ? phoneVerified : null;
					break;

				// Oid
				case "oid":
					profile.Oid = claim.Value;
					break;

			}
		}
	}

	private static void ProcessOrganization(UserProfile profile, ClaimsIdentity identity) {
		var profileOrg = new UserProfileOrganization {
			OrganizationId = ClaimsHelper.ResolveTid(identity) ?? "",
			OrganizationName = identity.FindFirst("org_name")?.Value
					?? identity.FindFirst("org")?.Value
					?? identity.FindFirst("tenant_name")?.Value
					?? "",
			DirectoryGroupsRaw = [],
			DirectoryRolesRaw = [],
			Metadata = []
		};

		profile.Organization = profileOrg;
	}

	private static void ProcessOrgGroupClaims(UserProfile profile, ClaimsIdentity identity) {
		// Process roles that might come in different formats
		foreach (var claim in identity.Claims.Where(c => c.Type is "groups")) {
			try {
				// Try to parse as JSON array first
				var groups = JsonSerializer.Deserialize<List<string>>(claim.Value);
				groups?.ForEach(s => profile.Organization.DirectoryGroups.Add(new UserProfileMembership(
					s,
					s,
					UserProfileMembershipType.DirectoryGroup
				)));
			} catch (JsonException) {
				// If JSON parsing fails, treat as space/comma-separated
				var roles = claim.Value
					.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
					.Select(x => x.Trim())
					.ToList();
				roles.ForEach(s => profile.Organization.DirectoryGroups.Add(new UserProfileMembership(
					s,
					s,
					UserProfileMembershipType.DirectoryGroup
				)));
			}
		}
	}

	private static void ProcessAddressClaims(UserProfile profile, ClaimsIdentity identity) {

		// address.street_address, address.locality, address.region, address.postal_code, address.country, address.formatted
		var addressClaim = identity.Claims.FirstOrDefault(c => c.Type is "address")?.Value;
		if (string.IsNullOrEmpty(addressClaim)) {
			return; // No address claim found
		}

		try {
			// Deserialize the address JSON object
			var address = JsonSerializer.Deserialize<ClaimsAddress>(addressClaim);
			if (address is not null) {
				profile.Address = new UserProfileAddress {
					City = address.Locality,
					Country = address.Country,
					PostalCode = address.PostalCode,
					State = address.Region,
					StreetAddress = address.StreetAddress
				};
			}
#if DEBUG
		} catch (JsonException ex) {
			// Handle JSON deserialization errors
			Console.WriteLine($"Failed to deserialize address claim: {ex.Message}");
		}
#else
		} catch {
		}
#endif

	}

	private static void CaptureUnknownClaims(UserProfile profile, ClaimsIdentity identity) {
		// Store any non-standard claims in AdditionalData
		var standardClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"sub", "preferred_username", "name", "given_name", "family_name",
			"middle_name", "nickname", "picture", "locale", "birthdate", "updated_at", "email",
			"email_verified", "phone_number", "phone_number_verified",
			"roles", "role", "iss"
		};

		foreach (var claim in identity.Claims) {
			if (!standardClaims.Contains(claim.Type)) {
				profile.AdditionalData[claim.Type] = claim.Value;
			}
		}
	}

	class ClaimsAddress {
		public string? Formatted { get; set; }
		public string? StreetAddress { get; set; }
		public string? Locality { get; set; }
		public string? Region { get; set; }
		public string? PostalCode { get; set; }
		public string? Country { get; set; }
	}

}