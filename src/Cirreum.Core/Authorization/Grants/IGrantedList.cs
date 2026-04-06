namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Non-generic sidecar interface for grant-aware cross-owner queries. Carries the plural
/// <c>OwnerIds</c> set that the grant evaluator validates and stamps before the handler runs.
/// </summary>
/// <remarks>
/// <para>
/// The defining characteristic is cross-owner reach, not result cardinality — a List
/// operation may return a single aggregate row (e.g., a cross-tenant count) or thousands
/// of records. What matters is that the query's data scope spans multiple owners.
/// </para>
/// </remarks>
public interface IGrantedList {

	/// <summary>
	/// The identifiers of the owners (tenants/companies) the caller is reading from. When
	/// null, the grant evaluator stamps it from reach. When non-null, enforced
	/// <c>OwnerIds ⊆ reach</c>. Handler-trusted after authorization succeeds.
	/// </summary>
	IReadOnlyList<string>? OwnerIds { get; set; }
}

/// <summary>
/// Grant-aware cross-owner query. Composes foundation <see cref="IAuthorizableQuery{TResponse}"/>
/// with the <see cref="IGrantedList"/> sidecar and binds to <typeparamref name="TDomain"/>.
/// </summary>
/// <typeparam name="TDomain">The bounded-context domain marker (e.g., <c>IIssueOperation</c>).</typeparam>
/// <typeparam name="TResponse">The type of response returned by the list operation.</typeparam>
public interface IGrantedList<TDomain, out TResponse>
	: IAuthorizableQuery<TResponse>, IGrantedList
	where TDomain : class;
