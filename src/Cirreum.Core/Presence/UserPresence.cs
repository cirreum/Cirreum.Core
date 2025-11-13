namespace Cirreum.Presence;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The current user's Presence information.
/// </summary>
/// <param name="Status">The user's presence status</param>
/// <param name="Activity">The user's presence activity</param>
/// <param name="Message">The user's presence status message</param>
public record UserPresence(
	[Required] PresenceStatus Status,
	string? Activity,
	string? Message) { }