namespace Cirreum.Messaging;
/// <summary>
/// Defines a property - Name and Data (.NET) Type
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="Type">The Data (.NET) Type of the property</param>
public record MessageProperty(
	string Name,
	string Type
);