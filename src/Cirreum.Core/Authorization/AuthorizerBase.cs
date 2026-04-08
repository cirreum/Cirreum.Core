namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation;

/// <summary>
/// Base Authorizer of <see cref="IAuthorizableObject"/> objects.
/// </summary>
/// <typeparam name="TResource">The Type of the resource.</typeparam>
/// <remarks>
/// <para>
/// This abstract class provides a base implementation of the
/// <see cref="IAuthorizer{TResource}"/> interface and
/// serves as a foundational authorizer for resources that implement the
/// <see cref="IAuthorizableObject"/> interface.
/// </para>
/// </remarks>
public abstract class AuthorizerBase<TResource>
	: AbstractValidator<AuthorizationContext<TResource>>
	, IAuthorizer<TResource>
	where TResource : IAuthorizableObject {

	/// <summary>
	/// Initializes a new instance of <see cref="AuthorizerBase{TResource}"/>
	/// </summary>
	protected AuthorizerBase() {

		this.RuleFor(context => context.UserState)
			.NotNull()
			.WithMessage("User state cannot be null");

		this.RuleFor(context => context.EffectiveRoles)
			.NotNull()
			.WithMessage("User roles cannot be null");

		this.RuleFor(context => context.Resource)
			.NotNull()
			.WithMessage("Resource cannot be null")
			.Must(resource => resource.GetType() == typeof(TResource))
			.WithMessage($"Resource must be exactly of type {typeof(TResource).Name}, not a subclass");
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
	/// Conditionally registers rules when the operation declares the specified
	/// <paramref name="permission"/> via <see cref="RequiresPermissionAttribute"/>. Use this to
	/// write permission-aware rules without scattering <c>ctx.Permissions.Contains</c>
	/// checks across each rule's <c>When</c>-clause.
	/// </summary>
	/// <param name="permission">The required permission that gates the nested rules.</param>
	/// <param name="configure">The action that registers rules applicable when the gate is met.</param>
	/// <example>
	/// <code>
	/// this.WhenRequires(IssuePermissions.Delete, () =>
	///     this.HasAnyRole(Roles.IssueManager, Roles.IssueEscalator));
	/// </code>
	/// </example>
	protected void WhenRequires(Permission permission, Action configure) {
		ArgumentNullException.ThrowIfNull(permission);
		ArgumentNullException.ThrowIfNull(configure);
		this.When(ctx => ctx.Permissions.Contains(permission), configure);
	}

}