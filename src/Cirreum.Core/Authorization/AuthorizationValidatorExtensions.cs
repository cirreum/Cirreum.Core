namespace Cirreum.Authorization;

using Cirreum.Authorization.Validators;
using Cirreum.Security;
using FluentValidation;

/// <summary>
/// Extension methods for validators to use the custom validators.
/// </summary>
public static class AuthorizationValidatorExtensions {

	public static IRuleBuilderOptions<T, IEnumerable<Role>> HasRole<T>(
		this IRuleBuilder<T, IEnumerable<Role>> ruleBuilder, Role role) {
		return ruleBuilder.SetValidator(new HasRoleValidator<T>(role));
	}

	public static IRuleBuilderOptions<T, IEnumerable<Role>> HasAnyRole<T>(
		this IRuleBuilder<T, IEnumerable<Role>> ruleBuilder, params Role[] roles) {
		return ruleBuilder.SetValidator(new HasAnyRoleValidator<T>(roles));
	}

	public static IRuleBuilderOptions<T, IEnumerable<Role>> HasAllRoles<T>(
		this IRuleBuilder<T, IEnumerable<Role>> ruleBuilder, params Role[] roles) {
		return ruleBuilder.SetValidator(new HasAllRolesValidator<T>(roles));
	}

	public static IRuleBuilderOptions<T, IEnumerable<Role>> HasTwoOrMoreRoles<T>(
		this IRuleBuilder<T, IEnumerable<Role>> ruleBuilder) {
		return ruleBuilder.SetValidator(new HasTwoOrMoreRolesValidator<T>());
	}

	/// <summary>
	/// Validates that the user state has a specific claim with a specific value.
	/// </summary>
	/// <typeparam name="T">The type being validated</typeparam>
	/// <param name="ruleBuilder">The rule builder</param>
	/// <param name="claimType">The type of the claim</param>
	/// <param name="claimValue">The value of the claim</param>
	/// <returns>Rule builder options for chaining</returns>
	public static IRuleBuilderOptions<T, IUserState> HasClaim<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		string claimType,
		string claimValue) {
		return ruleBuilder.SetValidator(new HasClaimValidator<T>(claimType, claimValue));
	}


	// Delegation rules
	// -------------------------------------------------------------
	// Convention: rules that constrain delegated invocations short-circuit to pass for
	// direct callers. The exception is Delegated, which is the explicit
	// "this operation must be delegated" gate. Combine Delegated() with any
	// of the more specific Has*Delegation* rules below when both presence and a specific
	// property are required.

	/// <summary>
	/// Validates that the user state is not delegated — i.e. the caller is direct,
	/// not an M2M actor on behalf of a subject. Used by authorizers for operations that
	/// must not be invoked through a delegated channel.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> NotDelegated<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder) {
		return ruleBuilder.SetValidator(new NotDelegatedValidator<T>());
	}

	/// <summary>
	/// Validates that the user state has a delegation context — the operation is being
	/// invoked by an M2M actor acting on behalf of a subject. Used by authorizers for
	/// operations that must only be invoked through delegation.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> Delegated<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder) {
		return ruleBuilder.SetValidator(new DelegatedValidator<T>());
	}

	/// <summary>
	/// Validates that the delegating actor authenticated via one of the allowed schemes
	/// (e.g. <c>"ApiKey"</c>, <c>"SignedRequest"</c>). Short-circuits to pass for direct
	/// callers; combine with <see cref="Delegated{T}"/> when delegation is mandatory.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> HasDelegationActor<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		params string[] allowedSchemes) {
		return ruleBuilder.SetValidator(new HasDelegationActorValidator<T>(allowedSchemes));
	}

	/// <summary>
	/// Validates that the delegation was applied within the specified maximum age.
	/// Anti-replay / staleness gate. Short-circuits to pass for direct callers; combine
	/// with <see cref="Delegated{T}"/> when delegation is mandatory.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> HasDelegationWithin<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		TimeSpan maxAge) {
		return ruleBuilder.SetValidator(new HasDelegationWithinValidator<T>(maxAge));
	}

	/// <summary>
	/// Validates that the delegation was authorized via one of the allowed evidence types
	/// (e.g. <c>"ivr-session-validated"</c>, <c>"voice-biometric-verified"</c>). Short-circuits
	/// to pass for direct callers; combine with <see cref="Delegated{T}"/> when
	/// delegation is mandatory.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> HasDelegationEvidence<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		params string[] allowedEvidenceTypes) {
		return ruleBuilder.SetValidator(new HasDelegationEvidenceValidator<T>(allowedEvidenceTypes));
	}

	/// <summary>
	/// Validates that the delegated scope contains the specified permission. Per-operation
	/// scope-narrowing on delegated invocations. Short-circuits to pass for direct callers.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> HasDelegationScope<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		Permission required) {
		return ruleBuilder.SetValidator(new HasDelegationScopeValidator<T>(required));
	}

	/// <summary>
	/// Validates that the delegated scope contains at least one of the specified permissions.
	/// Short-circuits to pass for direct callers.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> HasAnyDelegationScope<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		params Permission[] permissions) {
		return ruleBuilder.SetValidator(new HasAnyDelegationScopeValidator<T>(permissions));
	}

	/// <summary>
	/// Validates that the delegated scope contains all of the specified permissions.
	/// Short-circuits to pass for direct callers.
	/// </summary>
	public static IRuleBuilderOptions<T, IUserState> HasAllDelegationScopes<T>(
		this IRuleBuilder<T, IUserState> ruleBuilder,
		params Permission[] permissions) {
		return ruleBuilder.SetValidator(new HasAllDelegationScopesValidator<T>(permissions));
	}

}