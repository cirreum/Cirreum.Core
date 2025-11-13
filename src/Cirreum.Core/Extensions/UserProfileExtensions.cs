namespace Cirreum;

/// <summary>
/// Extension methods for <see cref="UserProfile"/>.
/// </summary>
public static class UserProfileExtensions {

	/// <summary>
	/// Determines whether the user belongs to the specified role.
	/// </summary>
	/// <param name="profile">The <see cref="UserProfile"/> instance.</param>
	/// <param name="role">The role to check.</param>
	/// <returns>true if the user is in the specified role; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">user or role is null.</exception>
	public static bool IsInRole(this UserProfile profile, string role) {
		ArgumentNullException.ThrowIfNull(profile);
		ArgumentNullException.ThrowIfNull(role);

		return profile.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Determines whether the user belongs to any of the specified roles.
	/// </summary>
	/// <param name="profile">The <see cref="UserProfile"/> instance.</param>
	/// <param name="roles">The roles to check.</param>
	/// <returns>true if the user is in any of the specified roles; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">user or roles is null.</exception>
	public static bool IsInAnyRole(this UserProfile profile, params string[] roles) {
		ArgumentNullException.ThrowIfNull(profile);
		ArgumentNullException.ThrowIfNull(roles);

		return roles.Any(role => profile.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Determines whether the user belongs to all of the specified roles.
	/// </summary>
	/// <param name="profile">The <see cref="UserProfile"/> instance.</param>
	/// <param name="roles">The roles to check.</param>
	/// <returns>true if the user is in all of the specified roles; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">user or roles is null.</exception>
	public static bool IsInAllRoles(this UserProfile profile, params string[] roles) {
		ArgumentNullException.ThrowIfNull(profile);
		ArgumentNullException.ThrowIfNull(roles);

		return roles.All(role => profile.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
	}

}