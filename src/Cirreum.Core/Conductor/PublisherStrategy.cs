namespace Cirreum.Conductor;

/// <summary>
/// Strategy for publishing notifications to handlers.
/// </summary>
public enum PublisherStrategy {
	/// <summary>
	/// Handlers are invoked one at a time, in order. 
	/// If one fails, subsequent handlers still execute.
	/// </summary>
	Sequential,

	/// <summary>
	/// Handlers are invoked one at a time, in order. 
	/// If one fails, subsequent handlers will not be executed.
	/// </summary>
	FailFast,

	/// <summary>
	/// All handlers are invoked simultaneously.
	/// Waits for all to complete before returning.
	/// </summary>
	Parallel,

	/// <summary>
	/// Handlers are invoked asynchronously without waiting for completion.
	/// If one fails, subsequent handlers still execute.
	/// Returns immediately after queueing.
	/// </summary>
	FireAndForget
}