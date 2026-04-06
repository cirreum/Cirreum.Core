namespace Cirreum.Security;

/// <summary>
/// Default <see cref="IAccessScopeResolver"/> that treats every authenticated caller as
/// <see cref="AccessScope.Global"/> and every unauthenticated caller as
/// <see cref="AccessScope.None"/>.
/// </summary>
/// <remarks>
/// This is the correct default for single-scheme applications where all authenticated
/// users belong to the operator's own IdP. Multi-tenant applications should replace
/// this with a scheme-aware resolver (e.g., in <c>Cirreum.Runtime.Authorization</c>)
/// that distinguishes the operator's <c>PrimaryScheme</c> from tenant IdP schemes.
/// </remarks>
sealed class DefaultAccessScopeResolver : IAccessScopeResolver {

	/// <inheritdoc/>
	public AccessScope Resolve(IUserState userState, string? authenticationScheme) =>
		userState.IsAuthenticated ? AccessScope.Global : AccessScope.None;

}
