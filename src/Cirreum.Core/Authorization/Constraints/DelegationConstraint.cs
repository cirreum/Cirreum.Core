namespace Cirreum.Authorization.Constraints;

using Cirreum.Authorization.Operations;
using FluentValidation.Results;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Stage 1 Step 1 constraint that enforces every <see cref="DelegationCheckAttribute"/>
/// applied to the operation type. The framework's auto-scan registers this constraint
/// once; it discovers all decorated attributes per operation type via a cached
/// reflection lookup and invokes each attribute's <see cref="DelegationCheckAttribute.Check"/>
/// in a single per-request pass.
/// </summary>
/// <remarks>
/// <para>
/// <b>Single constraint, all delegation attributes.</b> The framework ships eight
/// concrete <see cref="DelegationCheckAttribute"/> subclasses
/// (<see cref="RequiresDirectCallerAttribute"/>, <see cref="RequiresDelegationAttribute"/>,
/// <see cref="RequiresDelegationActorAttribute"/>, <see cref="RequiresDelegationWithinAttribute"/>,
/// <see cref="RequiresDelegationEvidenceAttribute"/>, <see cref="RequiresDelegationScopeAttribute"/>,
/// <see cref="RequiresAnyDelegationScopeAttribute"/>,
/// <see cref="RequiresAllDelegationScopesAttribute"/>). The six facet attributes derive
/// from the <see cref="RequiresDelegationCheckAttribute"/> intermediate base, which
/// self-enforces the "delegation is mandatory" precondition so a facet cannot silently
/// fail-open when applied without an accompanying <see cref="RequiresDelegationAttribute"/>.
/// Apps add their own checks by subclassing <see cref="DelegationCheckAttribute"/> (or
/// <see cref="RequiresDelegationCheckAttribute"/> for a facet-style check) — no new
/// constraint class, no DI registration required. The framework's single registered
/// constraint picks them all up via
/// <c>Type.GetCustomAttributes&lt;DelegationCheckAttribute&gt;(inherit: true)</c>.
/// </para>
/// <para>
/// <b>Performance profile.</b> Operations without any <see cref="DelegationCheckAttribute"/>
/// pay one cached <see cref="Type"/> dictionary read and return the static
/// <see cref="Task{TResult}"/> — zero allocations. Operations with one or more
/// delegation attributes invoke each attribute's <see cref="DelegationCheckAttribute.Check"/>
/// inline and aggregate failures.
/// </para>
/// <para>
/// <b>Aggregation semantics.</b> All applicable checks run; failures aggregate into a
/// single <see cref="ValidationResult"/> so developers see every delegation issue at once
/// rather than fix-rerun-fix loops. When a direct caller hits an operation decorated with
/// both <see cref="RequiresDelegationAttribute"/> and one or more facet attributes, the
/// resulting <c>DELEGATION_REQUIRED</c> failures from each source are deduplicated by
/// error code so the caller sees a single denial, not one per facet.
/// </para>
/// </remarks>
public sealed class DelegationConstraint : IAuthorizationConstraint {

	/// <summary>
	/// Per-type cache of all <see cref="DelegationCheckAttribute"/> instances applied to
	/// the operation. Reflection cost is paid once per operation type (first encounter)
	/// and amortized over every subsequent invocation.
	/// </summary>
	private static readonly ConcurrentDictionary<Type, DelegationCheckAttribute[]> _cache = new();

	/// <summary>
	/// Sentinel for operations without any delegation attributes — reuses the same array
	/// instance to reduce cache memory.
	/// </summary>
	private static readonly DelegationCheckAttribute[] _none = [];

	/// <summary>
	/// Static success task — zero-allocation hot path for operations with no delegation
	/// attributes (the overwhelming majority of operations in most apps).
	/// </summary>
	private static readonly Task<ValidationResult> SuccessTask = Task.FromResult(new ValidationResult());

	/// <inheritdoc/>
	public Task<ValidationResult> EvaluateAsync<TAuthorizableObject>(
		AuthorizationContext<TAuthorizableObject> context,
		CancellationToken cancellationToken = default)
		where TAuthorizableObject : notnull, IAuthorizableObject {

		ArgumentNullException.ThrowIfNull(context);

		var checks = _cache.GetOrAdd(
			context.AuthorizableObject.GetType(),
			static t => {
				var attrs = t.GetCustomAttributes<DelegationCheckAttribute>(inherit: true).ToArray();
				return attrs.Length == 0 ? _none : attrs;
			});

		if (checks.Length == 0) {
			return SuccessTask;
		}

		var userState = context.UserState;
		List<ValidationFailure>? failures = null;
		var sawDelegationRequired = false;
		foreach (var check in checks) {
			var failure = check.Check(userState);
			if (failure is null) {
				continue;
			}

			// Dedup DELEGATION_REQUIRED: a direct caller hitting an operation decorated with
			// both [RequiresDelegation] and one or more facet attributes would otherwise
			// produce N identical failures. Surface one.
			if (failure.ErrorCode == "DELEGATION_REQUIRED") {
				if (sawDelegationRequired) {
					continue;
				}
				sawDelegationRequired = true;
			}

			(failures ??= []).Add(failure);
		}

		return failures is null
			? SuccessTask
			: Task.FromResult(new ValidationResult(failures));
	}

}
