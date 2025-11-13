namespace Cirreum.Conductor;

/// <summary>
/// Specifies the publishing strategy for a notification type.
/// </summary>
/// <remarks>
/// This attribute defines the default strategy used when publishing notifications of this type.
/// The strategy can still be overridden by passing an explicit strategy to <see cref="IPublisher.PublishAsync"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PublishingStrategyAttribute(PublisherStrategy strategy) : Attribute {
	/// <summary>
	/// Gets the publishing strategy for this notification type.
	/// </summary>
	public PublisherStrategy Strategy { get; } = strategy;
}