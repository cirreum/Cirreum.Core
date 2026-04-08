namespace Cirreum.Authorization;

/// <summary>
/// Scoped-per-request holder for the resolved <see cref="AuthorizationContext"/>. The authorization
/// pipeline populates it via <see cref="Set"/> after resolving the caller's effective roles;
/// downstream consumers (e.g., <c>ResourceAccessEvaluator</c>) read <see cref="Current"/> to
/// avoid redundant role resolution.
/// </summary>
/// <remarks>
/// <para>
/// The accessor is registered scoped so that each request gets its own instance. Before the
/// authorization pipeline runs, <see cref="Current"/> returns <see langword="null"/>.
/// </para>
/// <para>
/// Consumers should fall back to resolving roles independently when <see cref="Current"/> is
/// <see langword="null"/> — this covers background jobs and notification handlers where the
/// authorization pipeline does not run.
/// </para>
/// </remarks>
public interface IAuthorizationContextAccessor {

	/// <summary>
	/// The resolved authorization context for the current request, or <see langword="null"/>
	/// when the authorization pipeline has not yet run.
	/// </summary>
	AuthorizationContext? Current { get; }

	/// <summary>
	/// Stamps the resolved authorization context for the current request. Called by
	/// <c>DefaultAuthorizationEvaluator</c> after building the context. Handlers do not call this.
	/// </summary>
	void Set(AuthorizationContext context);
}
