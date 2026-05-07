namespace Cirreum.RemoteServices;

/// <summary>
/// Represents the lifecycle state of an <see cref="IRemoteConnection"/>.
/// </summary>
/// <remarks>
/// Reported via <see cref="IRemoteConnection.State"/> and the
/// <see cref="IRemoteConnection.StateChanged"/> event so consumers can render UI affordances
/// (connecting spinners, "reconnecting" toasts, "offline" banners) without polling.
/// </remarks>
public enum RemoteConnectionState {

	/// <summary>The connection has not been opened, has been closed, or has terminated.</summary>
	Disconnected = 0,

	/// <summary>The connection is in the process of being established for the first time.</summary>
	Connecting = 1,

	/// <summary>The connection is open and able to send and receive messages.</summary>
	Connected = 2,

	/// <summary>The connection was lost and is being re-established automatically.</summary>
	Reconnecting = 3,

	/// <summary>The connection is being closed by request and has not yet completed.</summary>
	Disconnecting = 4

}
