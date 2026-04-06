namespace Cirreum.Authorization.Grants;

/// <summary>
/// App-implemented contract for grant resolution within a single bounded context (domain).
/// </summary>
/// <remarks>
/// <para>
/// This is the only Grants interface the app writes code against. It has one required
/// method (<see cref="ResolveGrantsAsync"/>) and two optional hooks (<see cref="ShouldBypassAsync"/>,
/// <see cref="ResolveHomeOwnerAsync"/>). The app does not touch <see cref="AccessReach"/> —
/// Core's <c>GrantBasedAccessReachResolver&lt;TDomain&gt;</c> orchestrator does all
/// translation policy (denied/unrestricted semantics, home-owner merge, empty-set collapse).
/// </para>
/// <para>
/// Register with <c>services.AddAccessGrants&lt;TDomain, TGrantResolver&gt;()</c>. Core wires
/// up the orchestrator and binds it to <see cref="IAccessReachResolver"/> automatically.
/// </para>
/// </remarks>
/// <typeparam name="TDomain">
/// The bounded-context domain marker (e.g., <c>IIssueOperation</c>) used as the first type
/// argument in <see cref="IGrantedCommand{TDomain}"/>, <see cref="IGrantedRead{TDomain, TResponse}"/>,
/// and <see cref="IGrantedList{TDomain, TResponse}"/>. Ties a set of requests to this resolver.
/// </typeparam>
public interface IGrantResolver<TDomain>
	where TDomain : class {

	/// <summary>
	/// Queries the app's grants table. Returns the owner IDs — and any auxiliary dimensions —
	/// where the caller holds ALL permissions in <c>context.RequiredPermissions</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An empty owner list means the caller has no qualifying grants. The orchestrator
	/// translates an empty combined set (grants + home owner) to <see cref="AccessReach.Denied"/>.
	/// </para>
	/// <para>
	/// Do not encode role-to-permission rules here — those belong in resource authorizers
	/// (Stage 2). This method is a pure data lookup against the grants table.
	/// </para>
	/// </remarks>
	ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource;

	/// <summary>
	/// Optional wildcard bypass: when <see langword="true"/>, the orchestrator short-circuits
	/// to <see cref="AccessReach.Unrestricted"/> without calling <see cref="ResolveGrantsAsync"/>.
	/// Intended for a single bounded-context-wide admin role (e.g., <c>IssueManager</c>).
	/// </summary>
	/// <remarks>
	/// Default: <see langword="false"/>. Override only for wildcard-admin roles. Do not build
	/// per-permission role logic here — that belongs in resource authorizers.
	/// </remarks>
	ValueTask<bool> ShouldBypassAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource
		=> new(false);

	/// <summary>
	/// Optional home-owner policy. Default implementation returns
	/// <c>(ApplicationUser as IOwnedApplicationUser)?.OwnerId</c>, which is merged into the
	/// grant-derived owner set. Return <see langword="null"/> to skip the home-owner merge
	/// (e.g., for suspended users, revoked memberships, or strict grants-only policy).
	/// </summary>
	ValueTask<string?> ResolveHomeOwnerAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource
		=> new((context.UserState.ApplicationUser as IOwnedApplicationUser)?.OwnerId);
}
