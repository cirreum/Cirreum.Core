namespace Cirreum.Security;

using Cirreum.Authorization;
using System.Security.Claims;
using System.Security.Principal;

/// <summary>
/// Represents the current user state (authenticated or anonymous) of the application
/// with session information.
/// </summary>
public interface IUserState : IUserSession {

	/// <summary>
	/// Is the current user state represents an authenticated user.
	/// </summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// The application user id. Typically from the 'sub' claim.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This value is typically immutable and can't be reassigned or reused within
	/// the scope of a single application.
	/// </para>
	/// <para>
	/// The subject identifier may be pairwise (different for each application) or
	/// public (same across applications) depending on the Identity Provider's configuration.
	/// With pairwise identifiers, if a single user signs into two different apps using
	/// two different client IDs, those apps receive different values for the subject claim.
	/// </para>
	/// <para>
	/// Such as Microsoft Entra ID (formerly Azure AD), may vary the "sub" value per application.
	/// In such cases, consider using <see cref="UserProfile.Oid"/> for tenant-wide consistency if available.
	/// </para>
	/// </remarks>
	string Id { get; }

	/// <summary>
	/// The application user name. Typically from the 'name' claim.
	/// </summary>
	/// <remarks>
	/// The name claim provides a human-readable value that identifies the subject of the token. The value
	/// isn't guaranteed to be unique, it can be changed, and should be used only for display purposes (not to
	/// be confused with `displayName` from Entra ID). The profile scope is required to receive this claim.
	/// </remarks>
	string Name { get; }

	/// <summary>
	/// The <see cref="IdentityProviderType"/> that authenticated this user
	/// </summary>
	IdentityProviderType Provider { get; }

	/// <summary>
	/// Gets the configured Authentication Library Type.
	/// </summary>
	/// <value>
	/// One of the following <see cref="AuthenticationLibraryType"/> values:
	/// <list type="bullet">
	///   <item><description><see cref="AuthenticationLibraryType.None"/></description></item>
	///   <item><description><see cref="AuthenticationLibraryType.OIDC"/></description></item>
	///   <item><description><see cref="AuthenticationLibraryType.MSAL"/></description></item>
	/// </list>
	/// </value>
	AuthenticationLibraryType AuthenticationType { get; }

	/// <summary>
	/// The <see cref="UserProfile"/>
	/// </summary>
	UserProfile Profile { get; }

	/// <summary>
	/// Gets the current <see cref="ClaimsPrincipal"/>. This is maintained for compatibility
	/// with authentication system components that expect an <see cref="IPrincipal"/> or
	/// a <see cref="ClaimsPrincipal"/>.
	/// </summary>
	ClaimsPrincipal Principal { get; }

	/// <summary>
	/// Gets the <see cref="ClaimsIdentity"/> the <see cref="UserProfile"/> is
	/// based on.
	/// </summary>
	ClaimsIdentity Identity { get; }

	/// <summary>
	/// Gets the application's domain user associated with this identity state, if loaded.
	/// </summary>
	/// <value>
	/// The application user instance, or <see langword="null"/> if no application user 
	/// has been loaded or the user is not authenticated.
	/// </value>
	/// <remarks>
	/// <para>
	/// This property provides access to the application's domain user object that 
	/// corresponds to the authenticated identity. The application user typically contains
	/// business-specific information such as preferences, permissions, and domain-specific
	/// properties that are not part of the identity provider's user profile.
	/// </para>
	/// <para>
	/// Use <see cref="GetApplicationUser{T}"/> for type-safe access to specific 
	/// application user types.
	/// </para>
	/// </remarks>
	IApplicationUser? ApplicationUser { get; }

	/// <summary>
	/// Gets a value indicating whether an application user has been loaded and 
	/// associated with this user state.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if an application user has been loaded; otherwise, 
	/// <see langword="false"/>.
	/// </value>
	/// <remarks>
	/// <para>
	/// This property indicates whether the application user loading process has been 
	/// attempted, regardless of whether a user was found. A value of <see langword="true"/> 
	/// means the loading process completed, but <see cref="ApplicationUser"/> may still 
	/// be <see langword="null"/> if no corresponding application user exists.
	/// </para>
	/// <para>
	/// This is useful for determining whether to attempt loading the application user
	/// or whether the loading has already been performed.
	/// </para>
	/// </remarks>
	bool IsApplicationUserLoaded { get; }

	/// <summary>
	/// Gets the application user cast to the specified type.
	/// </summary>
	/// <typeparam name="T">
	/// The type of application user to retrieve. Must implement <see cref="IApplicationUser"/>.
	/// </typeparam>
	/// <returns>
	/// The application user cast to type <typeparamref name="T"/>, or <see langword="null"/> 
	/// if no application user is loaded or the loaded user is not of type <typeparamref name="T"/>.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method provides type-safe access to the application user. It will only return 
	/// a non-null value if:
	/// </para>
	/// <list type="bullet">
	/// <item><description>An application user has been loaded (<see cref="IsApplicationUserLoaded"/> is <see langword="true"/>)</description></item>
	/// <item><description>The loaded application user is exactly of type <typeparamref name="T"/></description></item>
	/// </list>
	/// <para>
	/// Use this method when you know the specific type of application user your application uses,
	/// rather than working with the base <see cref="IApplicationUser"/> interface.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var domainUser = userState.GetApplicationUser&lt;MyApplicationUser&gt;();
	/// if (domainUser != null)
	/// {
	///     // Work with strongly-typed domain user
	///     var preferences = domainUser.UserPreferences;
	/// }
	/// </code>
	/// </example>
	T? GetApplicationUser<T>() where T : class, IApplicationUser;

}