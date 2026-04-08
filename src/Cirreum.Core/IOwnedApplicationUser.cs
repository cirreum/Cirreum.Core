namespace Cirreum;

/// <summary>
/// An application user that belongs to a coarse-grained owner scope (tenant/company).
/// Enables the grant evaluator to resolve the caller's tenant ID directly from
/// the app user without requiring a custom evaluator subclass.
/// </summary>
public interface IOwnedApplicationUser : IApplicationUser {

	/// <summary>
	/// The identifier of the tenant/company this user belongs to. May be null for
	/// globally-scoped users or during enrichment handoff.
	/// </summary>
	string? OwnerId { get; }
}
