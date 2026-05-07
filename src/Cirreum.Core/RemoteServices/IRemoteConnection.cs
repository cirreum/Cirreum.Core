namespace Cirreum.RemoteServices;

/// <summary>
/// Caller-side typed handle to a long-lived bidirectional connection with a remote endpoint.
/// Pairs with <see cref="RemoteClient"/> in the same family — where <see cref="RemoteClient"/>
/// abstracts request/response calls, this abstracts persistent bidirectional channels
/// (SignalR Hub clients, raw WebSocket clients, gRPC streaming, and similar).
/// </summary>
/// <remarks>
/// <para>
/// Cross-host: works in WASM apps connecting to a backend, in server-side microservices
/// subscribing to events from another service, and anywhere else a long-lived outbound
/// connection is needed.
/// </para>
/// <para>
/// The interface is intentionally transport-agnostic — concrete impls in the
/// <c>Cirreum.Runtime.Invocation.{Source}.Wasm</c> family adapt SignalR's
/// <c>HubConnection</c>, raw <c>ClientWebSocket</c>, or other transports onto this contract.
/// </para>
/// </remarks>
public interface IRemoteConnection {

	/// <summary>Adapter-assigned identifier for the connection. Stable across reconnects within the same logical session.</summary>
	string ConnectionId { get; }

	/// <summary>The current connection lifecycle state.</summary>
	RemoteConnectionState State { get; }

	/// <summary>Raised whenever <see cref="State"/> transitions.</summary>
	event EventHandler<RemoteConnectionStateChangedEventArgs>? StateChanged;

	/// <summary>
	/// Open the connection to the remote endpoint. Idempotent for already-connected
	/// instances; throws if the connection has been disposed.
	/// </summary>
	Task ConnectAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Close the connection gracefully. Safe to call when already disconnected.
	/// </summary>
	Task DisconnectAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Register a typed handler for inbound messages routed by <paramref name="method"/>.
	/// Returns an <see cref="IDisposable"/> that unsubscribes the handler when disposed.
	/// </summary>
	/// <typeparam name="T">The expected message payload type.</typeparam>
	/// <param name="method">The method/event name as routed by the remote endpoint.</param>
	/// <param name="handler">Async handler invoked for each matching inbound message.</param>
	IDisposable On<T>(string method, Func<T, Task> handler);

	/// <summary>
	/// Send a typed message to the remote endpoint addressed by <paramref name="method"/>.
	/// </summary>
	/// <typeparam name="T">The payload type.</typeparam>
	/// <param name="method">The method/event name routed by the remote endpoint.</param>
	/// <param name="payload">The message payload.</param>
	/// <param name="cancellationToken">Cancellation token for the send operation.</param>
	Task SendAsync<T>(string method, T payload, CancellationToken cancellationToken = default);

}