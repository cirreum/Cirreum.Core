namespace Cirreum.Authorization;

using Cirreum.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

/// <summary>
/// Centralized telemetry for the Cirreum authorization pipeline. Publishes
/// a shared <see cref="ActivitySource"/> and <see cref="Meter"/>, plus stable
/// tag-name and stage/step constants used across pipeline evaluators.
/// </summary>
/// <remarks>
/// <para>
/// Evaluators should tag their <see cref="Activity"/> with the <c>Stage*</c>,
/// <c>Step*</c>, and <c>*Tag</c> constants declared here, and call
/// <see cref="RecordDecision"/> to increment the decision counter with a
/// consistent set of dimensions.
/// </para>
/// </remarks>
public static class AuthorizationTelemetry {

	// Stage names ——————————————————————————————————————————————

	/// <summary>Stage 1 — scope evaluation (owner-scope gate + authorization constraints).</summary>
	public const string StageScope = "scope";

	/// <summary>Stage 2 — object authorizers (<see cref="IAuthorizer{TAuthorizableObject}"/>).</summary>
	public const string StageResource = "resource";

	/// <summary>Stage 3 — policy validators (<see cref="IPolicyValidator"/>).</summary>
	public const string StagePolicy = "policy";

	/// <summary>Stage — resource access evaluation (<see cref="Resources.IResourceAccessEvaluator"/>).</summary>
	public const string StageResourceAccess = "resource-access";

	// Step names ———————————————————————————————————————————————

	/// <summary>Stage 1, Step 0 — owner-scope gate (<see cref="Operations.Grants.OperationGrantEvaluator"/>).</summary>
	public const string StepOwnerScope = "owner-scope";

	/// <summary>Stage 1, Step 0s — self-identity gate (<see cref="Operations.Grants.OperationGrantEvaluator"/>).</summary>
	public const string StepSelfIdentity = "self-identity";

	/// <summary>Stage 1, Step 1 — <see cref="Operations.IAuthorizationConstraint"/> chain.</summary>
	public const string StepConstraint = "constraint";

	/// <summary>Stage 2 — object authorizer (<see cref="AuthorizerBase{TAuthorizableObject}"/>).</summary>
	public const string StepResourceAuthorizer = "resource-authorizer";

	/// <summary>Stage 3 — policy validator (<see cref="IPolicyValidator"/>).</summary>
	public const string StepPolicyValidator = "policy-validator";

	/// <summary>Resource access check (<see cref="Resources.ResourceAccessEvaluator"/>).</summary>
	public const string StepResourceAccessCheck = "resource-access-check";

	// Decision values —————————————————————————————————————————

	/// <summary>Decision tag value: the stage passed.</summary>
	public const string DecisionPass = "pass";

	/// <summary>Decision tag value: the stage denied.</summary>
	public const string DecisionDeny = "deny";

	/// <summary>Reason tag value when a stage passed (no specific reason code).</summary>
	public const string ReasonPass = "pass";

	// Tag names ———————————————————————————————————————————————

	/// <summary>Tag: authorization stage (<see cref="StageScope"/>, <see cref="StageResource"/>, <see cref="StagePolicy"/>).</summary>
	public const string StageTag = "cirreum.authz.stage";

	/// <summary>Tag: step within a stage (e.g. <see cref="StepOwnerScope"/>).</summary>
	public const string StepTag = "cirreum.authz.step";

	/// <summary>Tag: access scope (none / global / tenant).</summary>
	public const string ScopeTag = "cirreum.authz.scope";

	/// <summary>Tag: decision (<see cref="DecisionPass"/> / <see cref="DecisionDeny"/>).</summary>
	public const string DecisionTag = "cirreum.authz.decision";

	/// <summary>Tag: machine-readable reason (deny code from <see cref="DenyCodes"/> or <see cref="ReasonPass"/>).</summary>
	public const string ReasonTag = "cirreum.authz.reason";

	/// <summary>Tag: evaluator type name.</summary>
	public const string EvaluatorTag = "cirreum.authz.evaluator";

	/// <summary>Tag: resource CLR type name.</summary>
	public const string ResourceTypeTag = "cirreum.authz.resource_type";

	// Grant cache level values ————————————————————————————————

	/// <summary>Grant resolved via admin bypass (always live, never cached).</summary>
	public const string GrantLevelBypass = "bypass";

	/// <summary>Grant resolved from L1 scoped in-memory cache (per-request dedup).</summary>
	public const string GrantLevelL1Hit = "l1-hit";

	/// <summary>Grant resolved via L2 cross-request cache (may be hot or cold).</summary>
	public const string GrantLevelL2 = "l2";

	/// <summary>Grant denied early (unauthenticated or no permissions).</summary>
	public const string GrantLevelDeniedEarly = "denied-early";

	// Grant tag names ————————————————————————————————————————

	/// <summary>Tag: grant cache level (bypass / l1-hit / l2 / denied-early).</summary>
	public const string GrantCacheLevelTag = "cirreum.authz.grant.cache_level";

	/// <summary>Tag: domain feature for grant resolution.</summary>
	public const string GrantDomainTag = "cirreum.authz.grant.domain";

	// Metrics ——————————————————————————————————————————————————

	/// <summary>Metric: total authorization decisions (tagged with stage/step/decision/reason).</summary>
	public const string DecisionsTotalMetric = "cirreum.authz.decisions";

	/// <summary>Metric: authorization pipeline duration in milliseconds.</summary>
	public const string DurationMetric = "cirreum.authz.duration";

	/// <summary>Metric: grant resolution duration in milliseconds (L2/cold path only).</summary>
	public const string GrantDurationMetric = "cirreum.authz.grant.duration";

	/// <summary>Metric: grant resolution cache hit/miss counter.</summary>
	public const string GrantCacheMetric = "cirreum.authz.grant.cache";

	// ActivitySource / Meter ————————————————————————————————

	internal static readonly ActivitySource ActivitySource =
		new(CirreumTelemetry.ActivitySources.Authorization, CirreumTelemetry.Version);

	private static readonly Meter _meter =
		new(CirreumTelemetry.Meters.Authorization, CirreumTelemetry.Version);

	private static readonly Counter<long> _decisionsCounter = _meter.CreateCounter<long>(
		DecisionsTotalMetric,
		description: "Total number of authorization decisions recorded by the Cirreum pipeline");

	private static readonly Histogram<double> _durationHistogram = _meter.CreateHistogram<double>(
		DurationMetric,
		unit: "ms",
		description: "Authorization pipeline processing duration in milliseconds");

	private static readonly Histogram<double> _grantDurationHistogram = _meter.CreateHistogram<double>(
		GrantDurationMetric,
		unit: "ms",
		description: "Grant resolution duration in milliseconds (L2/cold path)");

	private static readonly Counter<long> _grantCacheCounter = _meter.CreateCounter<long>(
		GrantCacheMetric,
		description: "Grant resolution cache hit/miss counter");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool HasListeners() => ActivitySource.HasListeners();

	// Activity Management ——————————————————————————————————

	/// <summary>
	/// Starts a child activity for the authorization pipeline. Returns null when
	/// no listeners are attached — all downstream code null-checks the activity.
	/// </summary>
	internal static Activity? StartActivity(string resourceType) {
		var activity = ActivitySource.StartActivity("Authorize Resource");
		activity?.SetTag(ResourceTypeTag, resourceType);
		return activity;
	}

	// Pipeline Duration ——————————————————————————————————

	/// <summary>
	/// Records total authorization pipeline duration and tags the activity with the outcome.
	/// </summary>
	internal static void RecordDuration(
		Activity? activity,
		string resourceType,
		double durationMs,
		string decision,
		string? reason = null,
		string? denyStage = null) {

		if (activity is not null) {
			activity.SetTag(DecisionTag, decision);
			if (reason is not null) {
				activity.SetTag(ReasonTag, reason);
			}
			if (denyStage is not null) {
				activity.SetTag(StageTag, denyStage);
			}
			activity.SetStatus(decision == DecisionPass
				? ActivityStatusCode.Ok
				: ActivityStatusCode.Error, reason);
		}

		var tags = new TagList {
			{ ResourceTypeTag, resourceType },
			{ DecisionTag, decision }
		};

		if (denyStage is not null) {
			tags.Add(StageTag, denyStage);
		}

		_durationHistogram.Record(durationMs, tags);
	}

	// Grant Resolution ——————————————————————————————————

	/// <summary>
	/// Records a grant resolution event (cache hit/miss/bypass) with optional duration
	/// for L2/cold-path resolutions.
	/// </summary>
	internal static void RecordGrantResolution(
		string? domain,
		string? resourceType,
		string cacheLevel,
		double? durationMs = null) {

		var tags = new TagList {
			{ GrantCacheLevelTag, cacheLevel }
		};

		if (domain is not null) {
			tags.Add(GrantDomainTag, domain);
		}
		if (resourceType is not null) {
			tags.Add(ResourceTypeTag, resourceType);
		}

		_grantCacheCounter.Add(1, tags);

		if (durationMs.HasValue) {
			_grantDurationHistogram.Record(durationMs.Value, tags);
		}
	}

	/// <summary>
	/// Records an authorization decision to the shared decisions counter.
	/// </summary>
	/// <param name="stage">Stage name — use <see cref="StageScope"/> / <see cref="StageResource"/> / <see cref="StagePolicy"/>.</param>
	/// <param name="step">Step name within the stage (e.g. <see cref="StepOwnerScope"/>).</param>
	/// <param name="decision"><see cref="DecisionPass"/> or <see cref="DecisionDeny"/>.</param>
	/// <param name="reason">Machine-readable reason — a <see cref="DenyCodes"/> value for denies, <see cref="ReasonPass"/> for passes.</param>
	/// <param name="scope">Access scope name (none / global / tenant).</param>
	/// <param name="evaluator">Evaluator type name.</param>
	/// <param name="resourceType">Resource CLR type name.</param>
	public static void RecordDecision(
		string stage,
		string step,
		string decision,
		string reason,
		string? scope = null,
		string? evaluator = null,
		string? resourceType = null) {

		var tags = new TagList {
			{ StageTag, stage },
			{ StepTag, step },
			{ DecisionTag, decision },
			{ ReasonTag, reason }
		};

		if (scope is not null) {
			tags.Add(ScopeTag, scope);
		}
		if (evaluator is not null) {
			tags.Add(EvaluatorTag, evaluator);
		}
		if (resourceType is not null) {
			tags.Add(ResourceTypeTag, resourceType);
		}

		_decisionsCounter.Add(1, tags);
	}
}
