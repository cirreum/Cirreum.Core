namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the delegated scope contains the specified permission. Used by
/// authorizers to enforce per-operation scope-narrowing on delegated invocations
/// (e.g. require <c>loans:inspect</c> to be in the delegated scope before allowing
/// a loan-detail read invoked via an IVA actor).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// <para>
/// Short-circuits to pass for direct callers — the rule only constrains delegated invocations.
/// Direct callers' authorization is governed by their roles and the operation's
/// <see cref="RequiresGrantAttribute"/> declarations as usual.
/// </para>
/// <para>
/// The delegated scope is the framework-computed intersection of
/// (subject entitlements) ∩ (actor credential allowed scopes) ∩ (delegation policy allowed scopes).
/// This validator checks whether the operation's required permission survived all three.
/// </para>
/// </remarks>
public class HasDelegationScopeValidator<T>(
	Permission required
) : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasDelegationScopeValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"Delegated scope does not include the required permission '{required}'.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		if (userState is null) {
			return false;
		}
		if (!userState.IsDelegated) {
			return true;        // direct caller — rule does not constrain
		}
		return userState.Actor!.Delegation.Scope.Contains(required);
	}

}
