namespace Cirreum;

/// <summary>
/// An <see cref="IPersistableStateContainer "/> object intended to be persisted to session storage via an 
/// <see cref="ISessionStatePersistence"/> service.
/// </summary>
/// <remarks>
/// <para>
/// An implementation could persist to the browser's session storage.
/// </para>
/// </remarks>
public interface ISessionState : IPersistableStateContainer;