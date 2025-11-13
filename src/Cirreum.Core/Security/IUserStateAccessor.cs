namespace Cirreum.Security;

/// <summary>
/// Defines the service for resolving the <see cref="IUserState"/> for
/// the current user.
/// </summary>
public interface IUserStateAccessor {
	/// <summary>
	/// Gets the current user's state
	/// </summary>
	/// <returns>The current user's state</returns>
	ValueTask<IUserState> GetUser();
}