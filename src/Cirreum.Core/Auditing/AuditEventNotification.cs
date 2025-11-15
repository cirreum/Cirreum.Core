namespace Cirreum.Auditing;

using Cirreum.Messaging;

public static class AuditEventNotificationDefinitions {
	public const string MessageId = "cirreum.auditing.audit-event-notification";
	public const string MessageVersion = "1";
}

[MessageDefinition(
	AuditEventNotificationDefinitions.MessageId,
	AuditEventNotificationDefinitions.MessageVersion,
	MessageTarget.Topic)]
public sealed record AuditEventNotification(
	AuditLogEntry AuditLogEntry
) : DistributedMessage {
	/// <summary>
	/// Set to true to enable background delivery for audit event notifications.
	/// </summary>
	public override bool? UseBackgroundDelivery { get; set; } = true;
}