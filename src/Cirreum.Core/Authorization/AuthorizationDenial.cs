namespace Cirreum.Authorization;

/// <summary>
/// Structured representation of an authorization denial.
/// </summary>
/// <param name="Code">Stable machine code (see <see cref="DenyCodes"/>).</param>
/// <param name="Message">Safe-for-any-audience message.</param>
/// <param name="DebugDetail">Development-only diagnostic context. Null in production.</param>
public sealed record AuthorizationDenial(
	string Code,
	string Message,
	string? DebugDetail = null);
