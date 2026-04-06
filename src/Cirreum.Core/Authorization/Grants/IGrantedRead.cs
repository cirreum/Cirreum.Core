namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Non-generic sidecar interface for grant-aware point-reads. Carries the scalar
/// <c>OwnerId</c> that is checked post-fetch by the handler for existence-hiding
/// (Pattern C) or pre-flight by the grant evaluator when non-null.
/// </summary>
public interface IGrantedRead {

	/// <summary>
	/// The identifier of the owner (tenant/company). When non-null, enforced
	/// <c>OwnerId ∈ reach</c> before the handler. When null, reach is stashed on
	/// <see cref="IAccessReachAccessor"/> and the handler checks the fetched entity's
	/// owner post-fetch.
	/// </summary>
	string? OwnerId { get; set; }
}

/// <summary>
/// Grant-aware point-read. Composes foundation <see cref="IAuthorizableQuery{TResponse}"/>
/// with the <see cref="IGrantedRead"/> sidecar and binds to <typeparamref name="TDomain"/>.
/// </summary>
/// <typeparam name="TDomain">The bounded-context domain marker (e.g., <c>IIssueOperation</c>).</typeparam>
/// <typeparam name="TResponse">The type of response returned by the read.</typeparam>
public interface IGrantedRead<TDomain, out TResponse>
	: IAuthorizableQuery<TResponse>, IGrantedRead
	where TDomain : class;
