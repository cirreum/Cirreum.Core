namespace Cirreum.Authorization.Grants;

using Cirreum.Security;

/// <summary>
/// Base detection interface for grant-aware cacheable lookups. Extends
/// <see cref="IGrantableLookupBase"/> with a <see cref="CallerAccessScope"/> discriminator
/// that the grant evaluator stamps after successful authorization to isolate
/// per-scope cache buckets.
/// </summary>
public interface IGrantableCacheableLookupBase : IGrantableLookupBase {

	/// <summary>
	/// The caller's <see cref="AccessScope"/> at the time authorization ran.
	/// Null before authorization; non-null after a successful grant evaluation.
	/// Used purely as a cache-key discriminator to prevent cross-scope bucket sharing.
	/// </summary>
	AccessScope? CallerAccessScope { get; set; }
}
