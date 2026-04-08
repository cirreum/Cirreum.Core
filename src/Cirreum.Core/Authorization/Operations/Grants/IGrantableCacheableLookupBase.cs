namespace Cirreum.Authorization.Operations.Grants;

using Cirreum.Security;

/// <summary>
/// Base detection interface for grant-aware cacheable lookups. Extends
/// <see cref="IGrantableLookupBase"/> with a <see cref="CallerAuthenticationBoundary"/> discriminator
/// that the grant evaluator stamps after successful grant evaluation to isolate
/// per-authentication-boundary cache buckets.
/// </summary>
public interface IGrantableCacheableLookupBase : IGrantableLookupBase {

	/// <summary>
	/// The caller's <see cref="AuthenticationBoundary"/> at the time the grant evaluation ran.
	/// <see langword="null"/> before evaluation; non-null after a successful grant pass.
	/// Used purely as a cache-key discriminator to prevent cross-boundary bucket sharing
	/// (e.g., a Global operator's cached result must not be served to a Tenant caller).
	/// </summary>
	AuthenticationBoundary? CallerAuthenticationBoundary { get; set; }
}
