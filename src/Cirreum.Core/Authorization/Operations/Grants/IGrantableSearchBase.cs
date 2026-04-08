namespace Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Base detection interface for grant-aware cross-owner queries. Carries the plural
/// <c>OwnerIds</c> set that the grant evaluator validates and stamps before the handler runs.
/// </summary>
/// <remarks>
/// <para>
/// The defining characteristic is cross-owner reach, not result cardinality — a search
/// operation may return a single aggregate row (e.g., a cross-tenant count) or thousands
/// of records. What matters is that the query's data scope spans multiple owners.
/// </para>
/// </remarks>
public interface IGrantableSearchBase {

	/// <summary>
	/// The identifiers of the owners (tenants/companies) the caller is reading from. When
	/// null, the grant evaluator stamps it from reach. When non-null, enforced
	/// <c>OwnerIds ⊆ reach</c>. Handler-trusted after authorization succeeds.
	/// </summary>
	IReadOnlyList<string>? OwnerIds { get; set; }
}