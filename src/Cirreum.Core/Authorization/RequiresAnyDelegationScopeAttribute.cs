namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation, when invoked via delegation, requires the
/// delegated scope to contain at least one of the specified <see cref="Permission"/>s.
/// </summary>
/// <remarks>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Inherits
/// the fail-closed precondition from <see cref="RequiresDelegationCheckAttribute"/> —
/// direct (non-delegated) callers fail with <c>DELEGATION_REQUIRED</c> before this
/// facet's scope check runs. Permission strings are parsed to <see cref="Permission"/>
/// instances lazily on first invocation and cached on the attribute instance (which is
/// itself cached per type by the runtime).
/// </remarks>
/// <example>
/// <code>
/// [RequiresAnyDelegationScope("loans:read", "loans:summarize")]
/// public sealed record GetLoanSummary(string LoanId) : IAuthorizableOperation&lt;LoanSummary&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresAnyDelegationScopeAttribute : RequiresDelegationCheckAttribute {

	private Permission[]? _parsed;

	/// <summary>
	/// Initializes a new <see cref="RequiresAnyDelegationScopeAttribute"/> with the
	/// permission strings — any one of which must be present in the delegated scope.
	/// </summary>
	/// <param name="permissions">One or more permission strings in <c>"feature:operation"</c> form.</param>
	public RequiresAnyDelegationScopeAttribute(params string[] permissions) {
		ArgumentNullException.ThrowIfNull(permissions);
		this.Permissions = permissions;
	}

	/// <summary>
	/// The permission strings — any one of which must be present in the delegated scope.
	/// </summary>
	public string[] Permissions { get; }

	/// <inheritdoc/>
	protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
		var parsed = this._parsed ??= [.. this.Permissions.Select(Permission.Parse)];
		if (actor.Delegation.Scope.ContainsAny(parsed)) {
			return null;
		}

		return new ValidationFailure(
			propertyName: nameof(RequiresAnyDelegationScopeAttribute),
			errorMessage: $"Delegated scope must include at least one of: {string.Join(", ", this.Permissions)}.") {
			ErrorCode = "DELEGATION_SCOPE_MISSING_ANY"
		};
	}

}
