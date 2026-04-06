namespace Cirreum.Authorization.Grants;

/// <summary>
/// Declares the permission namespace for a grant-domain marker interface. The namespace
/// is used by <see cref="RequiresPermissionAttribute"/> to auto-resolve single-arg
/// permission declarations, by the reach cache for human-readable keys, and by
/// namespace validation to ensure all permissions on a granted resource belong to
/// the domain.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to the TDomain marker interface passed as the first type argument
/// to <see cref="IGrantedCommand{TDomain}"/>, <see cref="IGrantedRead{TDomain, TResponse}"/>,
/// <see cref="IGrantedList{TDomain, TResponse}"/>, and
/// <see cref="IGrantedCacheableRead{TDomain, TResponse}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [GrantDomain("issues")]
/// public interface IIssueOperation;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class GrantDomainAttribute(string @namespace) : Attribute {

	/// <summary>
	/// The permission namespace for this domain (e.g., <c>"issues"</c>, <c>"documents"</c>).
	/// Normalized to lowercase.
	/// </summary>
	public string Namespace { get; } = @namespace?.ToLowerInvariant()
		?? throw new ArgumentNullException(nameof(@namespace));
}
