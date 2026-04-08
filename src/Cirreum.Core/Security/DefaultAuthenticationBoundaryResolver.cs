namespace Cirreum.Security;

/// <summary>
/// Default <see cref="IAuthenticationBoundaryResolver"/> that treats every authenticated caller as
/// <see cref="AuthenticationBoundary.Global"/> and every unauthenticated caller as
/// <see cref="AuthenticationBoundary.None"/>.
/// </summary>
/// <remarks>
/// This is the correct default for single-scheme applications where all authenticated
/// users belong to the operator's own IdP. Multi-tenant applications should replace
/// this with a scheme-aware resolver (e.g., in <c>Cirreum.Runtime.Authorization</c>)
/// that distinguishes the operator's <c>PrimaryScheme</c> from tenant IdP schemes.
/// </remarks>
sealed class DefaultAuthenticationBoundaryResolver : IAuthenticationBoundaryResolver {

	/// <inheritdoc/>
	public AuthenticationBoundary Resolve(IUserState userState, string? authenticationScheme) =>
		userState.IsAuthenticated ? AuthenticationBoundary.Global : AuthenticationBoundary.None;

}
