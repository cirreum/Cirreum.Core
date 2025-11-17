namespace Cirreum.Conductor;

using Cirreum.Messaging;

public static class RequestCompletedNotificationDefinition {
	public const string MessageId = "cirreum.conductor.request-completed";
	public const string MessageVersion = "1";
}

[MessageDefinition(
	RequestCompletedNotificationDefinition.MessageId,
	RequestCompletedNotificationDefinition.MessageVersion,
	MessageTarget.Topic)]
public sealed record RequestCompletedNotification(

	// ENVIRONMENT / RUNTIME
	string Environment,
	DomainRuntimeType RuntimeType,

	// WHEN
	DateTimeOffset Timestamp,
	double DurationMs,

	// WHO (User Context)
	string UserId,
	string UserName,
	string? TenantId,
	IdentityProviderType Provider,
	bool IsAuthenticated,

	// REQUEST (WHAT)
	string RequestType,
	string RequestId,
	string CorrelationId,

	// RESULT (Outcome)
	string Outcome,
	string? FailureReason,
	string? ErrorType

) : DistributedMessage {
	/// <summary>
	/// Set to true to enable background delivery for request completion notifications.
	/// </summary>
	public override bool? UseBackgroundDelivery { get; set; } = true;

	/// <summary>
	/// Create a RequestCompletedNotification from a <see cref="Result"/> and RequestContext.
	/// </summary>
	/// <typeparam name="TRequest"></typeparam>
	/// <param name="result"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	public static RequestCompletedNotification FromResult<TRequest>(
		Result result,
		RequestContext<TRequest> context)
		where TRequest : IRequest {

		var (outcome, errorMessage, errorType) = result.Match(
			() => ("SUCCESS", (string?)null, (string?)null),
			ex => ("FAILURE", ex.Message, ex.GetType().Name));

		return new RequestCompletedNotification(

			// ENVIRONMENT / RUNTIME
			Environment: context.Environment,
			RuntimeType: context.RuntimeType,

			// WHEN
			Timestamp: context.Timestamp,
			DurationMs: context.ElapsedDuration.TotalMilliseconds,

			// WHO
			UserId: context.UserId,
			UserName: context.UserName,
			TenantId: context.TenantId,
			Provider: context.Provider,
			IsAuthenticated: context.IsAuthenticated,

			// REQUEST
			RequestType: context.RequestType,
			RequestId: context.RequestId,
			CorrelationId: context.CorrelationId,

			// RESULT
			Outcome: outcome,
			FailureReason: errorMessage,
			ErrorType: errorType
		);

	}

	/// <summary>
	/// Create a RequestCompletedNotification from a <see cref="Result{TRequest}"/> and RequestContext.
	/// </summary>
	/// <typeparam name="TRequest"></typeparam>
	/// <typeparam name="TResponse"></typeparam>
	/// <param name="result"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	public static RequestCompletedNotification FromResult<TRequest, TResponse>(
		Result<TResponse> result,
		RequestContext<TRequest> context)
		where TRequest : IRequest<TResponse> {

		var (outcome, errorMessage, errorType) = result.Match(
			_ => ("SUCCESS", null!, null!),
			ex => ("FAILURE", ex.Message, ex.GetType().Name));

		return new RequestCompletedNotification(

			// ENVIRONMENT / RUNTIME
			Environment: context.Environment,
			RuntimeType: context.RuntimeType,

			// WHEN
			Timestamp: context.Timestamp,
			DurationMs: context.ElapsedDuration.TotalMilliseconds,

			// WHO
			UserId: context.UserId,
			UserName: context.UserName,
			TenantId: context.TenantId,
			Provider: context.Provider,
			IsAuthenticated: context.IsAuthenticated,

			// REQUEST
			RequestType: context.RequestType,
			RequestId: context.RequestId,
			CorrelationId: context.CorrelationId,

			// RESULT
			Outcome: outcome,
			FailureReason: errorMessage,
			ErrorType: errorType
		);

	}

}