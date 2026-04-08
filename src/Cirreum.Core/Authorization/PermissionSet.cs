namespace Cirreum.Authorization;

using System.Collections;

/// <summary>
/// An immutable, ordered collection of <see cref="Permission"/> instances declared on a
/// resource type. Provides query helpers for checking feature areas, operations, and
/// building deterministic cache signatures.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PermissionSet"/> is the runtime representation of all
/// <see cref="RequiresPermissionAttribute"/> declarations on a resource. It is created
/// once per resource type by <see cref="RequiredPermissionCache"/> and shared across
/// all requests — the set is fully immutable after construction.
/// </para>
/// <para>
/// Available on <c>AuthorizationContext.Permissions</c> for every authorization
/// stage — grant evaluators, resource authorizers, and policy validators.
/// </para>
/// </remarks>
public sealed class PermissionSet : IReadOnlyList<Permission> {

	/// <summary>
	/// A shared empty set representing "no permissions declared."
	/// </summary>
	public static readonly PermissionSet Empty = new([]);

	private readonly Permission[] _items;

	internal PermissionSet(IReadOnlyList<Permission> items) {
		this._items = items as Permission[] ?? [.. items];
	}

	/// <inheritdoc />
	public int Count => this._items.Length;

	/// <inheritdoc />
	public Permission this[int index] => this._items[index];

	/// <inheritdoc />
	public IEnumerator<Permission> GetEnumerator() =>
		((IEnumerable<Permission>)this._items).GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => this._items.GetEnumerator();

	/// <summary>
	/// Returns <see langword="true"/> if the set contains the specified permission
	/// (compared by <see cref="Permission"/> record equality — case-insensitive on
	/// both <see cref="Permission.Feature"/> and <see cref="Permission.Operation"/>).
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
	/// Returns <see langword="true"/> if the set contains a permission matching the
	/// <c>"feature:operation"</c> string (e.g., <c>"issues:delete"</c>).
	/// </summary>
	/// <exception cref="FormatException">
	/// Thrown when <paramref name="featureAndOperation"/> is not in <c>"feature:operation"</c> format.
	/// </exception>
	public bool Contains(string featureAndOperation) =>
		this.Contains(Permission.Parse(featureAndOperation));

	/// <summary>
	/// Returns <see langword="true"/> if any permission in the set belongs to the
	/// specified feature area (e.g., <c>"issues"</c>).
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
	/// Returns <see langword="true"/> if any permission in the set has the specified
	/// operation verb (e.g., <c>"delete"</c>).
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
	/// Returns the subset of permissions belonging to the specified feature area.
	/// Returns <see cref="Empty"/> if no permissions match.
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

	/// <summary>
	/// Builds a deterministic, sorted signature from the permission operation names.
	/// Used for cache key construction — AND semantics require order-independent keys.
	/// </summary>
	/// <returns>
	/// A <c>"+"</c>-joined string of sorted operation names (e.g., <c>"archive+delete"</c>),
	/// or <see cref="string.Empty"/> when the set is empty.
	/// </returns>
	public string ToSignature() {
		if (this._items.Length == 0) {
			return string.Empty;
		}
		if (this._items.Length == 1) {
			return this._items[0].Operation;
		}

		var names = new string[this._items.Length];
		for (var i = 0; i < this._items.Length; i++) {
			names[i] = this._items[i].Operation;
		}

		// WARNING: this is required for accurate and consistent cache keys — do not change to an order-dependent join!
		Array.Sort(names, StringComparer.Ordinal);

		return string.Join('+', names);

	}

}