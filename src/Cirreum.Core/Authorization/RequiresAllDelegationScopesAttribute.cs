namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation, when invoked via delegation, requires the
/// delegated scope to contain all of the specified <see cref="Permission"/>s.
/// </summary>
/// <remarks>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Inherits
/// the fail-closed precondition from <see cref="RequiresDelegationCheckAttribute"/> —
/// direct (non-delegated) callers fail with <c>DELEGATION_REQUIRED</c> before this
/// facet's scope check runs. Permission strings are parsed to <see cref="Permission"/>
/// instances lazily on first invocation and cached on the attribute instance.
/// </remarks>
/// <example>
/// <code>
/// [RequiresAllDelegationScopes("loans:read", "accounts:read")]
/// public sealed record GetCrossReferenceReport(...) : IAuthorizableOperation&lt;Report&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresAllDelegationScopesAttribute : RequiresDelegationCheckAttribute {

	private Permission[]? _parsed;

	/// <summary>
	/// Initializes a new <see cref="RequiresAllDelegationScopesAttribute"/> with the
	/// permission strings — all of which must be present in the delegated scope.
	/// </summary>
	/// <param name="permissions">One or more permission strings in <c>"feature:operation"</c> form.</param>
	public RequiresAllDelegationScopesAttribute(params string[] permissions) {
		ArgumentNullException.ThrowIfNull(permissions);
		this.Permissions = permissions;
	}

	/// <summary>The permission strings — all of which must be present in the delegated scope.</summary>
	public string[] Permissions { get; }

	/// <inheritdoc/>
	protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
		var parsed = this._parsed ??= [.. this.Permissions.Select(Permission.Parse)];
		if (actor.Delegation.Scope.ContainsAll(parsed)) {
			return null;
		}

		return new ValidationFailure(
			propertyName: nameof(RequiresAllDelegationScopesAttribute),
			errorMessage: $"Delegated scope must include all of: {string.Join(", ", this.Permissions)}.") {
			ErrorCode = "DELEGATION_SCOPE_MISSING_ALL"
		};
	}

}
