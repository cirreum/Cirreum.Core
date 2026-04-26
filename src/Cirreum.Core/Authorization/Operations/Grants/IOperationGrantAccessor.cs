namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Scoped-per-request holder for the caller's <see cref="OperationGrant"/> and the
/// outcome signals from Stage 1 grant evaluation. Stage 1 populates the accessor via
/// <see cref="Set"/> once the grant has been resolved; handlers read <see cref="Current"/>
/// during Pattern C post-fetch existence-hiding checks. See the Grants README for the
/// full Pattern A vs. Pattern C taxonomy.
/// </summary>
/// <remarks>
/// <para>
/// The accessor is registered scoped so that each authorization pipeline execution gets
/// its own instance. The resolved grant is valid only for the current operation — it
/// encodes the operation's required permissions and is not reusable across requests.
/// </para>
/// <para>
/// In addition to the grant itself, the accessor surfaces two **outcome signals** that
/// downstream consumers (handlers, Stage 2 authorizers, Stage 3 policies, telemetry)
/// can branch on:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="OwnerWasAutoStamped"/> — whether Stage 1 inferred <c>OwnerId</c> from
///     a single-owner grant, or the caller supplied it explicitly.
///   </description></item>
///   <item><description>
///     <see cref="WasRead"/> — whether handler code has read <see cref="Current"/> at
///     least once, used by Pattern C audit to detect lookup handlers that skipped the
///     post-fetch ownership check.
///   </description></item>
/// </list>
/// <para>
/// Before Stage 1 runs, <see cref="Current"/> returns <see cref="OperationGrant.Denied"/>
/// and both signals are <see langword="false"/>.
/// </para>
/// </remarks>
public interface IOperationGrantAccessor {

	/// <summary>
	/// The grant resolved for the current operation. Returns <see cref="OperationGrant.Denied"/>
	/// when not yet set. Reading this property sets <see cref="WasRead"/> to
	/// <see langword="true"/> as a side effect — the framework uses this signal to detect
	/// Pattern C handlers that forgot to check the grant after fetch.
	/// </summary>
	OperationGrant Current { get; }

	/// <summary>
	/// <see langword="true"/> when Stage 1 auto-stamped <c>OwnerId</c> from a single-owner
	/// grant rather than receiving it from the caller. Provides the runtime signal that
	/// the framework inferred owner intent — handlers and late-stage evaluators can
	/// audit, log, or branch on this (e.g., to require explicit owner choice on
	/// sensitive writes). An OTel activity tag is emitted in parallel for forensic
	/// audit.
	/// </summary>
	bool OwnerWasAutoStamped { get; }

	/// <summary>
	/// <see langword="true"/> when handler code (or any downstream consumer) has read
	/// <see cref="Current"/> at least once. Used by the Pattern C audit signal to
	/// detect <c>IGrantableLookupBase</c> handlers that were invoked with a null
	/// <c>OwnerId</c> and completed without performing the post-fetch ownership check.
	/// </summary>
	bool WasRead { get; }

	/// <summary>
	/// Stamps the resolved grant for the current operation. Called by the owner-scope
	/// gate after the grant factory completes. Handlers do not call this.
	/// </summary>
	void Set(OperationGrant grant);

	/// <summary>
	/// Records that Stage 1 auto-stamped <c>OwnerId</c> from a single-owner grant.
	/// Called by <c>OperationGrantEvaluator</c> when the framework infers the owner.
	/// Handlers do not call this.
	/// </summary>
	void MarkOwnerAutoStamped();
}
