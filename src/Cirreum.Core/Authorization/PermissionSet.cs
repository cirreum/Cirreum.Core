namespace Cirreum.Authorization;

using System.Collections;

/// <summary>
/// An immutable, ordered collection of <see cref="Permission"/> values with helpers for
/// membership tests, feature/operation queries, feature-scoped filtering, and deterministic
/// signature generation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PermissionSet"/> is general-purpose — it is not tied to grants, ACLs, or any
/// specific authorization stage. The grant pipeline uses it as the runtime representation of
/// <see cref="RequiresGrantAttribute"/> declarations (built once per type by
/// <see cref="RequiredGrantCache"/> and exposed via
/// <see cref="AuthorizationContext{TAuthorizableObject}.RequiredGrants"/>), but any code may
/// construct or consume a set for its own purposes.
/// </para>
/// <para>
/// Equality between elements follows <see cref="Permission"/> record equality — case-insensitive
/// on both <see cref="Permission.Feature"/> and <see cref="Permission.Operation"/>. The set is
/// sealed and fully immutable after construction.
/// </para>
/// </remarks>
public sealed class PermissionSet : IReadOnlyList<Permission> {

	/// <summary>
	/// Shared empty set. Returned wherever a query or filter produces no results, so callers
	/// can compare against this instance without allocating.
	/// </summary>
	public static readonly PermissionSet Empty = new([]);

	private readonly Permission[] _items;

	internal PermissionSet(IReadOnlyList<Permission> items) {
		this._items = items as Permission[] ?? [.. items];
	}

	/// <inheritdoc />
	public int Count => this._items.Length;

	/// <summary>
	/// <see langword="true"/> when the set contains no permissions.
	/// </summary>
	public bool IsEmpty => this._items.Length == 0;

	/// <inheritdoc />
	public Permission this[int index] => this._items[index];

	/// <inheritdoc />
	public IEnumerator<Permission> GetEnumerator() =>
		((IEnumerable<Permission>)this._items).GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => this._items.GetEnumerator();

	/// <summary>
	/// <see langword="true"/> if the set contains <paramref name="permission"/>.
	/// </summary>
	public bool Contains(Permission permission) {
		for (var i = 0; i < this._items.Length; i++) {
			if (this._items[i].Equals(permission)) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// <see langword="true"/> if the set contains the permission expressed in
	/// <c>"feature:operation"</c> format (e.g., <c>"issues:delete"</c>). The string is parsed
	/// via <see cref="Permission.Parse"/>.
	/// </summary>
	/// <exception cref="FormatException">
	/// <paramref name="featureAndOperation"/> is not in <c>"feature:operation"</c> form.
	/// </exception>
	public bool Contains(string featureAndOperation) =>
		this.Contains(Permission.Parse(featureAndOperation));

	/// <summary>
	/// <see langword="true"/> if the set contains at least one of <paramref name="permissions"/>.
	/// </summary>
	public bool ContainsAny(params Permission[] permissions) {
		for (var i = 0; i < permissions.Length; i++) {
			if (this.Contains(permissions[i])) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// <see langword="true"/> if the set contains every one of <paramref name="permissions"/>.
	/// </summary>
	public bool ContainsAll(params Permission[] permissions) {
		for (var i = 0; i < permissions.Length; i++) {
			if (!this.Contains(permissions[i])) {
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// <see langword="true"/> if any permission in the set belongs to the given feature area
	/// (e.g., <c>"issues"</c>). Comparison is case-insensitive.
	/// </summary>
	public bool HasFeature(string feature) {
		for (var i = 0; i < this._items.Length; i++) {
			if (string.Equals(this._items[i].Feature, feature, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// <see langword="true"/> if any permission in the set has the given operation verb
	/// (e.g., <c>"delete"</c>), regardless of feature. Comparison is case-insensitive.
	/// </summary>
	public bool HasOperation(string operation) {
		for (var i = 0; i < this._items.Length; i++) {
			if (string.Equals(this._items[i].Operation, operation, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns the subset of permissions belonging to <paramref name="feature"/>, or
	/// <see cref="Empty"/> if none match. Comparison is case-insensitive.
	/// </summary>
	public PermissionSet ForFeature(string feature) {
		List<Permission>? filtered = null;
		for (var i = 0; i < this._items.Length; i++) {
			if (string.Equals(this._items[i].Feature, feature, StringComparison.OrdinalIgnoreCase)) {
				(filtered ??= []).Add(this._items[i]);
			}
		}
		return filtered is null ? Empty : new PermissionSet(filtered);
	}

}
