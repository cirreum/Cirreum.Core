namespace Cirreum.RemoteServices;

/// <summary>
/// Event payload for <see cref="IRemoteConnection.StateChanged"/>.
/// </summary>
/// <param name="PreviousState">The state before the transition.</param>
/// <param name="NewState">The state after the transition.</param>
public sealed record RemoteConnectionStateChangedEventArgs(
	RemoteConnectionState PreviousState,
	RemoteConnectionState NewState);