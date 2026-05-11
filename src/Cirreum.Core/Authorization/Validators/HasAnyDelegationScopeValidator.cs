namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the delegated scope contains at least one of the specified permissions.
/// Used by authorizers to allow flexible scope-bounded access (e.g. allow a loan-summary
/// read if the delegated scope contains <c>loans:read</c> OR <c>loans:summarize</c>).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// Short-circuits to pass for direct callers — the rule only constrains delegated invocations.
/// </remarks>
public class HasAnyDelegationScopeValidator<T>(
	params Permission[] permissions
) : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasAnyDelegationScopeValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"Delegated scope must include at least one of: {string.Join(", ", permissions.Select(p => p.ToString()))}.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		if (userState is null) {
			return false;
		}
		if (!userState.IsDelegated) {
			return true;        // direct caller — rule does not constrain
		}
		return userState.Actor!.Delegation.Scope.ContainsAny(permissions);
	}

}
