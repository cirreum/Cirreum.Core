namespace Cirreum.SmartExtensions;

/// <summary>
/// A custom <see cref="CommandSource{T}"/>, that generates a <see cref="Guid"/>.
/// </summary>
/// <remarks>
/// <para>
/// Example:
/// <code>
/// var guidStr = "$Guid.New:D".Format();
/// // -OR-
/// var obj = new { MyGuidStrProp = "$Guid:New" }
/// obj.FormatProperties();
/// var guidStr = obj.MyGuidStrProp;
/// // -OR-
/// var emptyGuidStr = "$Guid:Empty".Format();
/// </code>
/// </para>
/// <para>
/// Formatting Options:
/// <code>
/// {Guid.New:N} - New GUID in "N" format (32 digits)
/// {Guid.New:D} - New GUID in "D" format (with hyphens)
/// {Guid.New:B} - New GUID in "B" format (with hyphens and braces)
/// {Guid.New:P} - New GUID in "P" format (with hyphens and parentheses)
/// {Guid.New:X} - New GUID in "X" format (with hyphens and enclosed in {0x, 0x, ... 0x})
/// </code>
/// </para>
/// </remarks>
public class GuidCommandSource() : CommandSource<Guid?>("Guid") {

	/// <inheritdoc/>
	protected override Guid? ResolveValue(string command) {

		return command.ToUpperInvariant() switch {
			"NEW" => Guid.NewGuid(),
			"NEWGUID" => Guid.NewGuid(),
			"EMPTY" => Guid.Empty,
			_ => null // Default or return null if not recognized
		};

	}

}