namespace Cirreum;

/// <summary>
/// State container that can be serialized/persisted
/// </summary>
public interface IPersistableStateContainer : IStateContainer {
	/// <summary>
	/// Serialize this instance to a string.
	/// </summary>
	/// <returns>The string.</returns>
	string SerializeToString();
	/// <summary>
	/// Deserialize this instance from a string.
	/// </summary>
	/// <param name="value">The string to deserialize from.</param>
	void DeserializeFromString(string value);

}