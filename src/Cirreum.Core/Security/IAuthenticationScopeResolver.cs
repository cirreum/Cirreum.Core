namespace Cirreum.Security;

/// <summary>
/// Resolves the <see cref="AuthenticationScope"/> for an authenticated user state.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are platform-specific. The default server implementation
/// (in <c>Cirreum.Runtime.Authorization</c>) matches the authenticated scheme
/// against the configured <c>Cirreum:Authorization:PrimaryScheme</c>.
/// </para>
/// <para>
/// Consumers may replace the default by registering their own implementation
/// before <c>AddAuthorization</c> runs (TryAdd pattern).
/// </para>
/// </remarks>
/// <example>
/// A minimal custom resolver that treats any caller whose scheme starts with
/// <c>"internal-"</c> as <see cref="AuthenticationScope.Global"/>:
/// <code>
/// internal sealed class PrefixAuthenticationScopeResolver : IAuthenticationScopeResolver {
///     public AuthenticationScope Resolve(IUserState userState, string? authenticationScheme) {
///         if (!userState.IsAuthenticated) return AuthenticationScope.None;
///         return authenticationScheme?.StartsWith("internal-") == true
///             ? AuthenticationScope.Global
///             : AuthenticationScope.Tenant;
///     }
/// }
/// </code>
/// </example>
public interface IAuthenticationScopeResolver {

	/// <summary>
	/// Computes the access scope for the given user state and authentication scheme.
	/// </summary>
	/// <param name="userState">The authenticated user state.</param>
	/// <param name="authenticationScheme">
	/// The authentication scheme name that authenticated the caller, or <see langword="null"/>
	/// when scheme is not applicable (e.g., Blazor WASM, Azure Functions binding context).
	/// </param>
	AuthenticationScope Resolve(IUserState userState, string? authenticationScheme);
}
