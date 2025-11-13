namespace Cirreum.Authorization;
/// <summary>
/// Marker interface for resources that can participate in the authorization system.
/// </summary>
/// <remarks>
/// <para>
/// This interface serves as a marker to identify types that should be subject to authorization checks.
/// Any type implementing this interface can be used with <see cref="IAuthorizationEvaluator.Enforce{TResource}"/> 
/// and can have corresponding authorization validators.
/// </para>
/// <para>
/// While this interface doesn't define any members, implementing it signifies that a type:
/// </para>
/// <list type="bullet">
///     <item><description>Contains information relevant to authorization decisions</description></item>
///     <item><description>Should be subject to authorization rules before access is granted</description></item>
///     <item><description>Can be evaluated by the authorization system</description></item>
/// </list>
/// <para>
/// Common implementations include:
/// </para>
/// <list type="bullet">
///     <item><description>Domain entities that require access control</description></item>
///     <item><description>API requests/commands that modify protected resources</description></item>
///     <item><description>View models representing protected information</description></item>
/// </list>
/// </remarks>
public interface IAuthorizableResource;