namespace Cirreum.Authorization.Grants;

/// <summary>
/// Default scoped holder for <see cref="AccessReach"/>. Backed by a single field.
/// </summary>
sealed class DefaultAccessReachAccessor : IAccessReachAccessor {

	private AccessReach? _reach;

	public AccessReach Current => this._reach ?? AccessReach.Denied;

	public void Set(AccessReach reach) {
		ArgumentNullException.ThrowIfNull(reach);
		this._reach = reach;
	}
}
