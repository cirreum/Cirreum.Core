namespace Cirreum.Authorization;
/// <summary>
/// Marker interface for resources that can participate in the authorization system.
/// </summary>
/// <remarks>
/// <para>
/// This interface serves as a marker to identify types that can participate in authorization checks.
/// Any type implementing this interface can be used with
/// <see cref="IAuthorizationEvaluator.Evaluate{TResource}(TResource, CancellationToken)"/> 
/// to execute any associated authorization validators or application authorization policies.
/// </para>
/// <para>
/// The implementing type:
/// </para>
/// <list type="bullet">
///     <item><description>Contains information relevant to authorization decisions</description></item>
///     <item><description>Should be subject to authorization rules before access is granted</description></item>
///     <item><description>Can be evaluated by the authorization system</description></item>
/// </list>
/// </remarks>
public interface IAuthorizableResource;