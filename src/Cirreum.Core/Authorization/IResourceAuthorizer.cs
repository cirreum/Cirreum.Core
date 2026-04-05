namespace Cirreum.Authorization;

/// <summary>
/// Defines an authorizer that enforces authorization rules for a specific resource type.
/// </summary>
/// <typeparam name="TResource">The type of resource to authorize. Must implement <see cref="IAuthorizableResource"/>.</typeparam>
/// <remarks>
/// <para>
/// Resource authorizers enforce permissions by evaluating user roles, state, and resource properties.
/// While commonly used with commands/queries, authorizers can be used with any resource type that
/// implements <see cref="IAuthorizableResource"/>.
/// </para>
/// <para>
/// This flexible design allows for Attribute-Based Access Control (ABAC) to be enforced anywhere
/// in the application, not just within the Conductor pipeline.
/// </para>
/// <para>
/// Domain developers should inherit from <see cref="ResourceAuthorizerBase{TResource}"/> rather than
/// implementing this interface directly.
/// </para>
/// <example>
/// <code>
/// // Can be used with commands
/// public class CreateOrderAuthorizer : ResourceAuthorizerBase&lt;CreateOrderCommand&gt;
///
/// // Or with domain entities
/// public class OrderAuthorizer : ResourceAuthorizerBase&lt;Order&gt;
///
/// // Or any other resource type
/// public class ReportAuthorizer : ResourceAuthorizerBase&lt;AnalyticsReport&gt;
/// </code>
/// </example>
/// </remarks>
public interface IResourceAuthorizer<TResource>
	where TResource : IAuthorizableResource;
