namespace Cirreum.Authorization.Grants;

/// <summary>
/// Resolves the caller's <see cref="AccessReach"/> for an authorization context. Consumed by
/// the Stage 1 owner-scope gate.
/// </summary>
/// <remarks>
/// <para>
/// Apps do not implement this directly — Core ships a generic orchestrator
/// <c>GrantBasedAccessReachResolver&lt;TDomain&gt;</c> that composes an app-provided
/// <c>IGrantResolver&lt;TDomain&gt;</c>. Register via <c>AddAccessGrants&lt;TDomain, TGrantResolver&gt;()</c>.
/// </para>
/// <para>
/// Implementations self-advertise which resource types they claim via <see cref="Handles"/>.
/// Core enforces 1:1 matching at startup: if two resolvers claim the same resource type,
/// startup fails fast.
/// </para>
/// </remarks>
public interface IAccessReachResolver {

	/// <summary>
	/// Returns <see langword="true"/> when this resolver claims the given resource type.
	/// Typically a test like <c>typeof(TMarker).IsAssignableFrom(resourceType)</c>.
	/// </summary>
	bool Handles(Type resourceType);

	/// <summary>
	/// Computes the <see cref="AccessReach"/> for the current operation.
	/// </summary>
	ValueTask<AccessReach> ResolveAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource;
}
