namespace Cirreum.Security;

using Cirreum.Authorization;

/// <summary>
/// Captures the runtime metadata about a delegation upgrade that occurred on an
/// <see cref="IUserState"/>: the evidence type that authorized it, the granted scope
/// (as the framework's native <see cref="PermissionSet"/>), and when the upgrade was applied.
/// </summary>
/// <remarks>
/// <para>
/// One <see cref="DelegationMetadata"/> instance is captured at delegation time and
/// surfaced via <see cref="IActorContext.Delegation"/> for the lifetime of the invocation.
/// Read by audit, telemetry, and delegation-aware authorization rules to enforce
/// scope-bounded operations and reconstruct who delegated what.
/// </para>
/// <para>
/// Aligns with RFC 8693 Token Exchange semantics: <see cref="EvidenceType"/> identifies
/// the kind of subject-side credential used to authorize the upgrade, and <see cref="Scope"/>
/// captures the intersection of (subject's entitlements) ∩ (actor credential's allowed
/// delegated scopes) ∩ (delegation-policy provider's permitted scopes). Delegation is
/// always scope-narrowing — the actor cannot exceed any of the three inputs.
/// </para>
/// <para>
/// Uses <see cref="PermissionSet"/> rather than a parallel string-based scope concept
/// so the framework's authorization pipeline can directly consume it. Delegation-aware
/// <see cref="IPolicyValidator"/> implementations check whether the operation's required
/// permissions are within the delegated scope via standard <see cref="PermissionSet"/>
/// containment.
/// </para>
/// <para>
/// Immutable record by design — once stamped, the metadata represents the historical
/// fact of the upgrade and must not change.
/// </para>
/// </remarks>
/// <param name="EvidenceType">
/// The evidence-type discriminator that authorized this delegation (e.g.,
/// <c>"ivr-session"</c>, <c>"phone-pin"</c>, <c>"kastle-session"</c>). Matches the
/// key under which the consuming evidence resolver was registered. A stable wire
/// contract — value space is open-ended and app-defined; treat changes as breaking.
/// </param>
/// <param name="Scope">
/// The permissions granted to the actor when acting on behalf of the subject. The
/// effective scope is the intersection of subject entitlements, actor credential
/// allowed scopes, and delegation policy allowed scopes. Authorization rules that
/// restrict delegated access read this set to enforce scope-bounded operations.
/// </param>
/// <param name="DelegatedAt">
/// UTC timestamp at which the upgrade was applied. For audit, telemetry latency
/// calculations, and replay-window detection.
/// </param>
public sealed record DelegationMetadata(
	string EvidenceType,
	PermissionSet Scope,
	DateTimeOffset DelegatedAt);
