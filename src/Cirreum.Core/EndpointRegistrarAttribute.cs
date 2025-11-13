namespace Cirreum;

/// <summary>
/// Marks a static method for automatic discovery and inclusion in the application's endpoint registration process.
/// The decorated method is responsible for registering a related group of API endpoints with the routing system.
/// </summary>
/// <remarks>
/// <para>
/// Methods decorated with this attribute must follow this signature:
/// <c>public static void {MethodName}(IEndpointRouteBuilder app)</c>
/// </para>
/// <para>
/// During compilation, the Cirreum.FeatureEndpoints source generator scans for methods decorated with this attribute
/// and generates a centralized registration method that calls each of these endpoint registrars. This approach
/// enables clean organization of endpoints by feature while maintaining compatibility with AOT compilation and trimming.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderEndpoints
/// {
///     [EndpointRegistrar]
///     public static void Map(IEndpointRouteBuilder app)
///     {
///         var orderGroup = app.MapGroup("/orders").WithTags("Orders");
///         
///         orderGroup.MapGet("/", GetOrders);
///         orderGroup.MapGet("/{id}", GetOrderById);
///         orderGroup.MapPost("/", CreateOrder);
///         orderGroup.MapPut("/{id}", UpdateOrder);
///         orderGroup.MapDelete("/{id}", DeleteOrder);
///     }
///     
///     private static async Task&lt;IResult&gt; GetOrders(...) { ... }
///     private static async Task&lt;IResult&gt; GetOrderById(...) { ... }
///     private static async Task&lt;IResult&gt; CreateOrder(...) { ... }
///     private static async Task&lt;IResult&gt; UpdateOrder(...) { ... }
///     private static async Task&lt;IResult&gt; DeleteOrder(...) { ... }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class EndpointRegistrarAttribute : Attribute;