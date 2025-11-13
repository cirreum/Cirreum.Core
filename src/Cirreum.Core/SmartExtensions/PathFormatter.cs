namespace Cirreum.SmartExtensions;

using SmartFormat.Core.Extensions;

#region PathFormatter
/// <summary>
/// Supports using the <see cref="Path"/> object to parse a value when performing composite string format.
/// </summary>
public class PathFormatter : IFormatter {

	private static readonly string name = "path";

	/// <summary>
	/// Collection of supported and available commands.
	/// </summary>
	readonly Dictionary<string, Func<string, string>> commands;

	/// <inheritdoc/>
	public string Name { get; set; } = name;

	/// <inheritdoc/>
	public bool CanAutoDetect { get; set; }

	/// <inheritdoc/>
	public bool TryEvaluateFormat(IFormattingInfo formattingInfo) {

		var format = formattingInfo.Format;
		var current = formattingInfo.CurrentValue?.ToString() ?? "";

		if (format is null) {
			return false;
		}

		if (format.HasNested) {
			return false;
		}

		if (string.IsNullOrWhiteSpace(current)) {
			return false;
		}

		var options =
			formattingInfo.FormatterOptions != "" ?
			formattingInfo.FormatterOptions :
			format.GetLiteralText();

		if (this.commands.TryGetValue(options ?? "", out var func)) {
			var v = func(current);
			foreach (var itemFormat in format.Items) {
				v = formattingInfo.FormatDetails.Formatter.Format("{0:" + itemFormat.RawText + "}", v);
			}
			formattingInfo.Write(v);
			return true;
		}

		return false;

	}

	/// <summary>
	/// 
	/// </summary>
	public PathFormatter() {

		this.commands = new Dictionary<string, Func<string, string>>(12, StringComparer.OrdinalIgnoreCase) {

				// Directory
				//1
				{
					"dir",
					(value) => {
						return Path.GetDirectoryName(value) ?? "";
					}
				},


				// Directory + FileNameWithoutExtension
				//2
				{
					"dirName",
					(value) => {
						return Path.GetFileNameWithoutExtension(Path.GetDirectoryName(value)) ?? "";
					}
				},


				// GetFileNameWithoutExtension
				//3
				{
					"nameWithoutExtension",
					(value) => {
						return Path.GetFileNameWithoutExtension(value) ?? "";
					}
				},
				//4
				{
					"nameNoExtension",
					(value) => {
						return Path.GetFileNameWithoutExtension(value) ?? "";
					}
				},
				//5
				{
					"nameNoExt",
					(value) => {
						return Path.GetFileNameWithoutExtension(value) ?? "";
					}
				},
				//6
				{
					"nameOnly",
					(value) => {
						return Path.GetFileNameWithoutExtension(value) ?? "";
					}
				},


				// GetFileName
				//7
				{
					"name",
					(value) => {
						return Path.GetFileName(value) ?? "";
					}
				},


				// GetExtension
				//8
				{
					"extension",
					(value) => {
						return Path.GetExtension(value) ?? "";
					}
				},
				//9
				{
					"ext",
					(value) => {
						return Path.GetExtension(value) ?? "";
					}
				},

				// GetFullPath
				//10
				{
					"fullPath",
					(value) => {
						return Path.GetFullPath(value) ?? "";
					}
				},
				//11
				{
					"full",
					(value) => {
						return Path.GetFullPath(value) ?? "";
					}
				},

				// GetPathRoot
				//12
				{
					"root",
					(value) => {
						return Path.GetPathRoot(value) ?? "";
					}
				}
			};

	}

}
#endregion