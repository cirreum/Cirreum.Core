namespace Cirreum;

/// <summary>
/// Defines the methods to save or restore state from storage.
/// </summary>
public interface IStatePersistence {
	Task SaveStateAsync();
	Task RestoreStateAsync();
}