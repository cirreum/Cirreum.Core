namespace Cirreum;

/// <summary>
/// An interface for initializing domain context during application startup.
/// </summary>
public interface IDomainContextInitializer {
	/// <summary>
	/// Initializes the domain context.
	/// </summary>
	void Initialize();
}