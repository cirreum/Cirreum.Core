namespace Cirreum;

using System.Security.Claims;
using System.Threading.Tasks;

/// <summary>
/// Base interface for profile enrichers.
/// </summary>
public interface IUserProfileEnricher {
	/// <summary>
	/// Enriches the specified user profile using the supplied identity.
	/// </summary>
	/// <param name="profile">The <see cref="UserProfile"/> being enriched.</param>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> providing the claims.</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	Task EnrichProfileAsync(UserProfile profile, ClaimsIdentity identity);
}