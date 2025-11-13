namespace Cirreum.SmartExtensions;

using SmartFormat.Core.Extensions;

/// <summary>
/// A SmartFormat Extension to support using a user provided Func as an <see cref="ISource"/> during string
/// formatting operations.
/// </summary>
public class FuncSource : ISource {

	/// <inheritdoc/>
	public bool TryEvaluateSelector(ISelectorInfo selectorInfo) {

		var current = selectorInfo.CurrentValue;
		var selector = selectorInfo.SelectorText;

		if (current is Func<string, object> objFunc) {

			var value = objFunc(selector ?? string.Empty);
			if (value != null) {
				selectorInfo.Result = value;
				return true;
			}

		}

		if (current is Func<string, string> strFunc) {

			var value = strFunc(selector ?? string.Empty);
			if (value != null) {
				selectorInfo.Result = value;
				return true;
			}

		}

		return false;

	}

}