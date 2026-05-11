namespace Cirreum.Security;

/// <summary>
/// Snapshot of the original M2M actor that initiated a delegated invocation, captured at
/// the moment <see cref="IUserState"/> was upgraded to represent the subject (the human
/// or app user the actor is acting on behalf of).
/// </summary>
/// <remarks>
/// <para>
/// When an authenticated M2M caller (e.g. ApiKey, SignedRequest) presents valid delegation
/// evidence and policy permits the upgrade, the framework swaps the
/// <see cref="IUserState"/>'s primary view (<see cref="IUserState.Id"/>,
/// <see cref="IUserState.Name"/>, <see cref="IUserState.Principal"/>,
/// <see cref="IUserState.Profile"/>, <see cref="IUserState.ApplicationUser"/>) to the
/// subject's identity. The original actor's snapshot is preserved here so audit, compliance
/// reporting, and delegation-aware authorization rules retain non-repudiable access to who
/// initiated the request.
/// </para>
/// <para>
/// Aligns with RFC 8693 Token Exchange's <c>act</c> claim model: actor identity always
/// preserved alongside subject identity — delegation, never impersonation.
/// </para>
/// <para>
/// Read by:
/// <list type="bullet">
///   <item><description>Audit / telemetry: <c>userState.Actor.Id</c> reveals "who acted on whose behalf"</description></item>
///   <item><description>Authorization rules restricting delegated access: <c>userState.IsDelegated</c> + <see cref="DelegationMetadata.Scope"/></description></item>
///   <item><description>Compliance exports: full chain via <see cref="Id"/>, <see cref="Name"/>, <see cref="Scheme"/>, and <see cref="Delegation"/></description></item>
/// </list>
/// </para>
/// <para>
/// Stamped under <see cref="AuthenticationContextKeys.Actor"/> in
/// <c>IInvocationContext.Items</c> (and the long-lived <c>IInvocationConnection.Items</c>
/// for connection-scoped sources) by the upstream M2M auth handler. Read by the server's
/// <c>UserStateAccessor</c> when constructing the per-invocation <see cref="IUserState"/>.
/// </para>
/// </remarks>
public interface IActorContext {

	/// <summary>
	/// The actor's stable identifier — typically the registered credential's identifier
	/// (e.g., ApiKey id, SignedRequest credential id). Used as the lookup key in audit
	/// trails to reconstruct which credential acted on whose behalf.
	/// </summary>
	string Id { get; }

	/// <summary>
	/// The actor's display name — typically the credential's human-readable name
	/// (e.g., <c>"Twilio IVA - prod"</c>). For diagnostics and audit-log readability.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// The authentication scheme that authenticated the actor on the wire
	/// (e.g., <c>"ApiKey"</c>, <c>"SignedRequest"</c>). Read by authorization rules that
	/// vary their behavior based on which M2M channel initiated the delegation.
	/// </summary>
	string Scheme { get; }

	/// <summary>
	/// Metadata about the delegation event itself — evidence type that authorized it,
	/// the permission scope granted, and when the upgrade was applied.
	/// </summary>
	DelegationMetadata Delegation { get; }

}
