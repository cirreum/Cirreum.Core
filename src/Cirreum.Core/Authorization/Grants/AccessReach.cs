namespace Cirreum.Authorization.Grants;

/// <summary>
/// The computed set of owners a caller can touch for a specific operation, after combining
/// their home ownership, grants, and the operation's required permissions.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AccessReach"/> is the Stage 1 gate's output and the handler's input. It answers:
/// <em>"for this operation, which owners can this caller reach?"</em>
/// </para>
/// <para>
/// Three distinguished shapes:
/// </para>
/// <list type="bullet">
///   <item><description><b>Denied</b> — empty set; the caller has no access (<see cref="OwnerIds"/> is empty).</description></item>
///   <item><description><b>Unrestricted</b> — no bound; the caller has cross-tenant visibility (<see cref="OwnerIds"/> is <see langword="null"/>).</description></item>
///   <item><description><b>Bounded</b> — an explicit non-empty set of owner IDs (<see cref="OwnerIds"/> is non-null).</description></item>
/// </list>
/// <para>
/// Construct via the static factories <see cref="Denied"/>, <see cref="Unrestricted"/>, and
/// <see cref="ForOwners"/>. Apps should not construct this record directly; the
/// <c>GrantBasedAccessReachResolver&lt;TMarker&gt;</c> orchestrator produces it from an
/// app-provided <see cref="GrantedReach"/>.
/// </para>
/// </remarks>
/// <param name="OwnerIds">
/// The set of owner IDs the caller can touch, or <see langword="null"/> when reach is unrestricted.
/// An empty non-null set means denied.
/// </param>
/// <param name="Extensions">
/// Optional app-specific auxiliary dimensions (e.g., SSV codes, tiers, regions) that handlers
/// may apply in addition to the owner filter. Keyed by app-chosen strings; opaque to Core.
/// </param>
public sealed record AccessReach(
	IReadOnlyList<string>? OwnerIds,
	IReadOnlyDictionary<string, object>? Extensions = null) {

	/// <summary>
	/// Reach representing no access — the caller cannot touch any owner for this operation.
	/// </summary>
	public static AccessReach Denied { get; } = new(OwnerIds: []);

	/// <summary>
	/// Reach representing cross-tenant visibility — the caller can touch any owner (no bound).
	/// Typically produced for Global operators or wildcard-admin role bypass.
	/// </summary>
	public static AccessReach Unrestricted { get; } = new(OwnerIds: null);

	/// <summary>
	/// Builds a bounded reach over an explicit owner set. An empty <paramref name="ownerIds"/>
	/// collapses to <see cref="Denied"/>.
	/// </summary>
	/// <param name="ownerIds">The non-empty set of owner IDs the caller can touch.</param>
	/// <param name="extensions">Optional auxiliary dimensions to pass through to handlers.</param>
	public static AccessReach ForOwners(
		IReadOnlyList<string> ownerIds,
		IReadOnlyDictionary<string, object>? extensions = null) {

		ArgumentNullException.ThrowIfNull(ownerIds);
		return ownerIds.Count == 0
			? Denied
			: new AccessReach(ownerIds, extensions);
	}

	/// <summary>
	/// <see langword="true"/> when the reach represents no access.
	/// </summary>
	public bool IsDenied => this.OwnerIds is { Count: 0 };

	/// <summary>
	/// <see langword="true"/> when the reach is unrestricted (no owner bound).
	/// </summary>
	public bool IsUnrestricted => this.OwnerIds is null;

	/// <summary>
	/// <see langword="true"/> when the reach is bounded to an explicit non-empty owner set.
	/// </summary>
	public bool IsBounded => this.OwnerIds is { Count: > 0 };

	/// <summary>
	/// Checks whether <paramref name="ownerId"/> is in reach. Unrestricted reach always returns
	/// <see langword="true"/>; denied reach always returns <see langword="false"/>.
	/// </summary>
	public bool Contains(string ownerId) {
		if (this.OwnerIds is null) {
			return true;
		}
		for (var i = 0; i < this.OwnerIds.Count; i++) {
			if (string.Equals(this.OwnerIds[i], ownerId, StringComparison.Ordinal)) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Checks whether every element of <paramref name="ownerIds"/> is in reach. Unrestricted
	/// reach always returns <see langword="true"/>; empty <paramref name="ownerIds"/> returns
	/// <see langword="true"/> vacuously.
	/// </summary>
	public bool ContainsAll(IReadOnlyList<string> ownerIds) {
		ArgumentNullException.ThrowIfNull(ownerIds);
		if (this.OwnerIds is null) {
			return true;
		}
		for (var i = 0; i < ownerIds.Count; i++) {
			if (!this.Contains(ownerIds[i])) {
				return false;
			}
		}
		return true;
	}
}
