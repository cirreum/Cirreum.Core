namespace Cirreum;

/// <summary>
/// Resolves an <see cref="IApplicationUser"/> from the application's data store
/// using the external identity provider user identifier.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to load the application user and their roles from your
/// data store. Each runtime environment is responsible for calling the resolver at
/// the appropriate point in its pipeline and caching the result for the duration
/// of the request or session to prevent redundant calls.
/// </para>
/// <para>
/// This interface is shared across all runtime environments (WASM, Server, Serverless)
/// to provide a uniform application user resolution pattern.
/// </para>
/// </remarks>
public interface IApplicationUserResolver {

	/// <summary>
	/// The authentication scheme this resolver handles, or <see langword="null"/>
	/// for the default fallback resolver.
	/// </summary>
	/// <remarks>
	/// <para>
	/// In multi-IdP server hosts, multiple <see cref="IApplicationUserResolver"/>
	/// implementations may be registered — one per IdP scheme that backs a distinct
	/// application user store. The dispatching consumer reads the request's
	/// authenticated scheme and selects the resolver whose <see cref="Scheme"/> matches,
	/// falling back to the resolver whose <see cref="Scheme"/> is <see langword="null"/>
	/// when no match is found.
	/// </para>
	/// <para>
	/// Singular by design — enforces a 1:1 scheme→resolver→store mapping. Apps that
	/// genuinely need to share a single store across multiple schemes own their own
	/// discriminator and can register the same resolver class with distinct
	/// <see cref="Scheme"/> values, or compose schemes inside a single resolver.
	/// </para>
	/// </remarks>
	string? Scheme => null;

	/// <summary>
	/// Resolves the application user for the given external identity.
	/// </summary>
	/// <param name="externalUserId">
	/// The user's identifier, typically sourced from the <c>oid</c>, <c>sub</c>,
	/// or <c>user_id</c> claim in the access token.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// The resolved <see cref="IApplicationUser"/> when the user exists in the
	/// application's data store; otherwise <see langword="null"/>. A null result
	/// indicates the external identity has no corresponding application user.
	/// </returns>
	Task<IApplicationUser?> ResolveAsync(
		string externalUserId,
		CancellationToken cancellationToken = default);

}
