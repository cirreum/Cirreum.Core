namespace Cirreum.Authorization.Grants;

using Cirreum.Security;
using FluentValidation.Results;
using System.Diagnostics;

/// <summary>
/// Stage 1 / Step 0 — the grant-aware scope gate. Evaluates the caller's
/// <see cref="AccessReach"/> via the registered <see cref="IAccessReachResolver"/> and
/// enforces grant timing:
/// <list type="bullet">
///   <item><description><b>Mutate:</b> <c>OwnerId ∈ reach</c> before handler.</description></item>
///   <item><description><b>Lookup:</b> stash reach for post-fetch check (Pattern C), or enforce <c>OwnerId ∈ reach</c> when supplied.</description></item>
///   <item><description><b>Search:</b> <c>OwnerIds ⊆ reach</c>, stamp when null.</description></item>
///   <item><description><b>Self:</b> <c>ExternalId == context.UserId</c> identity match; admin bypass via <see cref="IGrantResolver.ShouldBypassAsync"/>.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// Resources that do not implement <see cref="IGrantableMutateBase"/>, <see cref="IGrantableLookupBase"/>,
/// <see cref="IGrantableSearchBase"/>, or <see cref="IGrantableSelfBase"/> short-circuit with a pass —
/// the gate is purely a Grants concern.
/// </para>
/// <para>
/// All customization is in <see cref="IGrantResolver"/>: bypass logic, grant lookup,
/// and home-owner policy. This evaluator is sealed with no virtual extension points.
/// </para>
/// </remarks>
public sealed class GrantEvaluator(
	AccessReachResolverSelector resolverSelector,
	IAccessReachAccessor reachAccessor) {

	private readonly AccessReachResolverSelector _resolverSelector =
		resolverSelector ?? throw new ArgumentNullException(nameof(resolverSelector));
	private readonly IAccessReachAccessor _reachAccessor =
		reachAccessor ?? throw new ArgumentNullException(nameof(reachAccessor));

	/// <summary>
	/// Evaluates the grant-aware scope gate against the resource in the given context.
	/// </summary>
	public async Task<ValidationResult> EvaluateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken = default)
		where TResource : notnull, IAuthorizableResource {

		// Not applicable — short-circuit pass.
		var mutate = context.Resource as IGrantableMutateBase;
		var lookup = context.Resource as IGrantableLookupBase;
		var search = context.Resource as IGrantableSearchBase;
		var self = context.Resource as IGrantableSelfBase;
		if (mutate is null && lookup is null && search is null && self is null) {
			return Pass();
		}

		// IsEnabled progressive check
		if (!CheckApplicationUserEnabled(context)) {
			EmitTelemetry(context, DenyCodes.UserDisabled);
			return Deny(DenyCodes.UserDisabled, "User is disabled.");
		}

		// Self-scoped: identity match — fast path without reach resolution.
		if (self is not null) {
			return await this.EvaluateSelfAsync(context, self, cancellationToken)
				.ConfigureAwait(false);
		}

		// Owner-scoped: reach resolution required.
		var resolver = this._resolverSelector.SelectFor(typeof(TResource));
		if (resolver is null) {
			// Grant interface present but no resolver registered — misconfiguration.
			EmitTelemetry(context, DenyCodes.ScopeNotPermitted);
			return Deny(DenyCodes.ScopeNotPermitted,
				$"No IAccessReachResolver registered for resource type '{typeof(TResource).Name}'. " +
				$"Register a grant resolver via services.AddAccessGrants<TResolver>().");
		}

		var reach = await resolver
			.ResolveAsync(context, cancellationToken)
			.ConfigureAwait(false);

		this._reachAccessor.Set(reach);

		if (reach.IsDenied) {
			EmitTelemetry(context, DenyCodes.ReachDenied);
			return Deny(DenyCodes.ReachDenied, "Caller has no reach for this operation.");
		}

		var result = search is not null
			? EvaluateSearch(search, reach)
			: mutate is not null
			? EvaluateMutate(context, mutate, reach)
			: EvaluateLookup(context, lookup!, reach);

		EmitTelemetry(context, result.IsValid ? AuthorizationTelemetry.ReasonPass : DenyReason(result));
		return result;
	}

	// Self-scoped enforcement —————————————————————————————————

	private async Task<ValidationResult> EvaluateSelfAsync<TResource>(
		AuthorizationContext<TResource> context,
		IGrantableSelfBase self,
		CancellationToken cancellationToken)
		where TResource : notnull, IAuthorizableResource {

		// Mutate enrichment: auto-stamp Id from caller identity when null.
		if (self is IGrantableMutateSelfBase && self.Id is null) {
			self.Id = context.UserId;
			EmitTelemetry(context, AuthorizationTelemetry.ReasonPass, AuthorizationTelemetry.StepSelfIdentity);
			return Pass();
		}

		if (!self.IsValidId) {
			EmitTelemetry(context, DenyCodes.ResourceIdRequired, AuthorizationTelemetry.StepSelfIdentity);
			return Deny(DenyCodes.ResourceIdRequired, "Id is required and must be valid on self-scoped resources.");
		}

		// Fast path: identity match — no resolver needed.
		if (string.Equals(self.ExternalId, context.UserId, StringComparison.OrdinalIgnoreCase)) {
			EmitTelemetry(context, AuthorizationTelemetry.ReasonPass, AuthorizationTelemetry.StepSelfIdentity);
			return Pass();
		}

		// Non-match: check for admin/privilege bypass via reach resolution.
		var resolver = this._resolverSelector.SelectFor(typeof(TResource));
		if (resolver is not null) {
			var reach = await resolver
				.ResolveAsync(context, cancellationToken)
				.ConfigureAwait(false);

			if (reach.IsUnrestricted) {
				EmitTelemetry(context, AuthorizationTelemetry.ReasonPass, AuthorizationTelemetry.StepSelfIdentity);
				return Pass();
			}
		}

		EmitTelemetry(context, DenyCodes.NotResourceOwner, AuthorizationTelemetry.StepSelfIdentity);
		return Deny(DenyCodes.NotResourceOwner, "Caller is not the resource owner and does not have bypass privileges.");
	}

	// Owner-scoped enforcement ————————————————————————————————

	private static ValidationResult EvaluateMutate<TResource>(
		AuthorizationContext<TResource> context,
		IGrantableMutateBase mutate,
		AccessReach reach) where TResource : notnull, IAuthorizableResource {

		if (!string.IsNullOrWhiteSpace(mutate.OwnerId)) {
			return reach.Contains(mutate.OwnerId!)
				? Pass()
				: Deny(DenyCodes.OwnerNotInReach, "Requested owner is not in the caller's reach.");
		}

		// OwnerId is null — enrich from reach.
		if (context.AccessScope == AccessScope.Global) {
			return Deny(DenyCodes.OwnerIdRequired, "OwnerId is required for cross-tenant writes.");
		}
		if (reach.IsUnrestricted) {
			return Deny(DenyCodes.OwnerIdRequired, "OwnerId is required — caller's reach is unrestricted.");
		}
		if (reach.OwnerIds is { Count: 1 }) {
			mutate.OwnerId = reach.OwnerIds[0];
			return Pass();
		}
		return Deny(DenyCodes.OwnerAmbiguous, "OwnerId is required — caller's reach contains multiple owners.");
	}

	private static ValidationResult EvaluateLookup<TResource>(
		AuthorizationContext<TResource> context,
		IGrantableLookupBase lookup,
		AccessReach reach)
		where TResource : notnull, IAuthorizableResource {

		if (!string.IsNullOrWhiteSpace(lookup.OwnerId)) {
			if (!reach.Contains(lookup.OwnerId!)) {
				return Deny(DenyCodes.OwnerNotInReach, "Requested owner is not in the caller's reach.");
			}
			// OwnerId supplied and in reach — stamp CallerAccessScope for cacheable lookups.
			if (context.Resource is IGrantableCacheableLookupBase cacheableWithOwner) {
				cacheableWithOwner.CallerAccessScope = context.AccessScope;
			}
			return Pass();
		}

		// Cacheable lookup: Global callers MUST supply OwnerId to prevent unbounded cache bucket.
		if (context.Resource is IGrantableCacheableLookupBase) {
			if (context.AccessScope == AccessScope.Global) {
				return Deny(DenyCodes.CacheableReadOwnerIdRequired,
					"OwnerId is required for cross-tenant cacheable lookups.");
			}
		}

		// OwnerId null — defer to handler (Pattern C). Reach already stashed on accessor.
		// Stamp CallerAccessScope for cacheable lookups.
		if (context.Resource is IGrantableCacheableLookupBase cacheable) {
			cacheable.CallerAccessScope = context.AccessScope;
		}
		return Pass();
	}

	private static ValidationResult EvaluateSearch(
		IGrantableSearchBase search,
		AccessReach reach) {
		if (search.OwnerIds is null) {
			// Stamp reach. Unrestricted = null (no bound).
			search.OwnerIds = reach.IsUnrestricted ? null : reach.OwnerIds;
			return Pass();
		}
		if (!reach.ContainsAll(search.OwnerIds)) {
			return Deny(DenyCodes.OwnerNotInReach, "One or more requested owners are not in the caller's reach.");
		}
		return Pass();
	}

	// Application user guard ————————————————————————————————

	private static bool CheckApplicationUserEnabled<TResource>(AuthorizationContext<TResource> ctx)
		where TResource : notnull, IAuthorizableResource {

		if (!ctx.UserState.IsApplicationUserLoaded) {
			return true;
		}
		return ctx.UserState.ApplicationUser is IOwnedApplicationUser { IsEnabled: true };
	}

	// Helpers ————————————————————————————————————————————————

	private static ValidationResult Pass() => new();

	private static ValidationResult Deny(string code, string message)
		=> new([new ValidationFailure(propertyName: code, errorMessage: message) {
			ErrorCode = code
		}]);

	private static string DenyReason(ValidationResult r)
		=> r.Errors.FirstOrDefault()?.ErrorCode ?? "UNKNOWN";

	private static void EmitTelemetry<TResource>(
		AuthorizationContext<TResource> ctx,
		string outcome,
		string step = AuthorizationTelemetry.StepOwnerScope)
		where TResource : notnull, IAuthorizableResource {

		var isPass = outcome == AuthorizationTelemetry.ReasonPass;
		var decision = isPass
			? AuthorizationTelemetry.DecisionPass
			: AuthorizationTelemetry.DecisionDeny;
		var scope = ctx.AccessScope.ToString().ToLowerInvariant();
		var resourceType = ctx.Resource.GetType().Name;

		var activity = Activity.Current;
		if (activity is not null) {
			activity.SetTag(AuthorizationTelemetry.StageTag, AuthorizationTelemetry.StageScope);
			activity.SetTag(AuthorizationTelemetry.StepTag, step);
			activity.SetTag(AuthorizationTelemetry.ScopeTag, scope);
			activity.SetTag(AuthorizationTelemetry.DecisionTag, decision);
			activity.SetTag(AuthorizationTelemetry.ReasonTag, outcome);
			activity.SetTag(AuthorizationTelemetry.EvaluatorTag, nameof(GrantEvaluator));
			activity.SetTag(AuthorizationTelemetry.ResourceTypeTag, resourceType);
		}

		AuthorizationTelemetry.RecordDecision(
			stage: AuthorizationTelemetry.StageScope,
			step: step,
			decision: decision,
			reason: outcome,
			scope: scope,
			evaluator: nameof(GrantEvaluator),
			resourceType: resourceType);
	}
}
