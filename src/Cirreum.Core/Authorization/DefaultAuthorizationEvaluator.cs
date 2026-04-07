namespace Cirreum.Authorization;

using Cirreum.Authorization.Diagnostics;
using Cirreum.Authorization.Grants;
using Cirreum.Exceptions;
using Cirreum.Security;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

/// <summary>
/// The default implementation of the <see cref="IAuthorizationEvaluator"/>.
/// Runs the three-stage authorization pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>Stage 1 — Scope</b>
/// <list type="bullet">
/// <item><description>
/// Step 0: grant evaluator (<see cref="GrantEvaluator"/>, optional,
/// applies only to <see cref="IGrantableMutateBase"/>/<see cref="IGrantableLookupBase"/>/<see cref="IGrantableSearchBase"/>).
/// </description></item>
/// <item><description>
/// Step 1: generic scope evaluators (<see cref="IScopeEvaluator"/>, zero or more,
/// run in registration order).
/// </description></item>
/// </list>
/// First failure in Stage 1 short-circuits the pipeline.
/// </description></item>
/// <item><description>
/// <b>Stage 2 — Resource</b>: resource authorizers (<see cref="IResourceAuthorizer{TResource}"/>).
/// All authorizers run; failures are aggregated.
/// </description></item>
/// <item><description>
/// <b>Stage 3 — Policy</b>: policy validators (<see cref="IPolicyValidator"/>) whose
/// <see cref="IPolicyValidator.AppliesTo{TResource}(TResource, DomainRuntimeType, DateTimeOffset)"/>
/// returns true. Run in <see cref="IPolicyValidator.Order"/>; failures are aggregated.
/// </description></item>
/// </list>
/// </remarks>
/// <param name="registry">The authorization role registry for resolving effective roles.</param>
/// <param name="userAccessor">The accessor for retrieving current user state.</param>
/// <param name="services">The service provider for resolving validators.</param>
/// <param name="logger">The logger for authorization events.</param>
/// <param name="grantEvaluator">
/// Optional grant evaluator. When present and the resource implements a Granted
/// interface (<see cref="IGrantableMutateBase"/>/<see cref="IGrantableLookupBase"/>/<see cref="IGrantableSearchBase"/>),
/// runs as Stage 1 Step 0.
/// </param>
sealed class DefaultAuthorizationEvaluator(
	IAuthorizationRoleRegistry registry,
	IUserStateAccessor userAccessor,
	IServiceProvider services,
	ILogger<DefaultAuthorizationEvaluator> logger,
	GrantEvaluator? grantEvaluator = null
) : IAuthorizationEvaluator {

	/// <inheritdoc/>
	/// <remarks>
	/// Ad-hoc evaluation entry point. Builds the OperationContext from scratch
	/// and delegates to the context-aware overload.
	/// </remarks>
	public async ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		// Build OperationContext for ad-hoc evaluation
		var userState = await userAccessor.GetUser().ConfigureAwait(false);
		var operation = OperationContext.Create(
			userState,
			operationId: ActivitySpanId.CreateRandom().ToHexString(),
			correlationId: ActivityTraceId.CreateRandom().ToHexString(),
			startTimestamp: Timing.Start());

		// Delegate to the context-aware overload
		return await this.Evaluate(resource, operation, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	/// <remarks>
	/// Context-aware evaluation entry point. Uses the provided OperationContext
	/// to avoid rebuilding user state and environment information.
	/// </remarks>
	public async ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		OperationContext operation,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		var resourceRuntimeType = resource.GetType();
		var resourceName = resourceRuntimeType.Name;
		var resourceCompileTimeType = typeof(TResource);

		using var activity = AuthorizationTelemetry.StartActivity(resourceName);
		var startTimestamp = Timing.Start();

		// Check authentication
		if (!operation.IsAuthenticated) {
			//******************************************
			//
			// NOT AUTHENTICATED
			//
			//******************************************
			var ex = new UnauthenticatedAccessException("User is not authenticated.");

			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				ex.Message);

			AuthorizationTelemetry.RecordDuration(
				activity, resourceName,
				Timing.GetElapsedMilliseconds(startTimestamp),
				AuthorizationTelemetry.DecisionDeny,
				reason: "unauthenticated");

			return Result.Fail(ex);
		}

		// Check cancellation early
		cancellationToken.ThrowIfCancellationRequested();

		// Check if the runtime type matches the compile-time type
		if (resourceRuntimeType != resourceCompileTimeType) {
			throw new ArgumentException(
				$"Resource must be a concrete type. Expected {resourceCompileTimeType.Name} but got {resourceRuntimeType.Name}. " +
				"Do not pass casted instances.",
				nameof(resource));
		}

		// MS.DI's GetServices<T>() materializes as T[] internally; cast to avoid a second
		// allocation from .ToList() / .ToArray(). Fallback to collection-expr copy if a custom
		// container ever returns a non-array shape. Call GetService<IEnumerable<T>>()! directly
		// to bypass GetRequiredService's null-guard + throw-helper — IEnumerable<T> is always
		// registered by MS.DI (empty array if no components).
		var rawScope = services.GetService<IEnumerable<IScopeEvaluator>>()!;
		var scopeEvaluators = rawScope as IScopeEvaluator[] ?? [.. rawScope];

		var rawResource = services.GetService<IEnumerable<IResourceAuthorizer<TResource>>>()!;
		var resourceAuthorizers = rawResource as IResourceAuthorizer<TResource>[] ?? [.. rawResource];

		// Policy runtime-type filter is deferred into the foreach below — combined with
		// AppliesTo so we walk the array once instead of materializing a filtered copy here
		// and then iterating again with Where().OrderBy().
		var rawPolicy = services.GetService<IEnumerable<IPolicyValidator>>()!;
		var policyAuthorizers = rawPolicy as IPolicyValidator[] ?? [.. rawPolicy];

		var grantGateApplies = grantEvaluator is not null
			&& (resource is IGrantableMutateBase
				|| resource is IGrantableLookupBase
				|| resource is IGrantableSearchBase
				|| resource is IGrantableSelfBase);

		if (scopeEvaluators.Length == 0
			&& resourceAuthorizers.Length == 0
			&& policyAuthorizers.Length == 0
			&& !grantGateApplies) {
			//******************************************
			//
			// RESOURCE HAS NO AUTHORIZERS AND
			// RUNTIME HAS NO POLICY AUTHORIZERS
			// AND NO SCOPE CHECKS APPLY
			//
			//******************************************
			var emptyAuthContainerEx = new InvalidOperationException(
				$"Resource '{resourceName}' has no authorizers or applicable policies.");
			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				emptyAuthContainerEx.Message);

			AuthorizationTelemetry.RecordDuration(
				activity, resourceName,
				Timing.GetElapsedMilliseconds(startTimestamp),
				AuthorizationTelemetry.DecisionDeny,
				reason: "no-authorizers");

			return Result.Fail(emptyAuthContainerEx);
		}

		// Check cancellation before entering validation logic
		cancellationToken.ThrowIfCancellationRequested();

		// Get the user's roles
		var roles = operation.UserState.Profile.Roles
			.Select(registry.GetRoleFromString)
			.OfType<Role>()
			.ToImmutableList();

		if (roles.Count == 0) {
			//******************************************
			//
			// USER HAS NO REGISTERED ROLES
			//
			//******************************************
			var noRolesEx = new ForbiddenAccessException(
				$"User '{operation.UserName}' has no assigned roles.");

			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				noRolesEx.Message);

			AuthorizationTelemetry.RecordDuration(
				activity, resourceName,
				Timing.GetElapsedMilliseconds(startTimestamp),
				AuthorizationTelemetry.DecisionDeny,
				reason: "no-roles");

			return Result.Fail(noRolesEx);
		}

		// Check cancellation before entering validation logic
		cancellationToken.ThrowIfCancellationRequested();

		const string scopeTemplate = "Authorizing user '{UserName}' for Resource '{ResourceName}'";
		using var logScope = logger.BeginScope(scopeTemplate, operation.UserName, resourceName);

		try {

			// Build effective roles ONCE
			var effectiveRoles = registry.GetEffectiveRoles(roles);

			// Build the canonical AuthorizationContext that validators will use
			var authorizationContext = new AuthorizationContext<TResource>(
				operation,
				effectiveRoles,
				resource);

			//******************************************
			//
			// STAGE 1 — SCOPE
			//
			// Step 0: owner-scope gate
			// Step 1: generic scope evaluators
			//
			// First failure short-circuits.
			//
			//******************************************

			if (grantGateApplies) {
				var grantResult = await grantEvaluator!
					.EvaluateAsync(authorizationContext, cancellationToken)
					.ConfigureAwait(false);

				if (!grantResult.IsValid) {
					// GrantEvaluator already called RecordDecision() via EmitTelemetry()
					AuthorizationTelemetry.RecordDuration(
						activity, resourceName,
						Timing.GetElapsedMilliseconds(startTimestamp),
						AuthorizationTelemetry.DecisionDeny,
						denyStage: AuthorizationTelemetry.StageScope);
					return this.DenyFromStage(grantResult.Errors, operation.UserName, resourceName);
				}
			}

			foreach (var evaluator in scopeEvaluators) {
				cancellationToken.ThrowIfCancellationRequested();

				var scopeResult = await evaluator
					.EvaluateAsync(authorizationContext, cancellationToken)
					.ConfigureAwait(false);

				if (!scopeResult.IsValid) {
					AuthorizationTelemetry.RecordDecision(
						stage: AuthorizationTelemetry.StageScope,
						step: AuthorizationTelemetry.StepScopeEvaluator,
						decision: AuthorizationTelemetry.DecisionDeny,
						reason: scopeResult.Errors.FirstOrDefault()?.ErrorCode ?? "UNKNOWN",
						evaluator: evaluator.GetType().Name,
						resourceType: resourceName);
					AuthorizationTelemetry.RecordDuration(
						activity, resourceName,
						Timing.GetElapsedMilliseconds(startTimestamp),
						AuthorizationTelemetry.DecisionDeny,
						denyStage: AuthorizationTelemetry.StageScope);
					return this.DenyFromStage(scopeResult.Errors, operation.UserName, resourceName);
				}
			}

			//******************************************
			//
			// STAGE 2 — RESOURCE VALIDATORS
			//
			// Aggregate failures within the stage. If any resource authorizer
			// denies, short-circuit before Stage 3 — policy checks are
			// irrelevant (and often expensive) once resource-level access is
			// denied.
			//
			//******************************************

			// Create FluentValidation context
			var validationContext = new ValidationContext<AuthorizationContext<TResource>>(authorizationContext);

			List<ValidationFailure>? stageFailures = null;

			// Run the Resource Authorizer. By contract each TResource has exactly one
			// ResourceAuthorizerBase<TResource> registered (mirrors AbstractValidator<T>
			// per T in FluentValidation). Extra registrations are a misconfiguration and
			// fail loud at evaluation time.
			if (resourceAuthorizers.Length > 1) {
				throw new InvalidOperationException(
					$"Multiple IResourceAuthorizer<{typeof(TResource).Name}> registrations detected "
					+ $"({resourceAuthorizers.Length}). Exactly one ResourceAuthorizerBase<T> per "
					+ "resource type is the expected contract.");
			}
			if (resourceAuthorizers.Length == 1
				&& resourceAuthorizers[0] is ResourceAuthorizerBase<TResource> authorizer) {
				var authResult = await authorizer
					.ValidateAsync(validationContext, cancellationToken)
					.ConfigureAwait(false);
				foreach (var failure in authResult.Errors) {
					if (failure is not null) {
						(stageFailures ??= []).Add(failure);
					}
				}
			}

			if (stageFailures is not null) {
				AuthorizationTelemetry.RecordDecision(
					stage: AuthorizationTelemetry.StageResource,
					step: AuthorizationTelemetry.StepResourceAuthorizer,
					decision: AuthorizationTelemetry.DecisionDeny,
					reason: stageFailures[0].ErrorCode ?? "UNKNOWN",
					evaluator: resourceAuthorizers[0].GetType().Name,
					resourceType: resourceName);
				AuthorizationTelemetry.RecordDuration(
					activity, resourceName,
					Timing.GetElapsedMilliseconds(startTimestamp),
					AuthorizationTelemetry.DecisionDeny,
					denyStage: AuthorizationTelemetry.StageResource);
				return this.DenyFromStage(stageFailures, operation.UserName, resourceName);
			}

			//******************************************
			//
			// STAGE 3 — POLICY VALIDATORS
			//
			// Aggregate failures within the stage.
			//
			//******************************************

			// Run applicable Policy Authorizers. Combine runtime-support + AppliesTo filters
			// in one pass; sort only the applicable subset.
			var applicablePolicies = FilterAndOrderPolicies(
				policyAuthorizers, resource, operation.RuntimeType, operation.Timestamp);

			foreach (var policyValidator in applicablePolicies) {
				cancellationToken.ThrowIfCancellationRequested();

				var policyResult = await policyValidator
					.ValidateAsync(authorizationContext, cancellationToken)
					.ConfigureAwait(false);

				if (!policyResult.IsValid) {
					(stageFailures ??= []).AddRange(policyResult.Errors);
				}
			}

			if (stageFailures is not null) {
				AuthorizationTelemetry.RecordDecision(
					stage: AuthorizationTelemetry.StagePolicy,
					step: AuthorizationTelemetry.StepPolicyValidator,
					decision: AuthorizationTelemetry.DecisionDeny,
					reason: stageFailures[0].ErrorCode ?? "UNKNOWN",
					resourceType: resourceName);
				AuthorizationTelemetry.RecordDuration(
					activity, resourceName,
					Timing.GetElapsedMilliseconds(startTimestamp),
					AuthorizationTelemetry.DecisionDeny,
					denyStage: AuthorizationTelemetry.StagePolicy);
				return this.DenyFromStage(stageFailures, operation.UserName, resourceName);
			}

			//******************************************
			//
			// AUTHORIZED
			//
			//******************************************
			logger.LogAuthorizingResourceAllowed(
				operation.UserName,
				resourceName);

			AuthorizationTelemetry.RecordDuration(
				activity, resourceName,
				Timing.GetElapsedMilliseconds(startTimestamp),
				AuthorizationTelemetry.DecisionPass,
				reason: AuthorizationTelemetry.ReasonPass);

			return Result.Success;

		} catch (OperationCanceledException) {
			// Expected - let it propagate
			throw;

		} catch (Exception ex) {
			// Unexpected runtime errors during validation
			// (e.g., database failures, network issues in validators)
			logger.LogAuthorizingResourceUnknownError(
				ex,
				operation.UserName,
				resourceName,
				ex.Message);

			AuthorizationTelemetry.RecordDuration(
				activity, resourceName,
				Timing.GetElapsedMilliseconds(startTimestamp),
				AuthorizationTelemetry.DecisionDeny,
				reason: "error");

			return Result.Fail(ex);
		}
	}

	private static List<IPolicyValidator> FilterAndOrderPolicies<TResource>(
		IPolicyValidator[] all,
		TResource resource,
		DomainRuntimeType runtimeType,
		DateTimeOffset timestamp)
		where TResource : IAuthorizableResource {

		// Walk once, keep applicable, sort by Order. Typical sizes are small (2-8) so
		// List.Sort with the delegate comparer is cheaper than LINQ's OrderBy + stable sort.
		var applicable = new List<IPolicyValidator>(all.Length);
		foreach (var pv in all) {
			if (pv.SupportedRuntimeTypes.Contains(runtimeType)
				&& pv.AppliesTo(resource, runtimeType, timestamp)) {
				applicable.Add(pv);
			}
		}
		if (applicable.Count > 1) {
			applicable.Sort(static (a, b) => a.Order.CompareTo(b.Order));
		}
		return applicable;
	}

	private Result DenyFromStage(List<ValidationFailure> failures, string userName, string resourceName) {
		var message = string.Join(',', failures.Select(f => f.ErrorMessage));
		logger.LogAuthorizingResourceDenied(userName, resourceName, message);
		return Result.Fail(new ForbiddenAccessException(message));
	}
}
