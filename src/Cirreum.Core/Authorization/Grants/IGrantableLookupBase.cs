namespace Cirreum.Authorization.Grants;

/// <summary>
/// Base detection interface for grant-aware point-lookups. Carries the scalar
/// <c>OwnerId</c> that is checked post-fetch by the handler for existence-hiding
/// (Pattern C) or pre-flight by the grant evaluator when non-null.
/// </summary>
public interface IGrantableLookupBase {

	/// <summary>
	/// The identifier of the owner (tenant/company). When non-null, enforced
	/// <c>OwnerId ∈ reach</c> before the handler. When null, reach is stashed on
	/// <see cref="IAccessReachAccessor"/> and the handler checks the fetched entity's
	/// owner post-fetch.
	/// </summary>
	string? OwnerId { get; set; }
}
