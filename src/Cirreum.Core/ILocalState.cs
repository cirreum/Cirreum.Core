namespace Cirreum;

/// <summary>
/// An <see cref="IPersistableStateContainer "/> object intended to be persisted to local storage via an 
/// <see cref="ILocalStatePersistence"/> service.
/// </summary>
/// <remarks>
/// <para>
/// An implementation could persist to the browser's local storage.
/// </para>
/// </remarks>
public interface ILocalState : IPersistableStateContainer;