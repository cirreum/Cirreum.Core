namespace Cirreum.SmartExtensions;

using SmartFormat;
using SmartFormat.Core.Extensions;

#region CommandSource<T>
/// <summary>
/// An abstract extension of <see cref="Source"/> that provides base functionality for command-based sources
/// requiring a prefix of '$'. Example: {$dns.ip4address}.
/// </summary>
/// <typeparam name="T">The Type of object an implementation resolves.</typeparam>
/// <param name="key">The identifier of the implemented command.</param>
public abstract class CommandSource<T>(string key) : Source {

	/// <summary>
	/// A dollar sign ($) used to identify an implementation of an <see cref="ISource"/> as a <see cref="CommandSource{T}"/>.
	/// </summary>
	protected const string Prefix = "$";

	/// <summary>
	/// Gets the Key that identifies this command source.
	/// </summary>
	protected string Key { get; private set; } = Check.NotEmpty(key, nameof(key));

	/// <summary>
	/// Validates the string specified is not null and matches this instances <see cref="Key"/>.
	/// </summary>
	/// <param name="selector">The selector text that could match this instances <see cref="Key"/>.</param>
	/// <returns>True if the selector is meant for this instance; otherwise false.</returns>
	protected virtual bool IsCommandSource(string? selector) {
		return
			!string.IsNullOrWhiteSpace(selector) &&
			string.Equals(selector, this.Key, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Implementors of this class should place their command(s) value-resolution here...
	/// </summary>
	/// <param name="command">The name of the command to resolve a value.</param>
	/// <returns>The value this source returns if the command was found; otherwise NULL.</returns>
	protected abstract T? ResolveValue(string command);

	/// <inheritdoc/>
	public override void Initialize(SmartFormatter smartFormatter) {
		this.Key = Prefix + this.Key.TrimStart(Prefix.ToCharArray());
		base.Initialize(smartFormatter);
	}

	/// <inheritdoc/>
	public override bool TryEvaluateSelector(ISelectorInfo selectorInfo) {

		if (this.TrySetResultForNullableOperator(selectorInfo)) {
			return true;
		}

		if (selectorInfo.SelectorIndex == 0
			&& this.IsCommandSource(selectorInfo.SelectorText)) {
			selectorInfo.Result = selectorInfo.CurrentValue;
			return true;
		}

		if (selectorInfo.SelectorIndex == 1) {

			var rootSelector = selectorInfo.Placeholder?.GetSelectors()[0]?.RawText;
			if (this.IsCommandSource(rootSelector)) {

				var cmd = selectorInfo.SelectorText ?? "";
				var val = this.ResolveValue(cmd);
				if (val != null) {
					selectorInfo.Result = val;
					return true;
				}

			}

		}

		return false;
	}

}

#endregion