namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum.Authorization;
using Cirreum.Security;

[TestClass]
public class AuthenticationScopeTests {

	// IUserState default interface member
	// -------------------------------------------------------------

	[TestMethod]
	public void IUserState_default_AuthenticationScope_is_None_when_not_overridden() {
		IUserState user = new MinimalUserState();
		Assert.AreEqual(AuthenticationScope.None, user.AuthenticationScope);
	}

	[TestMethod]
	public void IUserState_default_IsAuthenticationScopeResolved_is_false() {
		IUserState user = new MinimalUserState();
		Assert.IsFalse(user.IsAuthenticationScopeResolved);
	}

	// UserStateBase backing field + setter
	// -------------------------------------------------------------

	[TestMethod]
	public void UserStateBase_AuthenticationScope_defaults_to_None() {
		var user = new TestUserState();
		Assert.AreEqual(AuthenticationScope.None, user.AuthenticationScope);
	}

	[TestMethod]
	public void UserStateBase_IsAuthenticationScopeResolved_defaults_to_false() {
		var user = new TestUserState();
		Assert.IsFalse(user.IsAuthenticationScopeResolved);
	}

	[TestMethod]
	public void UserStateBase_SetAuthenticationScope_persists_value() {
		var user = new AuthenticationScopeExposingUserState();
		user.StampScope(AuthenticationScope.Global);
		Assert.AreEqual(AuthenticationScope.Global, user.AuthenticationScope);

		user.StampScope(AuthenticationScope.Tenant);
		Assert.AreEqual(AuthenticationScope.Tenant, user.AuthenticationScope);
	}

	[TestMethod]
	public void UserStateBase_SetAuthenticationScope_marks_resolved() {
		var user = new AuthenticationScopeExposingUserState();
		Assert.IsFalse(user.IsAuthenticationScopeResolved);

		user.StampScope(AuthenticationScope.Global);
		Assert.IsTrue(user.IsAuthenticationScopeResolved);
	}

	[TestMethod]
	public void UserStateBase_SetAuthenticationScope_to_None_still_marks_resolved() {
		var user = new AuthenticationScopeExposingUserState();
		user.StampScope(AuthenticationScope.None);

		Assert.AreEqual(AuthenticationScope.None, user.AuthenticationScope);
		Assert.IsTrue(user.IsAuthenticationScopeResolved, "Explicitly setting None should still mark as resolved");
	}

	// AuthorizationContext<T> passthrough
	// -------------------------------------------------------------

	[TestMethod]
	public void AuthorizationContext_AuthenticationScope_reflects_UserState() {
		var user = new AuthenticationScopeExposingUserState();
		user.StampScope(AuthenticationScope.Tenant);

		var authCtx = new AuthorizationContext<TestResource>(
			UserState: user,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: new TestResource());

		Assert.AreEqual(AuthenticationScope.Tenant, authCtx.AuthenticationScope);
	}

	[TestMethod]
	public void AuthorizationContext_AuthenticationScope_is_None_when_UserState_is_None() {
		var user = new TestUserState();

		var authCtx = new AuthorizationContext<TestResource>(
			UserState: user,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: new TestResource());

		Assert.AreEqual(AuthenticationScope.None, authCtx.AuthenticationScope);
	}

	private sealed class TestResource : IAuthorizableObject;

	/// <summary>
	/// Subclass that publicly exposes the protected SetAuthenticationScope setter
	/// for assertion purposes.
	/// </summary>
	private sealed class AuthenticationScopeExposingUserState : UserStateBase {
		public override bool IsAuthenticationComplete => true;
		public void StampScope(AuthenticationScope scope) => base.SetAuthenticationScope(scope);
	}

	/// <summary>
	/// Minimal IUserState implementation with no AuthenticationScope override — exercises
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
