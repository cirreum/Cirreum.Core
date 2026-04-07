namespace Cirreum.Authorization.Grants;

using Cirreum.Security;
using FluentValidation.Results;
using System.Diagnostics;

/// <summary>
/// Stage 1 / Step 0 — the grant-aware owner-scope gate. Evaluates the caller's
/// <see cref="AccessReach"/> via the registered <see cref="IAccessReachResolver"/> and
/// enforces CRL timing:
/// <list type="bullet">
///   <item><description><b>Command:</b> <c>OwnerId ∈ reach</c> before handler.</description></item>
///   <item><description><b>Read:</b> stash reach for post-fetch check (Pattern C), or enforce <c>OwnerId ∈ reach</c> when supplied.</description></item>
///   <item><description><b>List:</b> <c>OwnerIds ⊆ reach</c>, stamp when null.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// Resources that do not implement <see cref="IGrantedCommandBase"/>, <see cref="IGrantedReadBase"/>,
/// or <see cref="IGrantedListBase"/> short-circuit with a pass — the gate is purely a Grants concern.
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
	/// Evaluates the grant-aware owner-scope gate against the resource in the given context.
	/// </summary>
	public async Task<ValidationResult> EvaluateAsync<TResource>(
		AuthorizationContext<TResource> context,
		CancellationToken cancellationToken = default)
		where TResource : notnull, IAuthorizableResource {

		// Not applicable — short-circuit pass.
		var grantedCommand = context.Resource as IGrantedCommandBase;
		var grantedRead = context.Resource as IGrantedReadBase;
		var grantedList = context.Resource as IGrantedListBase;
		if (grantedCommand is null && grantedRead is null && grantedList is null) {
			return Pass();
		}

		// IsEnabled progressive check
		if (!CheckApplicationUserEnabled(context)) {
			EmitTelemetry(context, DenyCodes.UserDisabled);
			return Deny(DenyCodes.UserDisabled, "User is disabled.");
		}

		// Find the resolver for this resource type.
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

		var result = grantedList is not null
			? EvaluateList(grantedList, reach)
			: grantedCommand is not null
			? EvaluateCommand(context, grantedCommand, reach)
			: EvaluateRead(context, grantedRead!, reach);

		EmitTelemetry(context, result.IsValid ? AuthorizationTelemetry.ReasonPass : DenyReason(result));
		return result;
	}

	// CRL enforcement ————————————————————————————————————————

	private static ValidationResult EvaluateCommand<TResource>(
		AuthorizationContext<TResource> context,
		IGrantedCommandBase command,
		AccessReach reach) where TResource : notnull, IAuthorizableResource {

		if (!string.IsNullOrWhiteSpace(command.OwnerId)) {
			return reach.Contains(command.OwnerId!)
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
			command.OwnerId = reach.OwnerIds[0];
			return Pass();
		}
		return Deny(DenyCodes.OwnerAmbiguous, "OwnerId is required — caller's reach contains multiple owners.");
	}

	private static ValidationResult EvaluateRead<TResource>(
		AuthorizationContext<TResource> context,
		IGrantedReadBase read,
		AccessReach reach)
		where TResource : notnull, IAuthorizableResource {

		if (!string.IsNullOrWhiteSpace(read.OwnerId)) {
			if (!reach.Contains(read.OwnerId!)) {
				return Deny(DenyCodes.OwnerNotInReach, "Requested owner is not in the caller's reach.");
			}
			// OwnerId supplied and in reach — stamp CallerAccessScope for cacheable reads.
			if (context.Resource is IGrantedCacheableReadBase cacheableWithOwner) {
				cacheableWithOwner.CallerAccessScope = context.AccessScope;
			}
			return Pass();
		}

		// Cacheable read: Global callers MUST supply OwnerId to prevent unbounded cache bucket.
		if (context.Resource is IGrantedCacheableReadBase) {
			if (context.AccessScope == AccessScope.Global) {
				return Deny(DenyCodes.CacheableReadOwnerIdRequired,
					"OwnerId is required for cross-tenant cacheable reads.");
			}
		}

		// OwnerId null — defer to handler (Pattern C). Reach already stashed on accessor.
		// Stamp CallerAccessScope for cacheable reads.
		if (context.Resource is IGrantedCacheableReadBase cacheable) {
			cacheable.CallerAccessScope = context.AccessScope;
		}
		return Pass();
	}

	private static ValidationResult EvaluateList(
		IGrantedListBase list,
		AccessReach reach) {
		if (list.OwnerIds is null) {
			// Stamp reach. Unrestricted = null (no bound).
			list.OwnerIds = reach.IsUnrestricted ? null : reach.OwnerIds;
			return Pass();
		}
		if (!reach.ContainsAll(list.OwnerIds)) {
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
		string outcome)
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
			activity.SetTag(AuthorizationTelemetry.StepTag, AuthorizationTelemetry.StepOwnerScope);
			activity.SetTag(AuthorizationTelemetry.ScopeTag, scope);
			activity.SetTag(AuthorizationTelemetry.DecisionTag, decision);
			activity.SetTag(AuthorizationTelemetry.ReasonTag, outcome);
			activity.SetTag(AuthorizationTelemetry.EvaluatorTag, nameof(GrantEvaluator));
			activity.SetTag(AuthorizationTelemetry.ResourceTypeTag, resourceType);
		}

		AuthorizationTelemetry.RecordDecision(
			stage: AuthorizationTelemetry.StageScope,
			step: AuthorizationTelemetry.StepOwnerScope,
			decision: decision,
			reason: outcome,
			scope: scope,
			evaluator: nameof(GrantEvaluator),
			resourceType: resourceType);
	}
}
