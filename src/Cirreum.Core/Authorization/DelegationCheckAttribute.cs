namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation.Results;

/// <summary>
/// Abstract base for declarative delegation rules expressed as attributes on operation
/// types. Each derived attribute encapsulates both its data (constructor / property
/// state) and its enforcement logic (<see cref="Check"/>). A single
/// <c>DelegationConstraint</c> discovers every <see cref="DelegationCheckAttribute"/>
/// applied to an operation at Stage 1 Step 1 and invokes <see cref="Check"/> on each.
/// </summary>
/// <remarks>
/// <para>
/// <b>Design pattern.</b> Inspired by <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/>
/// — the attribute carries both declarative metadata and the validation logic. One
/// framework constraint enumerates all derivatives in a single per-request pass, avoiding
/// the "N constraints registered, N dispatches per request" cost of pairing each
/// attribute with its own constraint class.
/// </para>
/// <para>
/// <b>App extensibility.</b> Authors of app-specific delegation checks subclass
/// <see cref="DelegationCheckAttribute"/>, override <see cref="Check"/>, and decorate
/// their operations. No constraint class to author, no DI registration to remember —
/// the framework's <c>DelegationConstraint</c> picks them up automatically via
/// <c>Type.GetCustomAttributes&lt;DelegationCheckAttribute&gt;(inherit: true)</c>.
/// </para>
/// <para>
/// <b>Facet attributes — fail-closed contract.</b> Attributes that further constrain a
/// delegation context (allowed actor schemes, freshness, evidence type, scope membership)
/// derive from <see cref="RequiresDelegationCheckAttribute"/>, which self-enforces the
/// "delegation is mandatory" precondition. A facet attribute applied without an
/// accompanying <see cref="RequiresDelegationAttribute"/> still fails closed against a
/// direct caller — facets are intersections with the requirement that delegation be
/// present, never unions.
/// </para>
/// <para>
/// <b>Channel gates.</b> <see cref="RequiresDirectCallerAttribute"/> (fails when delegated)
/// and <see cref="RequiresDelegationAttribute"/> (fails when not delegated) gate the
/// delegation channel itself and derive directly from this base — they are not facet
/// constraints.
/// </para>
/// </remarks>
public abstract class DelegationCheckAttribute : Attribute {

	/// <summary>
	/// Performs the attribute's delegation check against the supplied user state.
	/// </summary>
	/// <param name="userState">
	/// The current invocation's <see cref="IUserState"/>. The framework guarantees this is
	/// non-null and authenticated when invoked from <c>DelegationConstraint</c>'s pipeline
	/// stage — these conditions are pre-asserted by <c>DefaultAuthorizationEvaluator</c>.
	/// </param>
	/// <returns>
	/// <see langword="null"/> when the check passes (the attribute's constraint is
	/// satisfied or not applicable to this caller); a populated <see cref="ValidationFailure"/>
	/// when the check fails. The failure's <see cref="ValidationFailure.ErrorCode"/> should
	/// be a stable identifier suitable for telemetry tags and audit codes.
	/// </returns>
	public abstract ValidationFailure? Check(IUserState userState);

}
