namespace Cirreum.Authorization;

using Cirreum.Security;
using FluentValidation;

/// <summary>
/// Base Validator of <see cref="IAuthorizableResource"/> objects.
/// </summary>
/// <typeparam name="TResource">The Type of the resource.</typeparam>
/// <remarks>
/// <para>
/// This abstract class that provides a base implementation of the 
/// <see cref="IAuthorizationResourceValidator{TResource}"/> interface and
/// serves as a foundational validator for resources that implement the
/// <see cref="IAuthorizableResource"/> interface.
/// </para>
/// </remarks>
public abstract class AuthorizationValidatorBase<TResource>
	: AbstractValidator<AuthorizationContext<TResource>>
	, IAuthorizationResourceValidator<TResource>
	where TResource : IAuthorizableResource {

	/// <summary>
	/// Initializes a new instance of <see cref="AuthorizationValidatorBase{TResource}"/>
	/// </summary>
	protected AuthorizationValidatorBase() {

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

}