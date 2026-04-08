namespace Cirreum.Security;

/// <summary>
/// Resolves the <see cref="AuthenticationBoundary"/> for an authenticated user state.
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
/// <c>"internal-"</c> as <see cref="AuthenticationBoundary.Global"/>:
/// <code>
/// internal sealed class PrefixAuthenticationBoundaryResolver : IAuthenticationBoundaryResolver {
///     public AuthenticationBoundary Resolve(IUserState userState, string? authenticationScheme) {
///         if (!userState.IsAuthenticated) return AuthenticationBoundary.None;
///         return authenticationScheme?.StartsWith("internal-") == true
///             ? AuthenticationBoundary.Global
///             : AuthenticationBoundary.Tenant;
///     }
/// }
/// </code>
/// </example>
public interface IAuthenticationBoundaryResolver {

	/// <summary>
	/// Computes the authentication boundary for the given user state and authentication scheme.
	/// </summary>
	/// <param name="userState">The authenticated user state.</param>
	/// <param name="authenticationScheme">
	/// The authentication scheme name that authenticated the caller, or <see langword="null"/>
	/// when scheme is not applicable (e.g., Blazor WASM, Azure Functions binding context).
	/// </param>
	AuthenticationBoundary Resolve(IUserState userState, string? authenticationScheme);
}
