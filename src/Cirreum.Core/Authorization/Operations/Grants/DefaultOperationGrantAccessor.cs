namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Default scoped holder for <see cref="OperationGrant"/>. Backed by a single field.
/// </summary>
sealed class DefaultOperationGrantAccessor : IOperationGrantAccessor {

	private OperationGrant? _grant;

	public OperationGrant Current => this._grant ?? OperationGrant.Denied;

	public void Set(OperationGrant grant) {
		ArgumentNullException.ThrowIfNull(grant);
		this._grant = grant;
	}
}
