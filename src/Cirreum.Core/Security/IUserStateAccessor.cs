namespace Cirreum.Security;

/// <summary>
/// Resolves the <see cref="IUserState"/> for the current user, scoped to the
/// active invocation.
/// </summary>
/// <remarks>
/// <para>
/// This service depends on an <b>active invocation context</b> (HTTP request,
/// SignalR Hub method, WebSocket frame, queue trigger, scheduled task, etc.)
/// populated by the host's invocation-source adapter at the inbound seam. It is
/// intended to be called from within an executing invocation — typically from
/// CQRS handlers, authorization evaluators, repositories, or other invocation-scoped
/// framework code.
/// </para>
/// <para>
/// The <see cref="IUserState"/> returned by <see cref="GetUser"/> reflects the
/// <b>current invocation only</b>. Consumers must not capture the result in
/// singletons, static state, or fire-and-forget background work — analogous to the
/// standard <c>IHttpContextAccessor.HttpContext</c> capture warning. A captured
/// reference may read as the wrong user (or a disposed scope) when later inspected
/// against an unrelated invocation, and the leak surface is silent until that read.
/// </para>
/// <para>
/// When called outside an active invocation (e.g., a long-running
/// <c>IHostedService</c>, a timer-driven worker, or any pure background path that has
/// not synthesized an invocation), the returned <see cref="IUserState"/> is the
/// anonymous user. This is a contract guarantee, not an implementation detail —
/// callers may rely on a non-null, sensibly-defaulted result. Code that requires a
/// specific authenticated identity in background work should synthesize its own
/// invocation explicitly rather than expecting <see cref="IUserStateAccessor"/> to
/// surface one.
/// </para>
/// <para>
/// Lifetime: register as scoped. Per-invocation scope is established by the host's
/// invocation-source adapter; the accessor's implementation reads from the
/// scope-local context.
/// </para>
/// </remarks>
public interface IUserStateAccessor {

	/// <summary>
	/// Gets the <see cref="IUserState"/> for the current invocation.
	/// </summary>
	/// <returns>
	/// The current invocation's user state. Reflects the *current* invocation only;
	/// must not be captured beyond the invocation's scope. Returns the anonymous user
	/// when called outside an active invocation.
	/// </returns>
	ValueTask<IUserState> GetUser();

}
