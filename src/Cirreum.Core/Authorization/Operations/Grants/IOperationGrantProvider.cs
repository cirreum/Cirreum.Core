namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// App-implemented contract for grant resolution. A single universal resolver handles
/// all bounded contexts, using <c>context.DomainFeature</c> and
/// <c>context.RequiredGrants</c> to query the grants table.
/// </summary>
/// <remarks>
/// <para>
/// This is the only Grants interface the app writes code against. It has one required
/// method (<see cref="ResolveGrantsAsync"/>) and two optional hooks (<see cref="ShouldBypassAsync"/>,
/// <see cref="ResolveHomeOwnerAsync"/>). The app does not touch <see cref="OperationGrant"/> —
/// Core's <c>OperationGrantFactory</c> orchestrator does all
/// translation policy (denied/unrestricted semantics, home-owner merge, empty-set collapse).
/// </para>
/// <para>
/// Register with <c>services.AddOperationGrants&lt;TResolver&gt;()</c>. Core wires
/// up the orchestrator and binds it to <see cref="IOperationGrantFactory"/> automatically.
/// </para>
/// </remarks>
public interface IOperationGrantProvider {

	/// <summary>
	/// Queries the app's grants table. Returns the owner IDs — and any auxiliary dimensions —
	/// where the caller holds ALL permissions in <c>context.RequiredGrants</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An empty owner list means the caller has no qualifying grants. The orchestrator
	/// translates an empty combined set (grants + home owner) to <see cref="OperationGrant.Denied"/>.
	/// </para>
	/// <para>
	/// Do not encode role-to-permission rules here — those belong in resource authorizers
	/// (Stage 2). This method is a pure data lookup against the grants table.
	/// </para>
	/// </remarks>
	ValueTask<OperationGrantResult> ResolveGrantsAsync<TAuthorizableObject>(
		AuthorizationContext<TAuthorizableObject> context,
		CancellationToken cancellationToken)
		where TAuthorizableObject : IAuthorizableObject;

	/// <summary>
	/// Optional wildcard bypass: when <see langword="true"/>, the orchestrator short-circuits
	/// to <see cref="OperationGrant.Unrestricted"/> without calling <see cref="ResolveGrantsAsync"/>.
	/// Intended for a single bounded-context-wide admin role (e.g., <c>IssueManager</c>).
	/// </summary>
	/// <remarks>
	/// Default: <see langword="false"/>. Override only for wildcard-admin roles. Do not build
	/// per-permission role logic here — that belongs in resource authorizers.
	/// </remarks>
	ValueTask<bool> ShouldBypassAsync<TAuthorizableObject>(
		AuthorizationContext<TAuthorizableObject> context,
		CancellationToken cancellationToken)
		where TAuthorizableObject : IAuthorizableObject
		=> new(false);

	/// <summary>
	/// Optional home-owner policy. Default implementation returns
	/// <c>(ApplicationUser as IOwnedApplicationUser)?.OwnerId</c>, which is merged into the
	/// grant-derived owner set. Return <see langword="null"/> to skip the home-owner merge
	/// (e.g., for suspended users, revoked memberships, or strict grants-only policy).
	/// </summary>
	ValueTask<string?> ResolveHomeOwnerAsync<TAuthorizableObject>(
		AuthorizationContext<TAuthorizableObject> context,
		CancellationToken cancellationToken)
		where TAuthorizableObject : IAuthorizableObject
		=> new((context.UserState.ApplicationUser as IOwnedApplicationUser)?.OwnerId);
}
