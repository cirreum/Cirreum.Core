namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Declares that the decorated operation, when invoked via delegation, requires the
/// delegation to have been authorized via one of the specified evidence types. Used to
/// require specific evidence strengths for sensitive operations.
/// </summary>
/// <remarks>
/// <para>
/// Enforced at Stage 1 Step 1 by the framework's <c>DelegationConstraint</c>. Inherits
/// the fail-closed precondition from <see cref="RequiresDelegationCheckAttribute"/> —
/// direct (non-delegated) callers fail with <c>DELEGATION_REQUIRED</c> before this
/// facet's evidence-type check runs.
/// </para>
/// <para>
/// Evidence-type matching is ordinal case-insensitive. The evidence-type space is
/// app-defined; the framework does not enumerate valid values.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDelegationEvidence("ivr-session-validated", "voice-biometric-verified")]
/// public sealed record IvaTransferConfirm(...) : IAuthorizableOperation&lt;TransferResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class RequiresDelegationEvidenceAttribute : RequiresDelegationCheckAttribute {

	/// <summary>
	/// Initializes a new <see cref="RequiresDelegationEvidenceAttribute"/> with the allowed
	/// evidence-type identifier(s).
	/// </summary>
	/// <param name="allowedEvidenceTypes">
	/// One or more evidence-type identifiers permitted for delegation against this operation.
	/// </param>
	public RequiresDelegationEvidenceAttribute(params string[] allowedEvidenceTypes) {
		ArgumentNullException.ThrowIfNull(allowedEvidenceTypes);
		this.AllowedEvidenceTypes = allowedEvidenceTypes;
	}

	/// <summary>The evidence-type identifiers permitted for delegation against this operation.</summary>
	public string[] AllowedEvidenceTypes { get; }

	/// <inheritdoc/>
	protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
		var actualType = actor.Delegation.EvidenceType;
		foreach (var allowed in this.AllowedEvidenceTypes) {
			if (string.Equals(actualType, allowed, StringComparison.OrdinalIgnoreCase)) {
				return null;
			}
		}

		return new ValidationFailure(
			propertyName: nameof(RequiresDelegationEvidenceAttribute),
			errorMessage: $"Delegation evidence type is not one of the allowed values: {string.Join(", ", this.AllowedEvidenceTypes)}.") {
			ErrorCode = "DELEGATION_EVIDENCE_NOT_ALLOWED"
		};
	}

}
