namespace Cirreum;

/// <summary>
/// Marker interface extending <see cref="IStatePersistence"/> for session state persistence.
/// </summary>
/// <remarks>
/// <para>
/// An implementation could persist to the browser's session storage.
/// </para>
/// </remarks>
public interface ISessionStatePersistence : IStatePersistence;