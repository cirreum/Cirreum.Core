namespace Cirreum.Authorization.Grants;

/// <summary>
/// Default scoped holder for <see cref="AccessGrant"/>. Backed by a single field.
/// </summary>
sealed class DefaultAccessGrantAccessor : IAccessGrantAccessor {

	private AccessGrant? _grant;

	public AccessGrant Current => this._grant ?? AccessGrant.Denied;

	public void Set(AccessGrant grant) {
		ArgumentNullException.ThrowIfNull(grant);
		this._grant = grant;
	}
}
