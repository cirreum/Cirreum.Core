namespace Cirreum.Conductor;

/// <summary>
/// Unified interface for dispatching requests and publishing notifications.
/// Combines <see cref="IDispatcher"/> and <see cref="IPublisher"/> for convenience.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface in Minimal API endpoints and services that need both request dispatching 
/// and notification publishing. For services that only need one or the other, prefer the specific 
/// interfaces (<see cref="IDispatcher"/> or <see cref="IPublisher"/>) to follow the Interface 
/// Segregation Principle.
/// </para>
/// <para>
/// This interface is particularly useful in Minimal API route handlers, endpoint filters, and 
/// application services that coordinate both commands/queries (via requests) and events (via notifications).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Minimal API endpoint using IConductor
/// app.MapPost("/api/orders", async (PlaceOrderRequest request, IConductor conductor) => {
///     // Dispatch command
///     var result = await conductor.DispatchAsync(new PlaceOrderCommand(request));
///     
///     if (result.IsSuccess) {
///         // Publish event
///         await conductor.PublishAsync(new OrderPlacedEvent(result.Value));
///         return Results.Ok(result.Value);
///     }
///     
///     return Results.BadRequest(result.Error);
/// });
/// 
/// // Service class using specific interface
/// public class OrderService(IDispatcher dispatcher) {
///     public Task&lt;Result&lt;Order&gt;&gt; CreateOrder(CreateOrderCommand command) 
///         => dispatcher.DispatchAsync(command);
/// }
/// </code>
/// </example>
public interface IConductor : IDispatcher, IPublisher {
	// Marker interface - inherits all members from IDispatcher and IPublisher
}