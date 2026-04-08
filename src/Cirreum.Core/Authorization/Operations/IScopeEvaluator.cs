namespace Cirreum.Authorization.Operations;

using FluentValidation.Results;

/// <summary>
/// Evaluates a caller's scope against an authorizable resource. Stage 1, Step 1 of
/// the authorization pipeline — runs after the optional owner-scope gate.
/// </summary>
/// <remarks>
/// <para>
/// Scope evaluators are app-supplied, pure checks against
/// <see cref="Security.AccessScope"/> and resource shape. They do NOT know
/// about override roles or <see cref="IApplicationUser"/> — those concerns live
/// only in <see cref="Operations.Grants.OperationGrantEvaluator"/>.
/// </para>
/// <para>
/// Zero or more evaluators may be registered. They run in registration order; the
/// first failure short-circuits stage 1.
/// </para>
/// </remarks>
public interface IScopeEvaluator {

	/// <summary>
	/// Evaluates the caller's scope against the resource.
	/// </summary>
	/// <typeparam name="TResource">The type of resource being evaluated.</typeparam>
	/// <param name="context">The authorization context.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A <see cref="ValidationResult"/> representing the scope decision.</returns>
	Task<ValidationResult> EvaluateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken = default)
		where TResource : notnull, IAuthorizableObject;
}
