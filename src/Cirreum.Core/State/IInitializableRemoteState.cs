namespace Cirreum.State;

/// <summary>
/// Defines a remote state object that participates in application startup initialization.
/// </summary>
/// <remarks>
/// <para>
/// States implementing this interface are automatically discovered and initialized
/// during application startup by the <see cref="IInitializationOrchestrator"/>.
/// </para>
/// <para>
/// This interface combines <see cref="IRemoteState"/> (for data loading and state management)
/// with <see cref="IInitializable"/> (for ordered startup initialization with progress tracking).
/// </para>
/// <para>
/// The <see cref="IInitializable.InitializeAsync"/> implementation should delegate to
/// <see cref="IRemoteState.LoadAsync(CancellationToken)"/>.
/// </para>
/// </remarks>
/// <seealso cref="IInitializationOrchestrator"/>
/// <seealso cref="IActivityState"/>
public interface IInitializableRemoteState : IRemoteState, IInitializable;