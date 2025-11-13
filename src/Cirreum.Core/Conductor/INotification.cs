namespace Cirreum.Conductor;

/// <summary>
/// Marker interface for notifications that can be published to multiple handlers.
/// Unlike requests, notifications don't return values.
/// </summary>
public interface INotification;