namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Non-generic sidecar interface for grant-aware commands. Carries the scalar
/// <c>OwnerId</c> that the grant evaluator enforces before the handler runs.
/// </summary>
public interface IGrantedCommand {

	/// <summary>
	/// The identifier of the target owner (tenant/company). Enforced <c>OwnerId ∈ reach</c>
	/// before the handler. Enriched from single-element reach when null (Tenant callers).
	/// </summary>
	string? OwnerId { get; set; }
}

/// <summary>
/// Grant-aware command with no response. Composes foundation <see cref="IAuthorizableCommand"/>
/// with the <see cref="IGrantedCommand"/> sidecar and binds to <typeparamref name="TDomain"/>
/// for resolver matching.
/// </summary>
/// <typeparam name="TDomain">The bounded-context domain marker (e.g., <c>IIssueOperation</c>).</typeparam>
public interface IGrantedCommand<TDomain>
	: IAuthorizableCommand, IGrantedCommand
	where TDomain : class;

/// <summary>
/// Grant-aware command with a response. Composes foundation <see cref="IAuthorizableCommand{TResponse}"/>
/// with the <see cref="IGrantedCommand"/> sidecar and binds to <typeparamref name="TDomain"/>.
/// </summary>
/// <typeparam name="TDomain">The bounded-context domain marker.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public interface IGrantedCommand<TDomain, out TResponse>
	: IAuthorizableCommand<TResponse>, IGrantedCommand
	where TDomain : class;
