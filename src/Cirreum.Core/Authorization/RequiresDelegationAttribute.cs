namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation must be invoked via delegation — an M2M actor
/// acting on behalf of a subject. Used for operations that only have meaning in a
/// delegated context (e.g., IVA tool calls, agent-mediated workflows) where direct
/// caller invocation is not the supported path.
/// </summary>
/// <remarks>
/// <para>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Fails fast
/// before grant resolution, object authorizers, or policy validators run.
/// </para>
/// <para>
/// The complement to <see cref="RequiresDirectCallerAttribute"/>. Operations decorated
/// with neither are delegation-neutral.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDelegation]
/// [RequiresFreshDelegation(Minutes = 2)]
/// public sealed record IvaCheckBalance(string AccountId) : IAuthorizableOperation&lt;BalanceResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDelegationAttribute : DelegationCheckAttribute {

	/// <inheritdoc/>
	public override ValidationFailure? Check(IUserState userState) {
		return userState.IsDelegated
			? null
			: new ValidationFailure(
				propertyName: nameof(RequiresDelegationAttribute),
				errorMessage: "Operation requires a delegation context; direct callers are not permitted.") {
				ErrorCode = "DELEGATION_REQUIRED"
			};
	}

}
