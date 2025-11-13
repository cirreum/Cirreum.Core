namespace Cirreum.Authorization;

/// <summary>
/// Defines a validator that enforces authorization rules for a specific resource type.
/// </summary>
/// <typeparam name="TResource">The type of resource to validate. Must implement <see cref="IAuthorizableResource"/>.</typeparam>
/// <remarks>
/// <para>
/// Authorization validators enforce permissions by validating user roles, state, and resource properties.
/// While commonly used with commands/queries, validators can be used with any resource type that 
/// implements <see cref="IAuthorizableResource"/>.
/// </para>
/// <para>
/// This flexible design allows for Attribute-Based Access Control (ABAC) to be enforced anywhere
/// in the application, not just within the MediatR pipeline.
/// </para>
/// <para>
/// Domain developers should inherit from <see cref="AuthorizationValidatorBase{TResource}"/> rather than
/// implementing this interface directly.
/// </para>
/// <example>
/// <code>
/// // Can be used with commands
/// public class CreateOrderAuthorizor : AuthorizationValidator&lt;CreateOrderCommand&gt; 
/// 
/// // Or with domain entities
/// public class OrderAuthorizor : AuthorizationValidator&lt;Order&gt;
/// 
/// // Or any other resource type
/// public class ReportAuthorizor : AuthorizationValidator&lt;AnalyticsReport&gt;
/// </code>
/// </example>
/// </remarks>
public interface IAuthorizationResourceValidator<TResource>
	where TResource : IAuthorizableResource;