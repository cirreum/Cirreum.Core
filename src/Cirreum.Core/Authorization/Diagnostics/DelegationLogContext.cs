namespace Cirreum.Authorization.Diagnostics;

using Cirreum.Security;

/// <summary>
/// Captures the delegation-related fields needed at authorization audit/log sites, derived
/// once from an <see cref="IUserState"/> so call sites don't repeat null-checks and conditional
/// formatting.
/// </summary>
/// <remarks>
/// <para>
/// Pure value type (readonly struct) — zero heap allocation per request when the user
/// state is not delegated (returns <see cref="None"/>).
/// </para>
/// <para>
/// Read by <c>AuthorizationLogging</c> log methods and <c>AuthorizationTelemetry.RecordDecision</c>
/// to surface actor / evidence / channel state in audit logs and metrics tags.
/// </para>
/// </remarks>
internal readonly record struct DelegationLogContext(
	string? DelegationSuffix,
	string? ActorId,
	string? ActorScheme,
	string? EvidenceType,
	bool? IsDelegated) {

	/// <summary>The "no delegation context" sentinel — all fields null/false.</summary>
	public static readonly DelegationLogContext None = new(null, null, null, null, null);

	/// <summary>
	/// Derives a <see cref="DelegationLogContext"/> from the supplied
	/// <see cref="IUserState"/>. Returns <see cref="None"/> when the state is not
	/// delegated.
	/// </summary>
	public static DelegationLogContext From(IUserState? userState) {
		if (userState is null || !userState.IsDelegated) {
			return None;
		}

		var actor = userState.Actor!;
		var suffix = $" (delegated via {actor.Name}/{actor.Scheme}, evidence={actor.Delegation.EvidenceType})";
		return new DelegationLogContext(
			DelegationSuffix: suffix,
			ActorId: actor.Id,
			ActorScheme: actor.Scheme,
			EvidenceType: actor.Delegation.EvidenceType,
			IsDelegated: true);
	}

}
