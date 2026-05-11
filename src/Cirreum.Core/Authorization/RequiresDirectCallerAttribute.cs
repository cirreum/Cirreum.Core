namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation must be invoked by a direct caller — i.e.
/// not produced via delegation. Used for high-risk operations (wire transfers, sensitive
/// admin actions, audit-sensitive mutations) that should never be reachable through a
/// delegated channel.
/// </summary>
/// <remarks>
/// <para>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Fails fast
/// before grant resolution, object authorizers, or policy validators run.
/// </para>
/// <para>
/// The complement to <see cref="RequiresDelegationAttribute"/> — together they form the
/// "delegation channel" axis. Operations decorated with neither are delegation-neutral.
/// </para>
/// <para>
/// Name-paired with the framework's
/// <see cref="Validators.NotDelegatedValidator{T}"/> Stage 2
/// validator: same semantic, different pipeline stage and enforcement style (declarative
/// attribute vs. authorizer rule).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDirectCaller]
/// public sealed record InitiateWireTransfer(...) : IAuthorizableOperation;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDirectCallerAttribute : DelegationCheckAttribute {

	/// <inheritdoc/>
	public override ValidationFailure? Check(IUserState userState) {
		return userState.IsDelegated
			? new ValidationFailure(
				propertyName: nameof(RequiresDirectCallerAttribute),
				errorMessage: "Operation cannot be performed via delegation; caller must be authenticated directly.") {
				ErrorCode = "DELEGATION_DENIED"
			}
			: null;
	}

}