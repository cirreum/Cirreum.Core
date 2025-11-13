namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Exceptions;

public sealed record OrderResource(
	string OrderId,
	string OwnerUserId
) : IAuthorizableResource;

public sealed record AdminOnlyResource(
	string ResourceName
) : IAuthorizableResource;

/// <summary>
/// Test double for <see cref="IAuthorizationEvaluator"/> that can be configured
/// via delegates for both Evaluate and Enforce behaviors.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="TestAuthorizationEvaluator"/>.
/// </remarks>
/// <param name="evaluate">
/// Delegate used by <see cref="Evaluate{TResource}"/>. If null, authorization always succeeds.
/// </param>
/// <param name="enforce">
/// Delegate used by <see cref="Enforce{TResource}"/>. If null, authorization always succeeds.
/// </param>
public sealed class TestAuthorizationEvaluator(
	Func<object, string, string, CancellationToken, Result>? evaluate = null,
	Func<object, string, string, CancellationToken, ValueTask>? enforce = null) : IAuthorizationEvaluator {

	private readonly Func<object, string, string, CancellationToken, Result> _evaluate = evaluate ?? ((_, _, _, _) => Result.Success);
	private readonly Func<object, string, string, CancellationToken, ValueTask> _enforce = enforce ?? ((_, _, _, _) => ValueTask.CompletedTask);

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		if (resource is null) {
			throw new ArgumentNullException(nameof(resource));
		}

		var result = _evaluate(resource, requestId, correlationId, cancellationToken);
		return new ValueTask<Result>(result);
	}

	public ValueTask Enforce<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		if (resource is null) {
			throw new ArgumentNullException(nameof(resource));
		}

		return _enforce(resource, requestId, correlationId, cancellationToken);
	}
}

/// <summary>
/// Authorization evaluator that always succeeds.
/// </summary>
public sealed class AlwaysAllowAuthorizationEvaluator : IAuthorizationEvaluator {

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return new ValueTask<Result>(Result.Success);
	}

	public ValueTask Enforce<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return ValueTask.CompletedTask;
	}
}

/// <summary>
/// Authorization evaluator that always fails with <see cref="ForbiddenAccessException"/>.
/// </summary>
public sealed class AlwaysDenyAuthorizationEvaluator(Exception? exception = null) : IAuthorizationEvaluator {

	private readonly Exception _exception = exception ?? new ForbiddenAccessException("Access denied by test evaluator.");

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return new ValueTask<Result>(Result.Fail(_exception));
	}

	public ValueTask Enforce<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return ValueTask.FromException(_exception);
	}
}
