namespace Cirreum.Authorization.Grants;

/// <summary>
/// Base detection interface for self-scoped authorization targets. Carries the resource <c>Id</c>
/// from which the grant evaluator extracts the <see cref="ExternalId"/> to compare
/// against the calling user's identity.
/// </summary>
/// <remarks>
/// <para>
/// Unlike owner-scoped kinds (Mutate/Lookup/Search) that resolve tenant-level
/// <see cref="AccessGrant"/>, Self-scoped requests perform a direct identity match:
/// <c>ExternalId == context.UserId</c>. No reach resolution is needed for the
/// happy path (owner accessing their own resource).
/// </para>
/// <para>
/// Admin/privilege bypass is supported via <see cref="IGrantResolver.ShouldBypassAsync"/>:
/// if the caller has bypass rights, they can access any user's resources regardless of
/// the identity match.
/// </para>
/// <para>
/// <b>Composite IDs.</b> When the persistence layer embeds the user identity within a
/// composite key (e.g., <c>{documentId}ζ{externalId}</c>), override <see cref="ExternalId"/>
/// to extract the identity portion. Override <see cref="IsValidId"/> to validate the
/// format of untrusted input before the evaluator attempts the identity match.
/// </para>
/// <para>
/// <b>Auto-enrichment.</b> When <see cref="Id"/> is null, the evaluator auto-enriches it
/// from the calling user's identity (<c>context.UserId</c>), allowing "get my profile" or
/// "update my profile" scenarios without requiring the caller to supply their own ID.
/// </para>
/// </remarks>
public interface IGrantableSelfBase {

	/// <summary>
	/// The resource identifier as supplied by the caller. May be a simple external ID
	/// or a composite key depending on the persistence layer.
	/// </summary>
	string? Id { get; set; }

	/// <summary>
	/// The external identity (IdP identifier) extracted from <see cref="Id"/>. The grant
	/// evaluator compares this value against <see cref="AuthorizationContext{TResource}.UserId"/>
	/// (which originates from <c>IUserState.Id</c>).
	/// Override when <see cref="Id"/> is a composite key to extract the identity portion.
	/// Defaults to <see cref="Id"/>.
	/// </summary>
	string? ExternalId => this.Id;

	/// <summary>
	/// Validates that <see cref="Id"/> is well-formed and safe to evaluate. The grant
	/// evaluator checks this before attempting the identity match. Override to enforce
	/// format constraints (e.g., composite key structure). Defaults to a non-null check.
	/// </summary>
	bool IsValidId => this.Id is not null;
}

