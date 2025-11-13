namespace Cirreum.Conductor;

/// <summary>
/// Represents a void type, used as a return type for operations that don't produce a value.
/// </summary>
public readonly struct Unit : IEquatable<Unit> {

	/// <summary>
	/// The singleton instance of <see cref="Unit"/>.
	/// </summary>
	public static readonly Unit Value = default;

	/// <inheritdoc />
	public bool Equals(Unit other) => true;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Unit;

	/// <inheritdoc />
	public override int GetHashCode() => 0;

	/// <inheritdoc />
	public override string ToString() => "()";

#pragma warning disable IDE0060 // Remove unused parameter
	public static bool operator ==(Unit left, Unit right) => true;
	public static bool operator !=(Unit left, Unit right) => false;
#pragma warning restore IDE0060 // Remove unused parameter

}