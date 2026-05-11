namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation, when invoked via delegation, requires the
/// actor to have authenticated via one of the allowed schemes. Used to restrict
/// delegation to specific wire-credential types (e.g., allow only SignedRequest-grade
/// actors to invoke audit-sensitive reads, block weaker ApiKey-only delegation).
/// </summary>
/// <remarks>
/// <para>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Inherits
/// the fail-closed precondition from <see cref="RequiresDelegationCheckAttribute"/> —
/// direct (non-delegated) callers fail with <c>DELEGATION_REQUIRED</c> before this
/// facet's actor-scheme check runs.
/// </para>
/// <para>
/// Scheme matching is ordinal case-insensitive.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDelegationActor("SignedRequest")]
/// public sealed record GetAccountBalance(string AccountId) : IAuthorizableOperation&lt;BalanceResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDelegationActorAttribute : RequiresDelegationCheckAttribute {

	/// <summary>
	/// Initializes a new <see cref="RequiresDelegationActorAttribute"/> with the allowed
	/// authentication scheme(s) for the actor.
	/// </summary>
	/// <param name="allowedSchemes">
	/// One or more authentication schemes permitted for the actor when this operation
	/// is invoked via delegation (e.g., <c>"ApiKey"</c>, <c>"SignedRequest"</c>).
	/// </param>
	public RequiresDelegationActorAttribute(params string[] allowedSchemes) {
		ArgumentNullException.ThrowIfNull(allowedSchemes);
		this.AllowedSchemes = allowedSchemes;
	}

	/// <summary>
	/// The authentication schemes permitted for the actor.
	/// </summary>
	public string[] AllowedSchemes { get; }

	/// <inheritdoc/>
	protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
		var actorScheme = actor.Scheme;
		foreach (var allowed in this.AllowedSchemes) {
			if (string.Equals(actorScheme, allowed, StringComparison.OrdinalIgnoreCase)) {
				return null;
			}
		}

		return new ValidationFailure(
			propertyName: nameof(RequiresDelegationActorAttribute),
			errorMessage: $"Delegation must originate from one of the allowed actor schemes: {string.Join(", ", this.AllowedSchemes)}.") {
			ErrorCode = "DELEGATION_ACTOR_NOT_ALLOWED"
		};
	}

}
