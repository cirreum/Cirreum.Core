namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Base detection interface for grant-aware cross-owner queries. Carries the plural
/// <c>OwnerIds</c> set that the grant evaluator validates and stamps before the handler runs.
/// </summary>
/// <remarks>
/// <para>
/// The defining characteristic is cross-owner reach, not result cardinality — a List
/// operation may return a single aggregate row (e.g., a cross-tenant count) or thousands
/// of records. What matters is that the query's data scope spans multiple owners.
/// </para>
/// </remarks>
public interface IGrantedListBase {

	/// <summary>
	/// The identifiers of the owners (tenants/companies) the caller is reading from. When
	/// null, the grant evaluator stamps it from reach. When non-null, enforced
	/// <c>OwnerIds ⊆ reach</c>. Handler-trusted after authorization succeeds.
	/// </summary>
	IReadOnlyList<string>? OwnerIds { get; set; }
}

/// <summary>
/// Grant-aware cross-owner query. Composes foundation <see cref="IAuthorizableQuery{TResponse}"/>
/// with the <see cref="IGrantedListBase"/> detection surface. Developers implement this
/// single interface for granted list queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the list operation.</typeparam>
public interface IGrantedList<out TResponse>
	: IAuthorizableQuery<TResponse>, IGrantedListBase;
