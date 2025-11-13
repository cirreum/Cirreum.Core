namespace Cirreum;
/// <summary>
/// Represents a structured postal address as defined in the OpenID Connect Core specification.
/// </summary>
public record UserProfileAddress {
	public string? StreetAddress { get; init; }
	public string? City { get; init; }
	public string? State { get; init; }
	public string? PostalCode { get; init; }
	public string? Country { get; init; }
}