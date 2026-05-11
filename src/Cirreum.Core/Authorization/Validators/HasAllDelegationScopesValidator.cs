namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the delegated scope contains all of the specified permissions. Used by
/// authorizers for operations that require multiple distinct capabilities to be present
/// in the delegated scope simultaneously (e.g. a cross-reference report that needs both
/// <c>loans:read</c> AND <c>accounts:read</c> in the delegated scope).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// Short-circuits to pass for direct callers — the rule only constrains delegated invocations.
/// </remarks>
public class HasAllDelegationScopesValidator<T>(
	params Permission[] permissions
) : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasAllDelegationScopesValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"Delegated scope must include all of: {string.Join(", ", permissions.Select(p => p.ToString()))}.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		if (userState is null) {
			return false;
		}
		if (!userState.IsDelegated) {
			return true;        // direct caller — rule does not constrain
		}
		return userState.Actor!.Delegation.Scope.ContainsAll(permissions);
	}

}