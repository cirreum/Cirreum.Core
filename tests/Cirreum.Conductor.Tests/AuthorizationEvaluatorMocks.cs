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
/// via delegates for both ad-hoc and context-aware evaluation behaviors.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="TestAuthorizationEvaluator"/>.
/// </remarks>
/// <param name="evaluateAdHoc">
/// Delegate used by the ad-hoc <see cref="Evaluate{TResource}(TResource, CancellationToken)"/>. 
/// If null, authorization always succeeds.
/// </param>
/// <param name="evaluateWithContext">
/// Delegate used by the context-aware <see cref="Evaluate{TResource}(TResource, OperationContext, CancellationToken)"/>. 
/// If null, authorization always succeeds.
/// </param>
public sealed class TestAuthorizationEvaluator(
	Func<object, CancellationToken, Result>? evaluateAdHoc = null,
	Func<object, OperationContext, CancellationToken, Result>? evaluateWithContext = null)
	: IAuthorizationEvaluator {

	private readonly Func<object, CancellationToken, Result> _evaluateAdHoc =
		evaluateAdHoc ?? ((_, _) => Result.Success);

	private readonly Func<object, OperationContext, CancellationToken, Result> _evaluateWithContext =
		evaluateWithContext ?? ((_, _, _) => Result.Success);

	/// <summary>
	/// Ad-hoc evaluation (builds context internally in real implementation).
	/// </summary>
	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		if (resource is null) {
			throw new ArgumentNullException(nameof(resource));
		}

		var result = _evaluateAdHoc(resource, cancellationToken);
		return new ValueTask<Result>(result);
	}

	/// <summary>
	/// Context-aware evaluation (uses provided OperationContext).
	/// </summary>
	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		OperationContext operation,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		if (resource is null) {
			throw new ArgumentNullException(nameof(resource));
		}

		ArgumentNullException.ThrowIfNull(operation);

		var result = _evaluateWithContext(resource, operation, cancellationToken);
		return new ValueTask<Result>(result);
	}
}

/// <summary>
/// Authorization evaluator that always succeeds.
/// </summary>
public sealed class AlwaysAllowAuthorizationEvaluator : IAuthorizationEvaluator {

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return ValueTask.FromResult(Result.Success);
	}

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		OperationContext operation,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return ValueTask.FromResult(Result.Success);
	}
}

/// <summary>
/// Authorization evaluator that always fails with <see cref="ForbiddenAccessException"/>.
/// </summary>
public sealed class AlwaysDenyAuthorizationEvaluator(Exception? exception = null) : IAuthorizationEvaluator {

	private readonly Exception _exception =
		exception ??
		new ForbiddenAccessException("Access denied by test evaluator.");

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return ValueTask.FromResult(Result.Fail(_exception));
	}

	public ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		OperationContext operation,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		return ValueTask.FromResult(Result.Fail(_exception));
	}
}