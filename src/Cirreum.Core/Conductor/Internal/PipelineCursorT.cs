namespace Cirreum.Conductor.Internal;

using Cirreum.Conductor;

/// <summary>
/// Per-request pipeline walker for typed-response requests. One allocation per request,
/// plus one delegate allocation bound to <see cref="Next"/> (reused for every interceptor
/// in the chain). Replaces the recursive lambda-per-level pattern that allocated a fresh
/// closure at each interceptor.
/// </summary>
/// <remarks>
/// Interceptors MUST call <c>next()</c> at most once per <c>HandleAsync</c> invocation —
/// the cursor's <see cref="_index"/> is mutable shared state and calling next twice advances
/// past the intended interceptor. All built-in interceptors comply. Custom interceptors
/// that need retry/loop/fan-out semantics must snapshot state and build their own cursor.
/// </remarks>
/// <typeparam name="TRequest">The concrete request type.</typeparam>
/// <typeparam name="TResponse">The typed response.</typeparam>
internal sealed class PipelineCursor<TRequest, TResponse>
	where TRequest : class, IRequest<TResponse> {

	private readonly IIntercept<TRequest, TResponse>[] _intercepts;
	private readonly IRequestHandler<TRequest, TResponse> _handler;
	private int _index;
	public readonly RequestHandlerDelegate<TRequest, TResponse> NextDelegate;

	public PipelineCursor(
		IIntercept<TRequest, TResponse>[] intercepts,
		IRequestHandler<TRequest, TResponse> handler) {

		this._intercepts = intercepts;
		this._handler = handler;
		this.NextDelegate = this.Next;
	}

	private Task<Result<TResponse>> Next(
		RequestContext<TRequest> context,
		CancellationToken cancellationToken) {

		if (this._index >= this._intercepts.Length) {
			return this._handler.HandleAsync(context.Request, cancellationToken);
		}
		var current = this._intercepts[this._index++];
		return current.HandleAsync(context, this.NextDelegate, cancellationToken);
	}
}
