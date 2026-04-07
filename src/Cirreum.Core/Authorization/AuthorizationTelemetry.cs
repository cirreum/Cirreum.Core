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

	/// <summary>Stage 1 — scope evaluation (owner-scope gate + generic scope evaluators).</summary>
	public const string StageScope = "scope";

	/// <summary>Stage 2 — resource authorizers (<see cref="IResourceAuthorizer{TResource}"/>).</summary>
	public const string StageResource = "resource";

	/// <summary>Stage 3 — policy validators (<see cref="IPolicyValidator"/>).</summary>
	public const string StagePolicy = "policy";

	// Step names ———————————————————————————————————————————————

	/// <summary>Stage 1, Step 0 — owner-scope gate (<see cref="Grants.GrantEvaluator"/>).</summary>
	public const string StepOwnerScope = "owner-scope";

	/// <summary>Stage 1, Step 1 — generic <see cref="IScopeEvaluator"/> chain.</summary>
	public const string StepScopeEvaluator = "scope-evaluator";

	/// <summary>Stage 2 — resource authorizer (<see cref="ResourceAuthorizerBase{TResource}"/>).</summary>
	public const string StepResourceAuthorizer = "resource-authorizer";

	/// <summary>Stage 3 — policy validator (<see cref="IPolicyValidator"/>).</summary>
	public const string StepPolicyValidator = "policy-validator";

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

	// Reach cache level values ————————————————————————————————

	/// <summary>Reach resolved via admin bypass (always live, never cached).</summary>
	public const string ReachLevelBypass = "bypass";

	/// <summary>Reach resolved from L1 scoped in-memory cache (per-request dedup).</summary>
	public const string ReachLevelL1Hit = "l1-hit";

	/// <summary>Reach resolved via L2 cross-request cache (may be hot or cold).</summary>
	public const string ReachLevelL2 = "l2";

	/// <summary>Reach denied early (unauthenticated or no permissions).</summary>
	public const string ReachLevelDeniedEarly = "denied-early";

	// Reach tag names ————————————————————————————————————————

	/// <summary>Tag: reach cache level (bypass / l1-hit / l2 / denied-early).</summary>
	public const string ReachCacheLevelTag = "cirreum.authz.reach.cache_level";

	/// <summary>Tag: domain feature for reach resolution.</summary>
	public const string ReachDomainTag = "cirreum.authz.reach.domain";

	// Metrics ——————————————————————————————————————————————————

	/// <summary>Metric: total authorization decisions (tagged with stage/step/decision/reason).</summary>
	public const string DecisionsTotalMetric = "cirreum.authz.decisions";

	/// <summary>Metric: authorization pipeline duration in milliseconds.</summary>
	public const string DurationMetric = "cirreum.authz.duration";

	/// <summary>Metric: reach resolution duration in milliseconds (L2/cold path only).</summary>
	public const string ReachDurationMetric = "cirreum.authz.reach.duration";

	/// <summary>Metric: reach resolution cache hit/miss counter.</summary>
	public const string ReachCacheMetric = "cirreum.authz.reach.cache";

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

	private static readonly Histogram<double> _reachDurationHistogram = _meter.CreateHistogram<double>(
		ReachDurationMetric,
		unit: "ms",
		description: "Reach resolution duration in milliseconds (L2/cold path)");

	private static readonly Counter<long> _reachCacheCounter = _meter.CreateCounter<long>(
		ReachCacheMetric,
		description: "Reach resolution cache hit/miss counter");

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

	// Reach Resolution ——————————————————————————————————

	/// <summary>
	/// Records a reach resolution event (cache hit/miss/bypass) with optional duration
	/// for L2/cold-path resolutions.
	/// </summary>
	internal static void RecordReachResolution(
		string? domain,
		string? resourceType,
		string cacheLevel,
		double? durationMs = null) {

		var tags = new TagList {
			{ ReachCacheLevelTag, cacheLevel }
		};

		if (domain is not null) {
			tags.Add(ReachDomainTag, domain);
		}
		if (resourceType is not null) {
			tags.Add(ResourceTypeTag, resourceType);
		}

		_reachCacheCounter.Add(1, tags);

		if (durationMs.HasValue) {
			_reachDurationHistogram.Record(durationMs.Value, tags);
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
