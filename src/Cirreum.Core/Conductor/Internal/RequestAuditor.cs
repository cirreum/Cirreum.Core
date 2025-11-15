namespace Cirreum.Conductor.Internal;

using Cirreum.Auditing;
using Microsoft.Extensions.Logging;

internal static class RequestAuditor {

	public static async ValueTask AuditRequestIfRequired<TRequest, TResponse>(
		IPublisher publisher,
		Result<TResponse> result,
		RequestContext<TRequest> context,
		ILogger logger)
		where TRequest : notnull {

		if (context.Request is not IAuditableRequestBase) {
			return;
		}

		try {
			await AuditRequest(publisher, result, context);
		} catch (Exception ex) {
			logger.LogAuditLoggingFailed(ex);
		}

	}

	public static async ValueTask AuditRequest<TRequest, TResponse>(
		IPublisher publisher,
		Result<TResponse> result,
		RequestContext<TRequest> context)
		where TRequest : notnull {

		// Publish notification - fire-and-forget
		await result.SwitchAsync(
			onSuccess: async ct => {
				var auditEntry = AuditLogEntryFactory.FromRequestContext(
					context,
					"SUCCESS",
					null,
					null);
				await publisher.PublishAsync(
					new AuditEventNotification(auditEntry),
					PublisherStrategy.FireAndForget,
					CancellationToken.None);
			},
			onFailure: async error => {
				var auditEntry = AuditLogEntryFactory.FromRequestContext(
					context,
					"FAILURE",
					error.Message,
					error.GetType().Name);
				await publisher.PublishAsync(
					new AuditEventNotification(auditEntry),
					PublisherStrategy.FireAndForget,
					CancellationToken.None);
			});

	}

}