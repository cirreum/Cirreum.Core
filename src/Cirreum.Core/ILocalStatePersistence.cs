namespace Cirreum;

/// <summary>
/// Marker interface extending <see cref="IStatePersistence"/> for local state persistence.
/// </summary>
/// <remarks>
/// <para>
/// An implementation could persist to the browser's local storage.
/// </para>
/// </remarks>
public interface ILocalStatePersistence : IStatePersistence;