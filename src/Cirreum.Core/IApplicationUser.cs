namespace Cirreum;

/// <summary>
/// Implemented by application-layer User entities and models, that are persisted
/// independently of the identity provider, enabling the authorization
/// system to enforce application-specific access rules.
/// </summary>
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