namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Scoped-per-request holder for the caller's <see cref="OperationGrant"/>. Stage 1 populates it
/// via <see cref="Set"/> once the grant has been resolved; handlers read <see cref="Current"/>
/// during post-fetch existence-hiding checks (Pattern C) or nested authorization (Pattern D).
/// </summary>
/// <remarks>
/// <para>
/// The accessor is registered scoped so that each authorization pipeline execution gets its
/// own instance. The resolved grant is valid only for the current operation — it encodes the
/// operation's required permissions and is not reusable across requests.
/// </para>
/// <para>
/// Before Stage 1 runs, <see cref="Current"/> returns <see cref="OperationGrant.Denied"/>.
/// </para>
/// </remarks>
public interface IOperationGrantAccessor {

	/// <summary>
	/// The grant resolved for the current operation. Returns <see cref="OperationGrant.Denied"/>
	/// when not yet set.
	/// </summary>
	OperationGrant Current { get; }

	/// <summary>
	/// Stamps the resolved grant for the current operation. Called by the owner-scope gate
	/// after the grant factory completes. Handlers do not call this.
	/// </summary>
	void Set(OperationGrant grant);
}
