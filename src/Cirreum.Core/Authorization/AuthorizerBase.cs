namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation;

/// <summary>
/// Base Authorizer of <see cref="IAuthorizableObject"/> objects.
/// </summary>
/// <typeparam name="TAuthorizableObject">The type of the <see cref="IAuthorizableObject"/> being authorized.</typeparam>
/// <remarks>
/// <para>
/// This abstract class provides a base implementation of the
/// <see cref="IAuthorizer{TAuthorizableObject}"/> interface and
/// serves as a foundational authorizer for types that implement the
/// <see cref="IAuthorizableObject"/> interface.
/// </para>
/// </remarks>
public abstract class AuthorizerBase<TAuthorizableObject>
	: AbstractValidator<AuthorizationContext<TAuthorizableObject>>
	, IAuthorizer<TAuthorizableObject>
	where TAuthorizableObject : IAuthorizableObject {

	/// <summary>
	/// Initializes a new instance of <see cref="AuthorizerBase{TAuthorizableObject}"/>
	/// </summary>
	protected AuthorizerBase() {

		this.RuleFor(context => context.UserState)
			.NotNull()
			.WithMessage("User state cannot be null");

		this.RuleFor(context => context.EffectiveRoles)
			.NotNull()
			.WithMessage("User roles cannot be null");

		this.RuleFor(context => context.AuthorizableObject)
			.NotNull()
			.WithMessage("Authorizable object cannot be null")
			.Must(obj => obj.GetType() == typeof(TAuthorizableObject))
			.WithMessage($"Authorizable object must be exactly of type {typeof(TAuthorizableObject).Name}, not a subclass");
	}

	/// <summary>
	/// Validates that the user has the specified role.
	/// </summary>
	/// <param name="role">The role to check.</param>
	protected void HasRole(Role role) {
		this.RuleFor(context => context.EffectiveRoles)
			.HasRole(role);
	}

	/// <summary>
	/// Must have at least one of the specified roles.
	/// </summary>
	/// <param name="roles">The roles to evaluate for inclusion.</param>
	protected void HasAnyRole(params Role[] roles) {
		this.RuleFor(context => context.EffectiveRoles)
			.HasAnyRole(roles);
	}

	/// <summary>
	/// Must have all roles specified.
	/// </summary>
	/// <param name="roles">The roles to evaluate for inclusion.</param>
	protected void HasAllRoles(params Role[] roles) {
		this.RuleFor(context => context.EffectiveRoles)
			.HasAllRoles(roles);
	}

	/// <summary>
	/// The user must have 2 or more roles.
	/// </summary>
	protected void HasTwoOrMoreRoles() {
		this.RuleFor(context => context.EffectiveRoles)
			.HasTwoOrMoreRoles();
	}

	/// <summary>
	/// The <see cref="IUserState.Principal"/> must have the specified <paramref name="claimType"/>
	/// the specified <paramref name="claimValue"/>.
	/// </summary>
	/// <param name="claimType">The type (name) of the claim.</param>
	/// <param name="claimValue">The value of the claim.</param>
	protected void HasClaim(string claimType, string claimValue) {
		this.RuleFor(context => context.UserState).HasClaim(claimType, claimValue);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the operation
	/// declares the specified <paramref name="permission"/> via
	/// <see cref="RequiresGrantAttribute"/>.
	/// </summary>
	/// <param name="permission">The permission whose presence enables the nested rules.</param>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse case.
	/// </returns>
	/// <example>
	/// <code>
	/// this.WhenRequiresGrant(IssuePermissions.Delete, () =>
	///     this.HasAnyRole(Roles.IssueManager, Roles.IssueEscalator))
	/// .Otherwise(() =>
	///     this.HasRole(Roles.ReadOnlyUser));
	/// </code>
	/// </example>
	protected IConditionBuilder WhenRequiresGrant(Permission permission, Action configure) {
		ArgumentNullException.ThrowIfNull(permission);
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => ctx.RequiredGrants.Contains(permission), configure);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the operation
	/// declares any of the specified <paramref name="permissions"/> via
	/// <see cref="RequiresGrantAttribute"/>.
	/// </summary>
	/// <param name="permissions">The permissions to check — rules apply when any is present.</param>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse case.
	/// </returns>
	/// <example>
	/// <code>
	/// this.WhenRequiresAnyGrant([IssuePermissions.Delete, IssuePermissions.Archive], () =>
	///     this.HasRole(Roles.IssueManager))
	/// .Otherwise(() =>
	///     this.HasAnyRole(Roles.User, Roles.ReadOnlyUser));
	/// </code>
	/// </example>
	protected IConditionBuilder WhenRequiresAnyGrant(Permission[] permissions, Action configure) {
		ArgumentNullException.ThrowIfNull(permissions);
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => ctx.RequiredGrants.ContainsAny(permissions), configure);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the operation
	/// declares all of the specified <paramref name="permissions"/> via
	/// <see cref="RequiresGrantAttribute"/>.
	/// </summary>
	/// <param name="permissions">The permissions to check — rules apply when all are present.</param>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse case.
	/// </returns>
	/// <example>
	/// <code>
	/// this.WhenRequiresAllGrants([IssuePermissions.Delete, IssuePermissions.Admin], () =>
	///     this.HasRole(Roles.SuperAdmin));
	/// </code>
	/// </example>
	protected IConditionBuilder WhenRequiresAllGrants(Permission[] permissions, Action configure) {
		ArgumentNullException.ThrowIfNull(permissions);
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => ctx.RequiredGrants.ContainsAll(permissions), configure);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the operation
	/// does not declare the specified <paramref name="permission"/> via
	/// <see cref="RequiresGrantAttribute"/>.
	/// </summary>
	/// <param name="permission">The permission whose absence enables the nested rules.</param>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse case.
	/// </returns>
	/// <example>
	/// <code>
	/// this.UnlessRequiresGrant(IssuePermissions.Delete, () =>
	///     this.HasRole(Roles.ReadOnlyUser))
	/// .Otherwise(() =>
	///     this.HasAnyRole(Roles.IssueManager, Roles.IssueEscalator));
	/// </code>
	/// </example>
	protected IConditionBuilder UnlessRequiresGrant(Permission permission, Action configure) {
		ArgumentNullException.ThrowIfNull(permission);
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => !ctx.RequiredGrants.Contains(permission), configure);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the operation
	/// does not declare any of the specified <paramref name="permissions"/> via
	/// <see cref="RequiresGrantAttribute"/>.
	/// </summary>
	/// <param name="permissions">The permissions to check — rules apply when none are present.</param>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse case.
	/// </returns>
	protected IConditionBuilder UnlessRequiresAnyGrant(Permission[] permissions, Action configure) {
		ArgumentNullException.ThrowIfNull(permissions);
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => !ctx.RequiredGrants.ContainsAny(permissions), configure);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the operation
	/// does not declare all of the specified <paramref name="permissions"/> via
	/// <see cref="RequiresGrantAttribute"/>.
	/// </summary>
	/// <param name="permissions">The permissions to check — rules apply when not all are present.</param>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse case.
	/// </returns>
	protected IConditionBuilder UnlessRequiresAllGrants(Permission[] permissions, Action configure) {
		ArgumentNullException.ThrowIfNull(permissions);
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => !ctx.RequiredGrants.ContainsAll(permissions), configure);
	}


	// Delegation rules
	// -------------------------------------------------------------
	// Convention: rules that constrain delegated invocations short-circuit to pass for
	// direct callers. The exception is Delegated, which is the explicit
	// "this operation must be delegated" gate.

	/// <summary>
	/// The user state must not be delegated — i.e. the caller is direct, not an
	/// M2M actor on behalf of a subject. Use for operations that must not be invoked through
	/// a delegated channel (e.g., wire transfers, sensitive admin actions).
	/// </summary>
	protected void NotDelegated() {
		this.RuleFor(context => context.UserState).NotDelegated();
	}

	/// <summary>
	/// The user state must have a delegation context — an M2M actor acting on behalf of a
	/// subject. Use for operations that must only be invoked through delegation.
	/// </summary>
	protected void Delegated() {
		this.RuleFor(context => context.UserState).Delegated();
	}

	/// <summary>
	/// When delegated, the actor must have authenticated via one of the allowed schemes
	/// (e.g. <c>"ApiKey"</c>, <c>"SignedRequest"</c>). Short-circuits to pass for direct callers.
	/// </summary>
	/// <param name="allowedSchemes">The authentication schemes permitted for the actor.</param>
	protected void HasDelegationActor(params string[] allowedSchemes) {
		this.RuleFor(context => context.UserState).HasDelegationActor(allowedSchemes);
	}

	/// <summary>
	/// When delegated, the delegation must have been applied within the specified
	/// maximum age. Anti-replay / staleness gate. Short-circuits to pass for direct callers.
	/// </summary>
	/// <param name="maxAge">The maximum age allowed since the delegation was applied.</param>
	protected void HasDelegationWithin(TimeSpan maxAge) {
		this.RuleFor(context => context.UserState).HasDelegationWithin(maxAge);
	}

	/// <summary>
	/// When delegated, the delegation must have been authorized via one of the allowed
	/// evidence types (e.g. <c>"ivr-session-validated"</c>, <c>"voice-biometric-verified"</c>).
	/// Short-circuits to pass for direct callers.
	/// </summary>
	/// <param name="allowedEvidenceTypes">The evidence types permitted for this operation.</param>
	protected void HasDelegationEvidence(params string[] allowedEvidenceTypes) {
		this.RuleFor(context => context.UserState).HasDelegationEvidence(allowedEvidenceTypes);
	}

	/// <summary>
	/// When delegated, the delegated scope must contain the specified permission.
	/// Per-operation scope-narrowing on delegated invocations. Short-circuits to pass for
	/// direct callers (whose authorization is governed by roles and grant attributes).
	/// </summary>
	/// <param name="required">The permission that must be present in the delegated scope.</param>
	protected void HasDelegationScope(Permission required) {
		this.RuleFor(context => context.UserState).HasDelegationScope(required);
	}

	/// <summary>
	/// When delegated, the delegated scope must contain at least one of the specified
	/// permissions. Short-circuits to pass for direct callers.
	/// </summary>
	/// <param name="permissions">The permissions — at least one must be present.</param>
	protected void HasAnyDelegationScope(params Permission[] permissions) {
		this.RuleFor(context => context.UserState).HasAnyDelegationScope(permissions);
	}

	/// <summary>
	/// When delegated, the delegated scope must contain all of the specified permissions.
	/// Short-circuits to pass for direct callers.
	/// </summary>
	/// <param name="permissions">The permissions — all must be present.</param>
	protected void HasAllDelegationScopes(params Permission[] permissions) {
		this.RuleFor(context => context.UserState).HasAllDelegationScopes(permissions);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the user state
	/// represents a delegated identity (an M2M actor acting on behalf of a subject).
	/// </summary>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse (direct-caller) case.
	/// </returns>
	/// <example>
	/// <code>
	/// this.WhenDelegated(() =&gt; {
	///     this.HasActor("SignedRequest");
	///     this.HasFreshDelegation(TimeSpan.FromMinutes(5));
	///     this.HasDelegationScope(new Permission("loans", "read"));
	/// })
	/// .Otherwise(() =&gt; {
	///     this.HasRole(Roles.LoanOfficer);
	/// });
	/// </code>
	/// </example>
	protected IConditionBuilder WhenDelegated(Action configure) {
		ArgumentNullException.ThrowIfNull(configure);
		return this.When(ctx => ctx.UserState.IsDelegated, configure);
	}

	/// <summary>
	/// Registers nested rules under a gate that applies them only when the user state
	/// represents a direct caller (i.e., NOT delegated).
	/// </summary>
	/// <param name="configure">The action that defines the nested rules.</param>
	/// <returns>
	/// An <see cref="IConditionBuilder"/> that can be chained with <c>.Otherwise(...)</c>
	/// to register rules for the inverse (delegated) case.
	/// </returns>
	/// <example>
	/// <code>
	/// this.UnlessDelegated(() =&gt; {
	///     this.HasRole(Roles.AdminUser);
	/// });
	/// </code>
	/// </example>
	protected IConditionBuilder UnlessDelegated(Action configure) {
		ArgumentNullException.ThrowIfNull(configure);
		return this.Unless(ctx => ctx.UserState.IsDelegated, configure);
	}

}