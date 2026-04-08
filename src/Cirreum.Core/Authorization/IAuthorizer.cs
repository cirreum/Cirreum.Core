namespace Cirreum.Authorization;

/// <summary>
/// Defines an authorizer that enforces authorization rules for a specific resource type.
/// </summary>
/// <typeparam name="TResource">The type of resource to authorize. Must implement <see cref="IAuthorizableObject"/>.</typeparam>
/// <remarks>
/// <para>
/// Resource authorizers enforce permissions by evaluating user roles, state, and resource properties.
/// While commonly used with commands/queries, authorizers can be used with any resource type that
/// implements <see cref="IAuthorizableObject"/>.
/// </para>
/// <para>
/// This flexible design allows for Attribute-Based Access Control (ABAC) to be enforced anywhere
/// in the application, not just within the Conductor pipeline.
/// </para>
/// <para>
/// Domain developers should inherit from <see cref="AuthorizerBase{TResource}"/> rather than
/// implementing this interface directly.
/// </para>
/// <example>
/// <code>
/// // Can be used with commands
/// public class CreateOrderAuthorizer : AuthorizerBase&lt;CreateOrderCommand&gt;
///
/// // Or with domain entities
/// public class OrderAuthorizer : AuthorizerBase&lt;Order&gt;
///
/// // Or any other resource type
/// public class ReportAuthorizer : AuthorizerBase&lt;AnalyticsReport&gt;
/// </code>
/// </example>
/// </remarks>
public interface IAuthorizer<TResource>
	where TResource : IAuthorizableObject;
