namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum.Authorization;
using Cirreum.Security;

[TestClass]
public class AuthenticationBoundaryTests {

	// IUserState default interface member
	// -------------------------------------------------------------

	[TestMethod]
	public void IUserState_default_AuthenticationBoundary_is_None_when_not_overridden() {
		IUserState user = new MinimalUserState();
		Assert.AreEqual(AuthenticationBoundary.None, user.AuthenticationBoundary);
	}

	[TestMethod]
	public void IUserState_default_IsAuthenticationBoundaryResolved_is_false() {
		IUserState user = new MinimalUserState();
		Assert.IsFalse(user.IsAuthenticationBoundaryResolved);
	}

	// UserStateBase backing field + setter
	// -------------------------------------------------------------

	[TestMethod]
	public void UserStateBase_AuthenticationBoundary_defaults_to_None() {
		var user = new TestUserState();
		Assert.AreEqual(AuthenticationBoundary.None, user.AuthenticationBoundary);
	}

	[TestMethod]
	public void UserStateBase_IsAuthenticationBoundaryResolved_defaults_to_false() {
		var user = new TestUserState();
		Assert.IsFalse(user.IsAuthenticationBoundaryResolved);
	}

	[TestMethod]
	public void UserStateBase_SetAuthenticationBoundary_persists_value() {
		var user = new AuthenticationBoundaryExposingUserState();
		user.StampScope(AuthenticationBoundary.Global);
		Assert.AreEqual(AuthenticationBoundary.Global, user.AuthenticationBoundary);

		user.StampScope(AuthenticationBoundary.Tenant);
		Assert.AreEqual(AuthenticationBoundary.Tenant, user.AuthenticationBoundary);
	}

	[TestMethod]
	public void UserStateBase_SetAuthenticationBoundary_marks_resolved() {
		var user = new AuthenticationBoundaryExposingUserState();
		Assert.IsFalse(user.IsAuthenticationBoundaryResolved);

		user.StampScope(AuthenticationBoundary.Global);
		Assert.IsTrue(user.IsAuthenticationBoundaryResolved);
	}

	[TestMethod]
	public void UserStateBase_SetAuthenticationBoundary_to_None_still_marks_resolved() {
		var user = new AuthenticationBoundaryExposingUserState();
		user.StampScope(AuthenticationBoundary.None);

		Assert.AreEqual(AuthenticationBoundary.None, user.AuthenticationBoundary);
		Assert.IsTrue(user.IsAuthenticationBoundaryResolved, "Explicitly setting None should still mark as resolved");
	}

	// AuthorizationContext<T> passthrough
	// -------------------------------------------------------------

	[TestMethod]
	public void AuthorizationContext_AuthenticationBoundary_reflects_UserState() {
		var user = new AuthenticationBoundaryExposingUserState();
		user.StampScope(AuthenticationBoundary.Tenant);

		var authCtx = new AuthorizationContext<TestResource>(
			UserState: user,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: new TestResource());

		Assert.AreEqual(AuthenticationBoundary.Tenant, authCtx.AuthenticationBoundary);
	}

	[TestMethod]
	public void AuthorizationContext_AuthenticationBoundary_is_None_when_UserState_is_None() {
		var user = new TestUserState();

		var authCtx = new AuthorizationContext<TestResource>(
			UserState: user,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: new TestResource());

		Assert.AreEqual(AuthenticationBoundary.None, authCtx.AuthenticationBoundary);
	}

	private sealed class TestResource : IAuthorizableObject;

	/// <summary>
	/// Subclass that publicly exposes the protected SetAuthenticationBoundary setter
	/// for assertion purposes.
	/// </summary>
	private sealed class AuthenticationBoundaryExposingUserState : UserStateBase {
		public override bool IsAuthenticationComplete => true;
		public void StampScope(AuthenticationBoundary scope) => base.SetAuthenticationBoundary(scope);
	}

	/// <summary>
	/// Minimal IUserState implementation with no AuthenticationBoundary override — exercises
	/// the default interface member on IUserState.
	/// </summary>
	private sealed class MinimalUserState : IUserState {
		public bool IsAuthenticated => false;
		public bool IsAuthenticationComplete => true;
		public string Id => string.Empty;
		public string Name => string.Empty;
		public IdentityProviderType Provider => IdentityProviderType.None;
		public AuthenticationLibraryType AuthenticationType => AuthenticationLibraryType.None;
		public UserProfile Profile => UserProfile.Anonymous;
		public ClaimsPrincipal Principal => AnonymousUser.Shared;
		public ClaimsIdentity Identity => AnonymousUser.Shared.Identity;
		public IApplicationUser? ApplicationUser => null;
		public bool IsApplicationUserLoaded => false;
		public DateTimeOffset? SessionStartTime => null;
		public DateTimeOffset? LastActivityTime => null;
		public void UpdateActivity() { }
		public bool IsSessionExpired(TimeSpan timeout) => true;
		public T? GetApplicationUser<T>() where T : class, IApplicationUser => null;
	}
}
