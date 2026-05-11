namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation, when invoked via delegation, requires the
/// delegation to be applied within the specified maximum age. Anti-replay / staleness gate.
/// </summary>
/// <remarks>
/// <para>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Inherits
/// the fail-closed precondition from <see cref="RequiresDelegationCheckAttribute"/> —
/// direct (non-delegated) callers fail with <c>DELEGATION_REQUIRED</c> before this
/// facet's freshness check runs.
/// </para>
/// <para>
/// The maximum age is the sum of <see cref="Seconds"/>, <see cref="Minutes"/>, and
/// <see cref="Hours"/> — set whichever named property reads most naturally.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDelegationWithin(Minutes = 5)]
/// public sealed record GetAccountBalance(string AccountId) : IAuthorizableOperation&lt;BalanceResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDelegationWithinAttribute : RequiresDelegationCheckAttribute {

	/// <summary>The seconds component of the maximum delegation age.</summary>
	public int Seconds { get; init; }

	/// <summary>The minutes component of the maximum delegation age.</summary>
	public int Minutes { get; init; }

	/// <summary>The hours component of the maximum delegation age.</summary>
	public int Hours { get; init; }

	/// <summary>
	/// Gets the composed maximum age as a <see cref="TimeSpan"/>. Sum of
	/// <see cref="Hours"/>, <see cref="Minutes"/>, and <see cref="Seconds"/>.
	/// </summary>
	public TimeSpan MaxAge => new(this.Hours, this.Minutes, this.Seconds);

	/// <inheritdoc/>
	protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
		var maxAge = this.MaxAge;
		var age = DateTimeOffset.UtcNow - actor.Delegation.DelegatedAt;

		if (age <= maxAge) {
			return null;
		}

		return new ValidationFailure(
			propertyName: nameof(RequiresDelegationWithinAttribute),
			errorMessage: $"Delegation is older than the allowed maximum age of {maxAge:g}.") {
			ErrorCode = "DELEGATION_STALE"
		};
	}

}
