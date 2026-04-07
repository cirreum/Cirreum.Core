namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Base detection interface for grant-aware commands. Carries the scalar
/// <c>OwnerId</c> that the grant evaluator enforces before the handler runs.
/// Does not inherit <see cref="IAuthorizableCommand"/> — used by
/// <see cref="GrantEvaluator"/> for runtime detection via <c>is</c> casts.
/// </summary>
public interface IGrantedCommandBase {

	/// <summary>
	/// The identifier of the target owner (tenant/company). Enforced <c>OwnerId ∈ reach</c>
	/// before the handler. Enriched from single-element reach when null (Tenant callers).
	/// </summary>
	string? OwnerId { get; set; }
}

/// <summary>
/// Grant-aware command with no response. Composes foundation <see cref="IAuthorizableCommand"/>
/// with the <see cref="IGrantedCommandBase"/> detection surface. Developers implement this
/// single interface for void granted commands.
/// </summary>
public interface IGrantedCommand
	: IAuthorizableCommand, IGrantedCommandBase;

/// <summary>
/// Grant-aware command with a response. Composes foundation <see cref="IAuthorizableCommand{TResponse}"/>
/// with the <see cref="IGrantedCommandBase"/> detection surface. Developers implement this
/// single interface for granted commands that return a value.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public interface IGrantedCommand<out TResponse>
	: IAuthorizableCommand<TResponse>, IGrantedCommandBase;
