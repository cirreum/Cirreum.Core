namespace Cirreum;

/// <summary>
/// Implemented by application-layer User entities that are persisted
/// independently of the identity provider, enabling the authorization
/// system to enforce application-specific access rules.
/// </summary>
public interface IApplicationUser {
	/// <summary>
	/// Gets a value indicating whether this user is active.
	/// A disabled user is treated as anonymous regardless of their IdP identity.
	/// </summary>
	bool IsEnabled { get; }
}