namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the user state has a delegation context — i.e. the operation is being
/// invoked by an M2M actor acting on behalf of a subject. Used by authorizers for
/// operations that must only be invoked through delegation (e.g. agent-mediated channels
/// where direct sign-in is not the supported path).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// <para>
/// This is the explicit counterpart to the implicit fail-closed guard built into
/// <see cref="RequiresDelegationCheckAttribute"/>. Use this validator (or its rule-builder
/// extension <c>Delegated()</c>) when the operation requires a delegation context
/// without any further facet constraint.
/// </para>
/// </remarks>
public class DelegatedValidator<T> : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "HasDelegationValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> "Operation requires a delegation context; direct callers are not permitted.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		return userState is not null && userState.IsDelegated;
	}

}
