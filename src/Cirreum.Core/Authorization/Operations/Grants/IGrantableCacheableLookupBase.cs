namespace Cirreum.Authorization.Operations.Grants;

using Cirreum.Security;

/// <summary>
/// Base detection interface for grant-aware cacheable lookups. Extends
/// <see cref="IGrantableLookupBase"/> with a <see cref="CallerAuthenticationScope"/> discriminator
/// that the grant evaluator stamps after successful authorization to isolate
/// per-scope cache buckets.
/// </summary>
public interface IGrantableCacheableLookupBase : IGrantableLookupBase {

	/// <summary>
	/// The caller's <see cref="AuthenticationScope"/> at the time authorization ran.
	/// Null before authorization; non-null after a successful grant evaluation.
	/// Used purely as a cache-key discriminator to prevent cross-scope bucket sharing.
	/// </summary>
	AuthenticationScope? CallerAuthenticationScope { get; set; }
}
