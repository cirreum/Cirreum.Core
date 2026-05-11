namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the delegation was authorized via one of the allowed evidence types.
/// Used by authorizers to require specific evidence strengths for sensitive operations
/// (e.g. require <c>voice-biometric-verified</c> or <c>ivr-session-validated</c> for
/// balance-inspection reads; reject lower-assurance evidence types like <c>phone-pin</c>).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// <para>
/// Short-circuits to pass for direct callers — the rule only constrains delegated invocations.
/// Combine with <see cref="DelegatedValidator{T}"/> when the operation must be
/// delegated AND backed by a specific evidence type.
/// </para>
/// <para>
/// Evidence-type matching is ordinal case-insensitive. The evidence-type space is
/// app-defined; the framework does not enumerate valid values.
/// </para>
/// </remarks>
public class HasDelegationEvidenceValidator<T>(
	params string[] allowedEvidenceTypes
) : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasDelegationEvidenceValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"Delegation evidence type is not one of the allowed values: {string.Join(", ", allowedEvidenceTypes)}.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		if (userState is null) {
			return false;
		}
		if (!userState.IsDelegated) {
			return true;        // direct caller — rule does not constrain
		}
		var actualType = userState.Actor!.Delegation.EvidenceType;
		foreach (var allowed in allowedEvidenceTypes) {
			if (string.Equals(actualType, allowed, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}
		return false;
	}

}
