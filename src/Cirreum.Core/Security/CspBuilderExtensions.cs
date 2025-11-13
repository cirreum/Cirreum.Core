namespace Cirreum.Security;

public static class CspBuilderExtensions {

	/// <summary>
	/// Adds the <paramref name="source"/> value as an allowed URI to the connect-src list.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <param name="source">The value to add to the connect-src</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddConnectSource(this ICspBuilder builder, string source) {
		builder.AddSource(CspDirectives.CONNECT_SRC, source);
		return builder;
	}

	/// <summary>
	/// Adds the <paramref name="source"/> value as an allowed URI to the script-src list.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <param name="source">The value to add to the script-src</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddScriptSource(this ICspBuilder builder, string source) {
		builder.AddSource(CspDirectives.SCRIPT_SRC, source);
		return builder;
	}

	/// <summary>
	/// Adds the 'unsafe-eval' to the scrip-src list. Not recommended.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddUnsafeEvalScript(this ICspBuilder builder) {
		builder.AddSource(CspDirectives.SCRIPT_SRC, "'unsafe-eval'");
		return builder;
	}
	/// <summary>
	/// Adds the 'wasm-unsafe-eval' to the script-src list.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddWasmUnsafeEvalScript(this ICspBuilder builder) {
		builder.AddSource(CspDirectives.SCRIPT_SRC, "'wasm-unsafe-eval'");
		return builder;
	}

	/// <summary>
	/// Adds the <paramref name="source"/> value as an allowed URI to the style-src list.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <param name="source">The value to add to the style-src</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddStyleSource(this ICspBuilder builder, string source) {
		builder.AddSource(CspDirectives.STYLE_SRC, source);
		return builder;
	}

	/// <summary>
	/// Adds the 'unsafe-inline' to the style-src list.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddUnsafeInlineStyle(this ICspBuilder builder) {
		builder.AddSource(CspDirectives.STYLE_SRC, "'unsafe-inline'");
		return builder;
	}

	/// <summary>
	/// Adds the <paramref name="source"/> as an allowed URI to the font-src list.
	/// </summary>
	/// <param name="builder">The <see cref="ICspBuilder"/> to extend.</param>
	/// <param name="source">The value to add to the font-src</param>
	/// <returns>The provided The <see cref="ICspBuilder"/>.</returns>
	public static ICspBuilder AddFontSource(this ICspBuilder builder, string source) {
		builder.AddSource(CspDirectives.FONT_SRC, source);
		return builder;
	}

}