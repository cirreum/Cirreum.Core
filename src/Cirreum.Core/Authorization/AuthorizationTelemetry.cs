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

	/// <summary>Stage 1, Step 0 — owner-scope gate (<see cref="OwnerScopeEvaluatorBase"/>).</summary>
	public const string StepOwnerScope = "owner-scope";

	/// <summary>Stage 1, Step 1 — generic <see cref="IScopeEvaluator"/> chain.</summary>
	public const string StepScopeEvaluator = "scope-evaluator";

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

	// Metrics ——————————————————————————————————————————————————

	/// <summary>Metric: total authorization decisions (tagged with stage/step/decision/reason).</summary>
	public const string DecisionsTotalMetric = "cirreum.authz.decisions";

	// ActivitySource / Meter ————————————————————————————————

	internal static readonly ActivitySource ActivitySource =
		new(CirreumTelemetry.ActivitySources.Authorization, CirreumTelemetry.Version);

	private static readonly Meter _meter =
		new(CirreumTelemetry.Meters.Authorization, CirreumTelemetry.Version);

	private static readonly Counter<long> _decisionsCounter = _meter.CreateCounter<long>(
		DecisionsTotalMetric,
		description: "Total number of authorization decisions recorded by the Cirreum pipeline");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool HasListeners() => ActivitySource.HasListeners();

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
