namespace Cirreum.Authorization;

/// <summary>
/// An authorizable resource that is scoped to a coarse-grained owner (tenant/company).
/// The Core-shipped <see cref="OwnerScopeEvaluatorBase"/> handles OwnerId enforcement and
/// <see cref="Security.AccessScope.Tenant"/> enrichment for resources that implement
/// this interface.
/// </summary>
/// <remarks>
/// <para>
/// <c>OwnerId</c> is a settable reference to the tenant/company scope the resource
/// belongs to. Typically:
/// <list type="bullet">
///   <item><description>Left null by clients and enriched by the owner-scope evaluator
///     (AccessScope.Tenant callers — evaluator stamps it from the caller's app user).</description></item>
///   <item><description>Required on writes from AccessScope.Global callers (cross-tenant
///     operator staff must name the target tenant explicitly).</description></item>
///   <item><description>Handler-trusted once authorization has succeeded — downstream code can rely on it.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IAuthorizableOwnerScopedResource : IAuthorizableResource {

	/// <summary>
	/// The identifier of the tenant/company that owns this resource.
	/// Null before authorization runs; non-null after a successful owner-scope evaluation.
	/// </summary>
	string? OwnerId { get; set; }
}
