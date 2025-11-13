namespace Cirreum.Messaging.Metrics;

/// <summary>
/// Defines a service for collecting and reporting messaging-related metrics.
/// </summary>
public interface IMessagingMetricsService : IDisposable {

	/// <summary>
	/// Records that a message was received for delivery.
	/// </summary>
	/// <param name="messageType">The type name of the message.</param>
	/// <param name="kind">The kind of message (Queue/Topic).</param>
	void RecordMessageReceived(string messageType, MessageTarget kind);

	/// <summary>
	/// Records that a message was queued for delivery.
	/// </summary>
	/// <param name="messageType">The type name of the message.</param>
	/// <param name="kind">The kind of message (Queue/Topic).</param>
	/// <param name="queueTimeMs">The duration of time to submit the message to the queue.</param>
	/// <param name="priority">The priority of the message</param>
	void RecordMessageQueued(string messageType, MessageTarget kind, long queueTimeMs, DistributedMessagePriority priority);

	/// <summary>
	/// Records that a message was dequeued for delivery.
	/// </summary>
	/// <param name="messageType">The type name of the message.</param>
	/// <param name="kind">The kind of message (Queue/Topic).</param>
	/// <param name="queueWaitTimeMs">The duration of time a message waited in the queue.</param>
	/// <param name="priority">The priority of the message</param>
	void RecordMessageDequeued(string messageType, MessageTarget kind, long queueWaitTimeMs, DistributedMessagePriority priority);

	/// <summary>
	/// Records that a message was successfully delivered.
	/// </summary>
	/// <param name="messageType">The type name of the message.</param>
	/// <param name="kind">The kind of message (Queue/Topic).</param>
	/// <param name="processingTimeMs">Time taken to process the message in milliseconds.</param>
	void RecordMessageDelivered(string messageType, MessageTarget kind, long processingTimeMs);

	/// <summary>
	/// Records that a message was successfully delivered as part of a batch.
	/// </summary>
	/// <param name="messageType">The type name of the message.</param>
	/// <param name="kind">The kind of message (Queue/Topic).</param>
	/// <param name="processingTimeMs">Time taken to process the message in milliseconds.</param>
	/// <param name="totalTimeMs">The end-to-end time from queue to delivery.</param>
	void RecordMessageDeliveredInBatch(
		string messageType,
		MessageTarget kind,
		long processingTimeMs,  // Estimated individual time
		long totalTimeMs); // End-to-end time from queue to delivery

	/// <summary>
	/// Records that a message delivery failed.
	/// </summary>
	/// <param name="messageType">The type name of the message.</param>
	/// <param name="kind">The kind of message (Queue/Topic).</param>
	/// <param name="errorType">The type of error that occurred.</param>
	/// <param name="processingTimeMs">Time taken to process the message in milliseconds.</param>
	void RecordMessageFailed(string messageType, MessageTarget kind, string errorType, long processingTimeMs);

	/// <summary>
	/// Records that a partial batch was created.
	/// </summary>
	/// <param name="batchCapacity">The configured size of a batch.</param>
	/// <param name="batchSize">The actual size of the batch.</param>
	void RecordPartialBatch(int batchCapacity, int batchSize);

	/// <summary>
	/// Records information about a processed batch.
	/// </summary>
	/// <param name="batchCapacity">The configured size of a batch.</param>
	/// <param name="batchSize">The actual size of the batch.</param>
	/// <param name="processingTimeMs">Time taken to process the batch in milliseconds.</param>
	/// <param name="successCount">The count of items that were successful.</param>
	/// <param name="failureCount">The count of items that were unsuccessful.</param>
	/// <param name="standardCount">The count of items that had a priority of <see cref="DistributedMessagePriority.Standard"/>.</param>
	/// <param name="timeSensitiveCount">The count of items that had a priority of <see cref="DistributedMessagePriority.TimeSensitive"/>.</param>
	/// <param name="systemCount">The count of items that had a priority of <see cref="DistributedMessagePriority.System"/>.</param>
	void RecordBatchProcessed(
		int batchCapacity,
		int batchSize,
		long processingTimeMs,
		int successCount,
		int failureCount,
		int standardCount,
		int timeSensitiveCount,
		int systemCount);

	/// <summary>
	/// Records the current queue depth.
	/// </summary>
	/// <param name="queueDepth">Number of messages currently in the queue.</param>
	Task RecordQueueDepth(int queueDepth);

}