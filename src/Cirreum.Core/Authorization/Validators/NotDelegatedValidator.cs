namespace Cirreum.Authorization.Validators;

using Cirreum.Security;
using FluentValidation;
using FluentValidation.Validators;

/// <summary>
/// Validates that the user state is not delegated — i.e. the operation is being
/// invoked by a direct caller, not by an M2M actor on behalf of a subject. Used by
/// authorizers for operations that must not be invoked through a delegated channel
/// (e.g. wire transfers, high-risk mutations, sensitive admin actions).
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// Fails when <see cref="IUserState.IsDelegated"/> is <see langword="true"/>. The
/// declarative attribute pairing is <see cref="RequiresDirectCallerAttribute"/>.
/// </remarks>
public class NotDelegatedValidator<T> : PropertyValidator<T, IUserState> {

	/// <inheritdoc/>
	public override string Name => "NotDelegatedValidator";

	/// <inheritdoc/>
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> "Operation cannot be performed via delegation; caller must be authenticated directly.";

	/// <inheritdoc/>
	public override bool IsValid(ValidationContext<T> context, IUserState userState) {
		return userState is not null && !userState.IsDelegated;
	}

}