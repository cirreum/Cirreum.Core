namespace Cirreum.Authorization;

using Cirreum.Conductor;
using Cirreum.Security;
using FluentValidation.Results;
using System.Diagnostics;

/// <summary>
/// Stage 1 / Step 0 — opinionated owner-scope gate. Enforces OwnerId presence on
/// writes, OwnerId matching on tenant reads, OwnerId enrichment on tenant callers.
/// Customize via virtual extension points.
/// </summary>
/// <remarks>
/// <para>
/// Non-applicable resources (anything that does not implement
/// <see cref="IAuthorizableOwnerScopedResource"/>) short-circuit with a pass.
/// </para>
/// <para>
/// This class is NOT an <see cref="IScopeEvaluator"/>. It occupies its own dedicated
/// tier (Step 0) in the authorization pipeline because its concerns —
/// application-user state and tenant resolution — do not belong in the generic
/// scope-evaluator contract.
/// </para>
/// <para>
/// Cross-tenant callers are handled architecturally via
/// <see cref="AccessScope.Global"/> and <see cref="EvaluateGlobal"/> — there is
/// no separate role-based override. If you need finer-grained per-app behavior,
/// override the virtual <c>Evaluate*</c> hooks in a subclass.
/// </para>
/// </remarks>
public abstract class OwnerScopeEvaluatorBase {

	/// <summary>
	/// Evaluates the owner-scope gate against the resource in the given context.
	/// </summary>
	public Task<ValidationResult> EvaluateAsync<TResource>(
		AuthorizationContext<TResource> context)
		where TResource : notnull, IAuthorizableResource {

		// Not applicable — short-circuit pass.
		if (context.Resource is not IAuthorizableOwnerScopedResource owned) {
			return Task.FromResult(Pass());
		}

		// IsEnabled progressive check
		if (!this.CheckApplicationUserEnabled(context)) {
			this.EmitTelemetry(context, DenyCodes.UserDisabled);
			return Task.FromResult(Deny(DenyCodes.UserDisabled, "User is disabled."));
		}

		// AccessScope dispatch
		var result = context.AccessScope switch {
			AccessScope.None => this.EvaluateNone(context, owned),
			AccessScope.Global => this.EvaluateGlobal(context, owned),
			AccessScope.Tenant => this.EvaluateTenant(context, owned),
			_ => Deny(DenyCodes.ScopeNotPermitted, "Scope not permitted.")
		};

		// Stamp the caller's access scope on cacheable owner-scoped queries so that
		// the composed cache key isolates Global-scope callers from Tenant-scope
		// callers even when they resolve to the same OwnerId.
		if (result.IsValid && context.Resource is IAuthorizableOwnerScopedCacheableQuery cacheable) {
			cacheable.CallerAccessScope = context.AccessScope;
		}

		this.EmitTelemetry(context, result.IsValid ? AuthorizationTelemetry.ReasonPass : DenyReason(result));
		return Task.FromResult(result);
	}

	// Virtual extension points ——————————————————————————————————————

	/// <summary>
	/// Decision for <see cref="AccessScope.None"/> callers (anonymous or unresolved).
	/// Default: deny with <see cref="DenyCodes.AuthenticationRequired"/>.
	/// </summary>
	protected virtual ValidationResult EvaluateNone<TResource>(
		AuthorizationContext<TResource> ctx, IAuthorizableOwnerScopedResource owned)
		where TResource : notnull, IAuthorizableResource
		=> Deny(DenyCodes.AuthenticationRequired, "Authentication required.");

	/// <summary>
	/// Decision for <see cref="AccessScope.Global"/> callers (cross-tenant operators).
	/// Default: writes MUST name a tenant; cacheable owner-scoped reads MUST name a tenant
	/// (to prevent an unbounded null-owner cache bucket); other reads MAY omit OwnerId.
	/// </summary>
	protected virtual ValidationResult EvaluateGlobal<TResource>(
		AuthorizationContext<TResource> ctx, IAuthorizableOwnerScopedResource owned)
		where TResource : notnull, IAuthorizableResource {

		if (!string.IsNullOrWhiteSpace(owned.OwnerId)) {
			return Pass();
		}

		if (ctx.Resource is IAuthorizableCommand) {
			return Deny(DenyCodes.OwnerIdRequired, "OwnerId is required for cross-tenant writes.");
		}

		if (ctx.Resource is IAuthorizableOwnerScopedCacheableQuery) {
			return Deny(
				DenyCodes.CacheableReadOwnerIdRequired,
				"OwnerId is required for cross-tenant cacheable reads.");
		}

		return Pass();
	}

	/// <summary>
	/// Decision for <see cref="AccessScope.Tenant"/> callers. Default: resolve the
	/// caller's tenant; enrich OwnerId if null; deny on mismatch.
	/// </summary>
	protected virtual ValidationResult EvaluateTenant<TResource>(
		AuthorizationContext<TResource> ctx, IAuthorizableOwnerScopedResource owned)
		where TResource : notnull, IAuthorizableResource {

		var tenantId = this.ResolveTenantId(ctx);
		if (string.IsNullOrWhiteSpace(tenantId)) {
			return Deny(DenyCodes.TenantUnresolvable, "Tenant ID could not be resolved.");
		}
		if (string.IsNullOrWhiteSpace(owned.OwnerId)) {
			owned.OwnerId = tenantId; // enrich
			return Pass();
		}
		return string.Equals(owned.OwnerId, tenantId, StringComparison.OrdinalIgnoreCase)
			? Pass()
			: Deny(DenyCodes.OwnerIdMismatch, "OwnerId does not match caller's tenant.");
	}

	/// <summary>
	/// Resolves the caller's tenant ID from the authoritative source: the loaded
	/// application-user layer (<see cref="IOwnedApplicationUser.OwnerId"/>).
	/// Returns <c>null</c> when no loaded owned user is available.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Cirreum deliberately does NOT fall back to
	/// <see cref="AuthorizationContext{TResource}.TenantId"/> (which is sourced from
	/// IdP/JWT claims). The app-user layer exists precisely because the IdP claim is
	/// not authoritative for tenant ownership — the application decides who belongs
	/// to which tenant, and that decision is persisted server-side.
	/// </para>
	/// <para>
	/// In HTTP apps, the application user is loaded during the
	/// <c>IClaimsTransformer</c> middleware phase (via <c>IApplicationUserResolver</c>)
	/// and cached in <c>HttpContext.Items</c>, long before authorization runs. An
	/// unloaded user inside this evaluator means one of: (1) the request bypassed
	/// the HTTP auth pipeline (background job, test harness, internal dispatch),
	/// (2) the resolver returned null (user not found in app DB), or (3) the app
	/// hasn't configured an <c>IApplicationUserResolver</c>. All three deny with
	/// <see cref="DenyCodes.TenantUnresolvable"/>.
	/// </para>
	/// <para>
	/// If your application accepts the trade-off of trusting the IdP tenant claim as
	/// a progressive-enrichment bridge, override this method and return
	/// <c>ctx.TenantId</c> when the app user is not yet loaded.
	/// </para>
	/// </remarks>
	protected virtual string? ResolveTenantId<TResource>(AuthorizationContext<TResource> ctx)
		where TResource : notnull, IAuthorizableResource
		=> (ctx.UserState.ApplicationUser as IOwnedApplicationUser)?.OwnerId;

	/// <summary>
	/// Returns true if the caller's application user is present, enabled, and
	/// <see cref="IOwnedApplicationUser"/>. Default: passes when
	/// <see cref="IUserState.IsApplicationUserLoaded"/> is false (progressive enrichment);
	/// otherwise requires an enabled <see cref="IOwnedApplicationUser"/>.
	/// </summary>
	/// <remarks>
	/// Inside the owner-scope evaluator, a loaded application user that does NOT
	/// implement <see cref="IOwnedApplicationUser"/> is treated as a misconfiguration —
	/// an ownership-aware app should configure an ownership-aware user type.
	/// Global operators are expected to be <see cref="IOwnedApplicationUser"/> with
	/// <see cref="IOwnedApplicationUser.OwnerId"/> = <c>null</c>.
	/// </remarks>
	protected virtual bool CheckApplicationUserEnabled<TResource>(AuthorizationContext<TResource> ctx)
		where TResource : notnull, IAuthorizableResource {

		if (!ctx.UserState.IsApplicationUserLoaded) {
			return true;
		}
		return ctx.UserState.ApplicationUser is IOwnedApplicationUser { IsEnabled: true };
	}

	/// <summary>
	/// Emits telemetry tags to <see cref="Activity.Current"/> and records the
	/// decision to the <see cref="AuthorizationTelemetry"/> counter. Override
	/// to delegate to a different telemetry sink.
	/// </summary>
	protected virtual void EmitTelemetry<TResource>(
		AuthorizationContext<TResource> ctx,
		string outcome)
		where TResource : notnull, IAuthorizableResource {

		var isPass = outcome == AuthorizationTelemetry.ReasonPass;
		var decision = isPass
			? AuthorizationTelemetry.DecisionPass
			: AuthorizationTelemetry.DecisionDeny;
		var scope = ctx.AccessScope.ToString().ToLowerInvariant();
		var evaluator = this.GetType().Name;
		var resourceType = ctx.Resource.GetType().Name;

		var activity = Activity.Current;
		if (activity is not null) {
			activity.SetTag(AuthorizationTelemetry.StageTag, AuthorizationTelemetry.StageScope);
			activity.SetTag(AuthorizationTelemetry.StepTag, AuthorizationTelemetry.StepOwnerScope);
			activity.SetTag(AuthorizationTelemetry.ScopeTag, scope);
			activity.SetTag(AuthorizationTelemetry.DecisionTag, decision);
			activity.SetTag(AuthorizationTelemetry.ReasonTag, outcome);
			activity.SetTag(AuthorizationTelemetry.EvaluatorTag, evaluator);
			activity.SetTag(AuthorizationTelemetry.ResourceTypeTag, resourceType);
		}

		AuthorizationTelemetry.RecordDecision(
			stage: AuthorizationTelemetry.StageScope,
			step: AuthorizationTelemetry.StepOwnerScope,
			decision: decision,
			reason: outcome,
			scope: scope,
			evaluator: evaluator,
			resourceType: resourceType);
	}

	// Helpers ————————————————————————————————————————————————————

	/// <summary>Creates a passing <see cref="ValidationResult"/>.</summary>
	protected static ValidationResult Pass() => new();

	/// <summary>Creates a failing <see cref="ValidationResult"/> carrying a single denial.</summary>
	protected static ValidationResult Deny(string code, string message)
		=> new([new ValidationFailure(propertyName: code, errorMessage: message) {
			ErrorCode = code
		}]);

	private static string DenyReason(ValidationResult r)
		=> r.Errors.FirstOrDefault()?.ErrorCode ?? "UNKNOWN";
}
