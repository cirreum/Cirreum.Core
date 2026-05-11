namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Intermediate base for delegation-facet attributes — checks that further constrain a
/// delegation context once it is known to be present (allowed actor schemes, freshness,
/// evidence type, scope membership). Self-enforces the "delegation is mandatory"
/// precondition so a facet attribute cannot silently fail-open when applied without an
/// accompanying <see cref="RequiresDelegationAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Fail-closed contract.</b> Marking an operation with any
/// <see cref="RequiresDelegationCheckAttribute"/> derivative declares that the operation
/// is invokable only via delegation that additionally satisfies the facet's constraint.
/// A direct (non-delegated) caller fails the base check with
/// <c>ErrorCode = "DELEGATION_REQUIRED"</c> before <see cref="CheckDelegated"/> ever
/// runs, so facets are intersections — never unions — with the requirement that
/// delegation be present.
/// </para>
/// <para>
/// <b>Derivative responsibility.</b> Concrete facet attributes override
/// <see cref="CheckDelegated"/> with the facet-specific logic. They may assume
/// <see cref="IUserState.IsDelegated"/> is <see langword="true"/> and
/// <see cref="IUserState.Actor"/> is non-null when invoked.
/// </para>
/// <para>
/// <b>Pairing with <see cref="RequiresDelegationAttribute"/>.</b> Applying both on the
/// same operation is supported but redundant — the framework's
/// <c>DelegationConstraint</c> deduplicates <c>DELEGATION_REQUIRED</c> failures by
/// error code so a direct caller sees a single denial, not one per facet.
/// </para>
/// </remarks>
public abstract class RequiresDelegationCheckAttribute : DelegationCheckAttribute {

	/// <inheritdoc/>
	public sealed override ValidationFailure? Check(IUserState userState) {
		if (!userState.IsDelegated) {
			return new ValidationFailure(
				propertyName: this.GetType().Name,
				errorMessage: "Operation requires a delegation context; direct callers are not permitted.") {
				ErrorCode = "DELEGATION_REQUIRED"
			};
		}
		return this.CheckDelegated(userState, userState.Actor!);
	}

	/// <summary>
	/// Performs the facet-specific check against a confirmed delegation context.
	/// </summary>
	/// <param name="userState">
	/// The current invocation's user state. Guaranteed non-null, authenticated, and
	/// delegated (<see cref="IUserState.IsDelegated"/> is <see langword="true"/>) by the
	/// base <see cref="Check"/> override.
	/// </param>
	/// <param name="actor">
	/// The non-null actor context (<paramref name="userState"/>.<see cref="IUserState.Actor"/>),
	/// supplied as a parameter so derivatives avoid the redundant null-forgiving dereference.
	/// </param>
	/// <returns>
	/// <see langword="null"/> when the facet check passes; a populated
	/// <see cref="ValidationFailure"/> when it fails.
	/// </returns>
	protected abstract ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor);

}
