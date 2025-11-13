namespace Cirreum;

/// <summary>
/// A User Profile Organization Membership
/// </summary>
/// <param name="Id"></param>
/// <param name="DisplayName"></param>
/// <param name="Type"><see cref="UserProfileMembershipType.None"/>, <see cref="UserProfileMembershipType.DirectoryGroup"/> or <see cref="UserProfileMembershipType.DirectoryRole"/></param>
public record UserProfileMembership(
	string Id,
	string DisplayName,
	UserProfileMembershipType Type) { }