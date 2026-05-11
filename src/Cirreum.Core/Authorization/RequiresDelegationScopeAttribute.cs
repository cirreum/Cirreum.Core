namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation, when invoked via delegation, requires the
/// delegated scope to contain the specified <see cref="Permission"/>.
/// </summary>
/// <remarks>
/// <para>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Inherits
/// the fail-closed precondition from <see cref="RequiresDelegationCheckAttribute"/> —
/// direct (non-delegated) callers fail with <c>DELEGATION_REQUIRED</c> before this
/// facet's scope check runs. Direct-caller authorization is governed by roles and
/// <see cref="RequiresGrantAttribute"/> declarations as usual on operations not gated by
/// a delegation facet.
/// </para>
/// <para>
/// The delegated scope is the framework-computed intersection of (subject's entitlements)
/// ∩ (actor credential's allowed scopes) ∩ (delegation policy's allowed scopes).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDelegationScope("loans", "read")]
/// public sealed record GetLoanDetails(string LoanId) : IAuthorizableOperation&lt;LoanDetails&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDelegationScopeAttribute : RequiresDelegationCheckAttribute {

	/// <summary>
	/// Declares the required delegated-scope permission.
	/// </summary>
	/// <param name="feature">The domain feature (e.g., <c>"loans"</c>).</param>
	/// <param name="operation">The operation verb (e.g., <c>"read"</c>).</param>
	public RequiresDelegationScopeAttribute(string feature, string operation) {
		ArgumentException.ThrowIfNullOrWhiteSpace(feature);
		ArgumentException.ThrowIfNullOrWhiteSpace(operation);
		this.Required = new Permission(feature, operation);
	}

	/// <summary>
	/// The required <see cref="Permission"/> that must be present in the delegated scope.
	/// </summary>
	public Permission Required { get; }

	/// <inheritdoc/>
	protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
		if (actor.Delegation.Scope.Contains(this.Required)) {
			return null;
		}

		return new ValidationFailure(
			propertyName: nameof(RequiresDelegationScopeAttribute),
			errorMessage: $"Delegated scope does not include the required permission '{this.Required}'.") {
			ErrorCode = "DELEGATION_SCOPE_MISSING"
		};
	}

}
