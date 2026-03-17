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
	/// Well-known key for caching the resolved <see cref="IApplicationUser"/>
	/// in per-request or per-session state.
	/// </summary>
	const string CacheKey = "__Cirreum_ApplicationUser";

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
