namespace Cirreum.Conductor.Internal;

using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

internal static class RequestContextFactory {

	public static Task<OperationContext<TOperation>> CreateRequestContext<TOperation>(
		this IServiceProvider serviceProvider,
		Activity? activity,
		long startTimestamp,
		TOperation typedRequest,
		string requestTypeName
	) where TOperation : notnull {

		// GetService<T>()! — IUserStateAccessor is registered by Cirreum bootstrap; skip
		// GetRequiredService's null-guard + throw-helper overhead on the hot path.
		var userStateVt = serviceProvider
			.GetService<IUserStateAccessor>()!
			.GetUser();

		// Hot path: ValueTask completed synchronously (cached user per-request) —
		// skip async state machine entirely (~120B + ~40-60ns saved).
		if (userStateVt.IsCompletedSuccessfully) {
			return Task.FromResult(BuildContext(
				userStateVt.Result, activity, startTimestamp, typedRequest, requestTypeName));
		}

		// Cold path: first call per request, actually async (user enrichment).
		return CreateRequestContextAsync(
			userStateVt, activity, startTimestamp, typedRequest, requestTypeName);
	}

	private static async Task<OperationContext<TOperation>> CreateRequestContextAsync<TOperation>(
		ValueTask<IUserState> userStateVt,
		Activity? activity,
		long startTimestamp,
		TOperation typedRequest,
		string requestTypeName
	) where TOperation : notnull {
		var userState = await userStateVt;
		return BuildContext(userState, activity, startTimestamp, typedRequest, requestTypeName);
	}

	private static OperationContext<TOperation> BuildContext<TOperation>(
		IUserState userState,
		Activity? activity,
		long startTimestamp,
		TOperation typedRequest,
		string requestTypeName
	) where TOperation : notnull {

		var requestId = activity?.SpanId.ToString()
			?? ActivitySpanId.CreateRandom().ToHexString();
		var correlationId = activity?.TraceId.ToString()
			?? ActivityTraceId.CreateRandom().ToHexString();

		return OperationContext<TOperation>.Create(
			userState,
			typedRequest,
			requestTypeName,
			requestId,
			correlationId,
			startTimestamp);
	}

}
