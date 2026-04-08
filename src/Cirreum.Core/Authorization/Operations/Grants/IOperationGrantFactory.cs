namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Creates the caller's <see cref="OperationGrant"/> for an authorization context. Consumed by
/// the Stage 1 owner-scope gate.
/// </summary>
/// <remarks>
/// <para>
/// Apps do not implement this directly — Core ships a generic orchestrator
/// <see cref="OperationGrantFactory"/> that composes an app-provided
/// <see cref="IOperationGrantProvider"/>. Register via <c>AddOperationGrants&lt;TGrantResolver&gt;()</c>.
/// </para>
/// </remarks>
public interface IOperationGrantFactory {

	/// <summary>
	/// Computes the <see cref="OperationGrant"/> for the current operation.
	/// </summary>
	ValueTask<OperationGrant> CreateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken)
		where TResource : IAuthorizableObject;
}
