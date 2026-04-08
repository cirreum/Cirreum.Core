namespace Cirreum.Authorization.Grants;

/// <summary>
/// Creates the caller's <see cref="AccessGrant"/> for an authorization context. Consumed by
/// the Stage 1 owner-scope gate.
/// </summary>
/// <remarks>
/// <para>
/// Apps do not implement this directly — Core ships a generic orchestrator
/// <see cref="AccessGrantFactory"/> that composes an app-provided
/// <see cref="IGrantResolver"/>. Register via <c>AddAccessGrants&lt;TGrantResolver&gt;()</c>.
/// </para>
/// </remarks>
public interface IAccessGrantFactory {

	/// <summary>
	/// Computes the <see cref="AccessGrant"/> for the current operation.
	/// </summary>
	ValueTask<AccessGrant> CreateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableResource;
}
