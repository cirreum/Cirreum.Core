namespace Cirreum;

/// <summary>
/// Implemented by application-layer User entities and models that are persisted
/// independently of the identity provider, enabling the authorization system
/// to enforce application-specific access rules.
/// </summary>
/// <remarks>
/// <para>
/// Applies to the <strong>tenant authentication track</strong> only — callers
/// authenticated via a customer IdP (Entra External ID, Descope, generic OIDC)
/// where the application maintains its own user store. For these callers,
/// <see cref="IApplicationUserResolver"/> loads the record per request and the
/// authorization pipeline reads <see cref="Roles"/> and <see cref="IsEnabled"/>
/// from it.
/// </para>
/// <para>
/// Callers on the <strong>operator track</strong> (workforce IdP — typically
/// Entra workforce) and the <strong>machine track</strong> (<c>ApiKey</c>,
/// <c>SignedRequest</c>, <c>External</c> BYOID) do not have an
/// <see cref="IApplicationUser"/> record. <c>IUserState.ApplicationUser</c> is
/// permanently <see langword="null"/> for them by design, and authority flows
/// from token claims or credential policy directly. The grant evaluator
/// accommodates this via documented null-fall-through.
/// </para>
/// </remarks>
public interface IApplicationUser {

	/// <summary>
	/// Gets a value indicating whether this user is active.
	/// A disabled user is treated as anonymous regardless of their IdP identity.
	/// </summary>
	bool IsEnabled { get; }

	/// <summary>
	/// Gets the application-level roles assigned to this user.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each runtime environment is responsible for incorporating these roles
	/// into its authorization pipeline. Return an empty collection if the
	/// user has no application-level roles.
	/// </para>
	/// </remarks>
	IReadOnlyList<string> Roles { get; }

}