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
	/// Gets a value indicating whether authentication state has been fully resolved —
	/// either an authenticated identity has been established or the user has been
	/// confirmed as anonymous. Consumers can safely read identity-related properties
	/// such as <see cref="IsAuthenticated"/> and <see cref="Principal"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>ApplicationUser</c> and full <c>UserProfile</c> enrichment properties may
	/// still be unresolved at this point.
	/// </para>
	/// <para>
	/// This becomes <see langword="true"/> on the first call to either
	/// <c>SetAuthenticatedPrincipal</c> or <c>SetAnonymous</c> — whichever occurs first.
	/// For no-auth applications, this is set to <see langword="true"/> immediately
	/// during startup before any component initializes.
	/// </para>
	/// </remarks>
	bool IsAuthenticationComplete { get; }

	/// <summary>
	/// The caller's stable identity, resolved from claims by
	/// <see cref="ClaimsHelper.ResolveId(ClaimsPrincipal)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Resolution prefers the Entra <c>oid</c> (object identifier) claim when present, falling
	/// back to <c>sub</c> (OIDC subject) and then <c>user_id</c> (Firebase / custom IdP).
	/// For Entra-backed applications, <c>oid</c> is tenant-wide and stable across apps, whereas
	/// <c>sub</c> may be pairwise (different per application registration).
	/// </para>
	/// <para>
	/// This value is immutable for the lifetime of the user's identity in the IdP and is used
	/// as the external identifier on <see cref="IApplicationUser"/> — the lookup key passed to
	/// <see cref="IApplicationUserResolver.ResolveAsync(string, CancellationToken)"/>
	/// to resolve the application user — and as the basis for grant cache keys and audit trails.
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
	/// The resolved <see cref="Security.AuthenticationBoundary"/> for this caller. Defaults to
	/// <see cref="AuthenticationBoundary.None"/> when no <see cref="IAuthenticationBoundaryResolver"/>
	/// has stamped a value.
	/// </summary>
	AuthenticationBoundary AuthenticationBoundary => AuthenticationBoundary.None;

	/// <summary>
	/// Gets a value indicating whether the <see cref="AuthenticationBoundary"/> has been resolved
	/// for this user state — either by an <see cref="IAuthenticationBoundaryResolver"/> or by
	/// explicit assignment.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <see langword="false"/>, the <see cref="AuthenticationBoundary"/> value is the
	/// unresolved default (<see cref="AuthenticationBoundary.None"/>). Consumers should not
	/// interpret <see cref="AuthenticationBoundary.None"/> as meaningful until this property
	/// is <see langword="true"/>.
	/// </para>
	/// <para>
	/// This becomes <see langword="true"/> when <c>SetAuthenticationBoundary</c> is called,
	/// regardless of the resolved value — including when the resolver explicitly
	/// returns <see cref="AuthenticationBoundary.None"/> for an anonymous caller.
	/// </para>
	/// </remarks>
	bool IsAuthenticationBoundaryResolved => false;

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

	/// <summary>
	/// Gets the original M2M actor that initiated this invocation, when this user state
	/// represents a delegated identity. Non-null only when an upstream M2M auth handler
	/// successfully upgraded this user state to act on behalf of a subject via delegation.
	/// </summary>
	/// <value>
	/// The captured snapshot of the actor's identity, or <see langword="null"/> when this
	/// user state was not produced through delegation (direct sign-in, direct M2M without
	/// evidence, anonymous, etc.).
	/// </value>
	/// <remarks>
	/// <para>
	/// After a delegation upgrade, every existing <see cref="IUserState"/> property
	/// (<see cref="Id"/>, <see cref="Name"/>, <see cref="Principal"/>, <see cref="Profile"/>,
	/// <see cref="ApplicationUser"/>) represents the <b>subject</b> — the human or app user
	/// the actor is acting on behalf of. <see cref="Actor"/> is the only property that
	/// surfaces the original actor's identity, preserved for audit and delegation-aware
	/// authorization decisions.
	/// </para>
	/// <para>
	/// Code that needs to distinguish "this is a direct user" from "this is delegated"
	/// should read <see cref="IsDelegated"/> rather than null-check <see cref="Actor"/>
	/// directly; the boolean form is more readable at call sites and matches the
	/// <see cref="IsAuthenticated"/> / <see cref="IsApplicationUserLoaded"/> conventions on
	/// this interface.
	/// </para>
	/// <para>
	/// Aligns with RFC 8693 Token Exchange's <c>act</c> claim model — actor identity is
	/// always preserved alongside subject identity. Delegation, never impersonation.
	/// </para>
	/// </remarks>
	IActorContext? Actor => null;

	/// <summary>
	/// Gets a value indicating whether this user state represents a delegated identity —
	/// i.e., an M2M actor that has been upgraded to act on behalf of a subject.
	/// </summary>
	/// <value>
	/// <see langword="true"/> when <see cref="Actor"/> is non-null; otherwise
	/// <see langword="false"/>. Provided as a boolean convenience for authorization rules
	/// and audit predicates that need to distinguish delegated from direct invocations.
	/// </value>
	bool IsDelegated => this.Actor is not null;

	/// <summary>
	/// Gets a value indicating whether delegation resolution has completed for this user
	/// state — i.e., the upstream M2M auth handler has processed any delegation evidence
	/// (or confirmed none was present).
	/// </summary>
	/// <value>
	/// <see langword="true"/> when the delegation processing step has run to completion,
	/// regardless of whether delegation was applied. A value of <see langword="true"/>
	/// together with <c><see cref="Actor"/> == null</c> means "we checked for delegation
	/// and none was present" — distinct from "we haven't checked yet."
	/// </value>
	/// <remarks>
	/// <para>
	/// Mirrors <see cref="IsApplicationUserLoaded"/> — captures the "processing has run"
	/// state independent of whether an entity was found. Useful for diagnostics
	/// (confirming the auth handler ran the delegation check) and for downstream consumers
	/// that need to distinguish "not-yet-resolved" from "resolved-as-direct-caller."
	/// </para>
	/// <para>
	/// Default-implemented as <see langword="false"/> so existing <see cref="IUserState"/>
	/// implementations don't break. The framework's <c>UserStateBase</c> tracks the value
	/// internally and flips it via <c>SetActor(...)</c>.
	/// </para>
	/// </remarks>
	bool IsDelegationResolved => false;

}