namespace Cirreum;

using Cirreum.Security;
using System.Linq;

public static class UserStateExtensions {

	/// <summary>
	/// Gets the value of the first claim with the specified type from the user's principal.
	/// </summary>
	/// <param name="userState">The user state.</param>
	/// <param name="claimType">The type (name) of the claim.</param>
	/// <returns>The value of the claim if found; otherwise, an empty string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="claimType"/> is null.</exception>
	public static string GetClaim(this IUserState userState, string claimType) {
		ArgumentException.ThrowIfNullOrWhiteSpace(claimType, nameof(claimType));

		return userState?.Principal?.Claims?.FirstOrDefault(c => c.Type == claimType)?.Value ?? "";
	}

	/// <summary>
	/// Determines whether the user's principal has a claim with the specified type.
	/// </summary>
	/// <param name="userState">The user state.</param>
	/// <param name="claimType">The type (name) of the claim.</param>
	/// <returns><c>true</c> if the user has a claim with the specified type; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="claimType"/> is null.</exception>
	public static bool HasClaim(this IUserState userState, string claimType) {
		ArgumentException.ThrowIfNullOrWhiteSpace(claimType, nameof(claimType));

		return userState?.Principal?.Claims?.Any(c => c.Type == claimType) ?? false;
	}

	/// <summary>
	/// Determines whether the user's principal has a claim with the specified type and value.
	/// The <see cref="IUserState.Principal"/> must have the specified <paramref name="claimType"/>
	/// with the specified <paramref name="claimValue"/>.
	/// </summary>
	/// <param name="userState">The user state.</param>
	/// <param name="claimType">The type (name) of the claim.</param>
	/// <param name="claimValue">The value of the claim.</param>
	/// <returns><c>true</c> if the user has a claim with the specified type and value; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="claimType"/> is null.</exception>
	public static bool HasClaimValue(this IUserState userState, string claimType, string claimValue) {
		ArgumentException.ThrowIfNullOrWhiteSpace(claimType, nameof(claimType));

		return userState?.Principal?.Claims?.Any(c => c.Type == claimType && c.Value == claimValue) ?? false;
	}

}