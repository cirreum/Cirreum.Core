namespace Cirreum.Security;

/// <summary>
/// Well-known keys used to coordinate authentication context across pipeline stages
/// via <c>HttpContext.Items</c>.
/// </summary>
/// <remarks>
/// <para>
/// Centralizes the string keys read and written by multiple subsystems (the dynamic
/// scheme forward selector, the role claims transformer, <see cref="IAuthenticationBoundaryResolver"/>
/// consumers, the application user resolver dispatcher, and downstream user accessors).
/// </para>
/// <para>
/// Packages that cannot take a dependency on Cirreum.Core (e.g. standalone authorization
/// provider runtimes) duplicate these literals locally with a comment pointing back here.
/// The string values are stable contracts; treat changes as breaking.
/// </para>
/// </remarks>
public static class AuthenticationContextKeys {

	/// <summary>
	/// The authentication scheme that authenticated the current request.
	/// </summary>
	/// <remarks>
	/// Stamped during dynamic scheme dispatch by the forward selector in
	/// <c>Cirreum.Runtime.Authorization</c>, and defensively by
	/// <c>AudienceProviderRoleClaimsTransformer</c> for routes wired to an explicit scheme.
	/// Read by <see cref="IAuthenticationBoundaryResolver"/> consumers (e.g. <c>UserAccessor</c>)
	/// and by the per-scheme <c>IApplicationUserResolver</c> dispatcher.
	/// </remarks>
	public const string AuthenticatedScheme = "__Cirreum_AuthenticatedScheme";

	/// <summary>
	/// The resolved <see cref="IApplicationUser"/> for the current request.
	/// </summary>
	/// <remarks>
	/// Populated by the role claims transformer's adapter when it loads the application
	/// user during role enrichment. Read by request-scoped consumers
	/// (e.g. <c>UserAccessor</c>) so they avoid a redundant resolver call.
	/// </remarks>
	public const string ApplicationUserCache = "__Cirreum_ApplicationUser";

	/// <summary>
	/// The captured <see cref="IActorContext"/> when the current invocation represents a
	/// delegated identity — an M2M actor acting on behalf of a subject.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Stamped by the upstream M2M auth handler (e.g., <c>Cirreum.Authorization.ApiKey</c>,
	/// <c>Cirreum.Authorization.SignedRequest</c>) into <c>HttpContext.Items</c> /
	/// <c>IInvocationConnection.Items</c> after a successful delegation orchestration.
	/// Read by the server's <c>UserStateAccessor</c> when constructing the per-invocation
	/// <see cref="IUserState"/>; surfaced via <see cref="IUserState.Actor"/>.
	/// </para>
	/// <para>
	/// Absent (or <see langword="null"/>) when the invocation is not delegated — direct
	/// sign-in, direct M2M without evidence, anonymous.
	/// </para>
	/// </remarks>
	public const string Actor = "__Cirreum_Actor";

}
