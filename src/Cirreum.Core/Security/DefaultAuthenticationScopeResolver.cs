namespace Cirreum.Security;

/// <summary>
/// Default <see cref="IAuthenticationScopeResolver"/> that treats every authenticated caller as
/// <see cref="AuthenticationScope.Global"/> and every unauthenticated caller as
/// <see cref="AuthenticationScope.None"/>.
/// </summary>
/// <remarks>
/// This is the correct default for single-scheme applications where all authenticated
/// users belong to the operator's own IdP. Multi-tenant applications should replace
/// this with a scheme-aware resolver (e.g., in <c>Cirreum.Runtime.Authorization</c>)
/// that distinguishes the operator's <c>PrimaryScheme</c> from tenant IdP schemes.
/// </remarks>
sealed class DefaultAuthenticationScopeResolver : IAuthenticationScopeResolver {

	/// <inheritdoc/>
	public AuthenticationScope Resolve(IUserState userState, string? authenticationScheme) =>
		userState.IsAuthenticated ? AuthenticationScope.Global : AuthenticationScope.None;

}
