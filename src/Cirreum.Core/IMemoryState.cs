namespace Cirreum;

/// <summary>
/// An <see cref="IStateContainer"/> object intended to store state in-memory,
/// exposing dynamic properties via <see cref="IStateValueHandle{T}"/> instances.
/// </summary>
public interface IMemoryState : IStateContainer;