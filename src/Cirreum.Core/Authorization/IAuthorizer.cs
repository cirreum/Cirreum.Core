namespace Cirreum.Authorization;

/// <summary>
/// Defines an authorizer that enforces authorization rules for a specific <see cref="IAuthorizableObject"/> type.
/// </summary>
/// <typeparam name="TAuthorizableObject">The type of <see cref="IAuthorizableObject"/> to authorize.</typeparam>
/// <remarks>
/// <para>
/// Authorizers enforce permissions by evaluating user roles, state, and object properties.
/// While commonly used with commands/queries, authorizers can be used with any type that
/// implements <see cref="IAuthorizableObject"/>.
/// </para>
/// <para>
/// This flexible design allows for Attribute-Based Access Control (ABAC) to be enforced anywhere
/// in the application, not just within the Conductor pipeline.
/// </para>
/// <para>
/// Domain developers should inherit from <see cref="AuthorizerBase{TAuthorizableObject}"/> rather than
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
/// // Or any other authorizable type
/// public class ReportAuthorizer : AuthorizerBase&lt;AnalyticsReport&gt;
/// </code>
/// </example>
/// </remarks>
public interface IAuthorizer<TAuthorizableObject>
	where TAuthorizableObject : IAuthorizableObject;
