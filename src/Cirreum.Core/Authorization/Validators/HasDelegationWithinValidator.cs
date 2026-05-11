namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the delegation context was applied within the specified maximum age.
/// Used by authorizers to enforce anti-replay / staleness gates on operations that
/// should only be invoked within a bounded window of the original upgrade event
/// (e.g. high-risk reads or mutations behind short-lived IVR session evidence).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// <para>
/// Short-circuits to pass for direct callers — the rule only constrains delegated invocations.
/// Combine with <see cref="DelegatedValidator{T}"/> when the operation must be
/// delegated AND fresh.
/// </para>
/// <para>
/// Age is computed against <see cref="DelegationMetadata.DelegatedAt"/> using
/// <see cref="DateTimeOffset.UtcNow"/>. The metadata's timestamp is stamped at the
/// upgrade orchestration point and is monotonic for the lifetime of the user state.
/// </para>
/// </remarks>
public class HasDelegationWithinValidator<T>(
	TimeSpan maxAge
) : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasDelegationWithinValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"Delegation is older than the allowed maximum age of {maxAge:g}.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		if (userState is null) {
			return false;
		}
		if (!userState.IsDelegated) {
			return true;        // direct caller — rule does not constrain
		}
		var age = DateTimeOffset.UtcNow - userState.Actor!.Delegation.DelegatedAt;
		return age <= maxAge;
	}

}
