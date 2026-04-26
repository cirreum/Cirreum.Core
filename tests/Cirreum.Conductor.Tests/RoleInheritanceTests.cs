namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Authorization.Validators;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Security regression suite for role inheritance — locks in the property that
/// <c>HasRole(parentRole)</c> succeeds for callers assigned a child role. A bug here
/// would silently downgrade authorization: an authorizer that says "you must have User"
/// would fail for Manager-level callers, breaking least-privilege ergonomics and pushing
/// developers toward over-broad role declarations.
/// </summary>
[TestClass]
public class RoleInheritanceTests {

	private static AuthorizationRoleRegistryBase BuildRegistry() {
		var registry = new TestAuthorizationRoleRegistry(NullLogger<TestAuthorizationRoleRegistry>.Instance);
		registry.InitializeAsync().AsTask().Wait();
		return registry;
	}

	[TestMethod]
	public void Manager_role_inherits_User_via_Internal() {
		// Hierarchy: Manager → Internal → User
		// Caller assigned Manager must resolve effective roles including User.
		var registry = BuildRegistry();

		var effectiveRoles = registry.GetEffectiveRoles([ApplicationRoles.AppManagerRole]);

		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppManagerRole),
			"EffectiveRoles must include the directly-assigned role.");
		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppInternalRole),
			"EffectiveRoles must include directly-inherited role (Internal).");
		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppUserRole),
			"EffectiveRoles must include transitively-inherited role (User via Internal). " +
			"A bug here breaks the contract that 'HasRole(User)' passes for higher-privileged callers.");
	}

	[TestMethod]
	public void HasRoleValidator_passes_for_inherited_role() {
		// Caller assigned Manager. An authorizer that requires HasRole(User)
		// MUST pass — User is inherited transitively.
		var registry = BuildRegistry();
		var effectiveRoles = registry.GetEffectiveRoles([ApplicationRoles.AppManagerRole]);

		var validator = new TestRoleValidator(ApplicationRoles.AppUserRole);
		var result = validator.TestValidate(effectiveRoles);

		Assert.IsFalse(result.Errors.Any(),
			"HasRole(User) must pass for Manager-assigned caller. " +
			"Validator works against EffectiveRoles, which is inheritance-expanded.");
	}

	[TestMethod]
	public void HasAnyRoleValidator_passes_when_only_inherited_role_matches() {
		// Caller assigned Manager only. HasAnyRole([User]) must pass via inheritance.
		var registry = BuildRegistry();
		var effectiveRoles = registry.GetEffectiveRoles([ApplicationRoles.AppManagerRole]);

		var validator = new TestAnyRoleValidator(ApplicationRoles.AppUserRole);
		var result = validator.TestValidate(effectiveRoles);

		Assert.IsFalse(result.Errors.Any(),
			"HasAnyRole([User]) must pass for Manager-assigned caller via role inheritance.");
	}

	[TestMethod]
	public void Admin_inherits_full_chain_down_to_User() {
		// Hierarchy: Admin → Manager + Agent; Manager → Internal; Agent → Internal; Internal → User
		// Caller assigned Admin must reach User through both Manager and Agent paths.
		var registry = BuildRegistry();

		var effectiveRoles = registry.GetEffectiveRoles([ApplicationRoles.AppAdminRole]);

		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppAdminRole));
		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppManagerRole));
		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppAgentRole));
		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppInternalRole));
		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppUserRole),
			"Admin must transitively inherit User through the full hierarchy chain.");
	}

	[TestMethod]
	public void User_role_does_not_inherit_higher_privileges() {
		// Sanity: inheritance flows downward only. User must NOT inherit Manager/Admin.
		var registry = BuildRegistry();

		var effectiveRoles = registry.GetEffectiveRoles([ApplicationRoles.AppUserRole]);

		Assert.IsTrue(effectiveRoles.Contains(ApplicationRoles.AppUserRole));
		Assert.DoesNotContain(ApplicationRoles.AppInternalRole, effectiveRoles,
			"User must not inherit Internal — that would invert the hierarchy.");
		Assert.DoesNotContain(ApplicationRoles.AppManagerRole, effectiveRoles,
			"User must not inherit Manager — privilege escalation regression.");
		Assert.DoesNotContain(ApplicationRoles.AppAdminRole, effectiveRoles,
			"User must not inherit Admin — privilege escalation regression.");
	}

	private sealed class TestRoleValidator : AbstractValidator<System.Collections.Immutable.IImmutableSet<Role>> {
		public TestRoleValidator(Role required) {
			this.RuleFor(roles => roles).HasRole(required);
		}
	}

	private sealed class TestAnyRoleValidator : AbstractValidator<System.Collections.Immutable.IImmutableSet<Role>> {
		public TestAnyRoleValidator(params Role[] required) {
			this.RuleFor(roles => roles).HasAnyRole(required);
		}
	}
}
