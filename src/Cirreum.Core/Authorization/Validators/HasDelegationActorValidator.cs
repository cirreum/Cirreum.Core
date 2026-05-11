namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the delegating actor authenticated via one of the allowed schemes.
/// Used by authorizers that permit delegation but restrict it to specific wire-credential
/// types (e.g. allow only <c>SignedRequest</c>-authenticated actors to invoke audit-sensitive
/// reads — block <c>ApiKey</c>-only delegation for the same operation).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// <para>
/// Short-circuits to pass for direct callers — the rule only constrains delegated invocations.
/// Combine with <see cref="DelegatedValidator{T}"/> when the operation must be
/// delegated AND limited to specific schemes.
/// </para>
/// <para>
/// Scheme matching is ordinal case-insensitive to match the existing scheme-comparison
/// conventions in <see cref="IApplicationUserResolver.Scheme"/> dispatch.
/// </para>
/// </remarks>
public class HasDelegationActorValidator<T>(
	params string[] allowedSchemes
) : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasDelegationActorValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"Delegation must originate from one of the allowed actor schemes: {string.Join(", ", allowedSchemes)}.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		if (userState is null) {
			return false;
		}
		if (!userState.IsDelegated) {
			return true;        // direct caller — rule does not constrain
		}
		var actorScheme = userState.Actor!.Scheme;
		foreach (var allowed in allowedSchemes) {
			if (string.Equals(actorScheme, allowed, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}
		return false;
	}

}