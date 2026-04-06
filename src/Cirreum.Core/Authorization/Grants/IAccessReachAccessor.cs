namespace Cirreum.Authorization.Grants;

/// <summary>
/// Scoped-per-request holder for the caller's <see cref="AccessReach"/>. Stage 1 populates it
/// via <see cref="Set"/> once reach has been resolved; handlers read <see cref="Current"/>
/// during post-fetch existence-hiding checks (Pattern C) or nested authorization (Pattern D).
/// </summary>
/// <remarks>
/// <para>
/// The accessor is registered scoped so that each authorization pipeline execution gets its
/// own instance. The resolved reach is valid only for the current operation — it encodes the
/// operation's required permissions and is not reusable across requests.
/// </para>
/// <para>
/// Before Stage 1 runs, <see cref="Current"/> returns <see cref="AccessReach.Denied"/>.
/// </para>
/// </remarks>
public interface IAccessReachAccessor {

	/// <summary>
	/// The reach resolved for the current operation. Returns <see cref="AccessReach.Denied"/>
	/// when not yet set.
	/// </summary>
	AccessReach Current { get; }

	/// <summary>
	/// Stamps the resolved reach for the current operation. Called by the owner-scope gate
	/// after the reach resolver completes. Handlers do not call this.
	/// </summary>
	void Set(AccessReach reach);
}
