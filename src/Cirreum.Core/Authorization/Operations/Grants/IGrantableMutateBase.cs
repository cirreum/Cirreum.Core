namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Base detection interface for grant-aware mutations. Carries the scalar
/// <c>OwnerId</c> that the grant evaluator enforces before the handler runs.
/// Does not inherit <see cref="Conductor.IAuthorizableRequest"/> — used by
/// <see cref="OperationGrantEvaluator"/> for runtime detection via <c>is</c> casts.
/// </summary>
public interface IGrantableMutateBase {

	/// <summary>
	/// The identifier of the target owner (tenant/company). Enforced <c>OwnerId ∈ reach</c>
	/// before the handler. Enriched from single-element reach when null (Tenant callers).
	/// </summary>
	string? OwnerId { get; set; }
}
