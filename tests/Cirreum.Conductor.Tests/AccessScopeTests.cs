namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum.Authorization;
using Cirreum.Security;

[TestClass]
public class AccessScopeTests {

	// IUserState default interface member
	// -------------------------------------------------------------

	[TestMethod]
	public void IUserState_default_AccessScope_is_None_when_not_overridden() {
		IUserState user = new MinimalUserState();
		Assert.AreEqual(AccessScope.None, user.AccessScope);
	}

	[TestMethod]
	public void IUserState_default_IsAccessScopeResolved_is_false() {
		IUserState user = new MinimalUserState();
		Assert.IsFalse(user.IsAccessScopeResolved);
	}

	// UserStateBase backing field + setter
	// -------------------------------------------------------------

	[TestMethod]
	public void UserStateBase_AccessScope_defaults_to_None() {
		var user = new TestUserState();
		Assert.AreEqual(AccessScope.None, user.AccessScope);
	}

	[TestMethod]
	public void UserStateBase_IsAccessScopeResolved_defaults_to_false() {
		var user = new TestUserState();
		Assert.IsFalse(user.IsAccessScopeResolved);
	}

	[TestMethod]
	public void UserStateBase_SetAccessScope_persists_value() {
		var user = new AccessScopeExposingUserState();
		user.StampScope(AccessScope.Global);
		Assert.AreEqual(AccessScope.Global, user.AccessScope);

		user.StampScope(AccessScope.Tenant);
		Assert.AreEqual(AccessScope.Tenant, user.AccessScope);
	}

	[TestMethod]
	public void UserStateBase_SetAccessScope_marks_resolved() {
		var user = new AccessScopeExposingUserState();
		Assert.IsFalse(user.IsAccessScopeResolved);

		user.StampScope(AccessScope.Global);
		Assert.IsTrue(user.IsAccessScopeResolved);
	}

	[TestMethod]
	public void UserStateBase_SetAccessScope_to_None_still_marks_resolved() {
		var user = new AccessScopeExposingUserState();
		user.StampScope(AccessScope.None);

		Assert.AreEqual(AccessScope.None, user.AccessScope);
		Assert.IsTrue(user.IsAccessScopeResolved, "Explicitly setting None should still mark as resolved");
	}

	// OperationContext passthrough
	// -------------------------------------------------------------

	[TestMethod]
	public void OperationContext_AccessScope_reflects_UserState() {
		var user = new AccessScopeExposingUserState();
		user.StampScope(AccessScope.Global);

		var ctx = CreateOperationContext(user);

		Assert.AreEqual(AccessScope.Global, ctx.AccessScope);
	}

	[TestMethod]
	public void OperationContext_AccessScope_is_None_when_UserState_is_None() {
		var user = new TestUserState();
		var ctx = CreateOperationContext(user);
		Assert.AreEqual(AccessScope.None, ctx.AccessScope);
	}

	// AuthorizationContext<T> passthrough
	// -------------------------------------------------------------

	[TestMethod]
	public void AuthorizationContext_AccessScope_reflects_Operation() {
		var user = new AccessScopeExposingUserState();
		user.StampScope(AccessScope.Tenant);
		var opCtx = CreateOperationContext(user);

		var authCtx = new AuthorizationContext<TestResource>(
			Operation: opCtx,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			Resource: new TestResource());

		Assert.AreEqual(AccessScope.Tenant, authCtx.AccessScope);
	}

	// Helpers
	// -------------------------------------------------------------

	private static OperationContext CreateOperationContext(IUserState userState) =>
		new(
			Environment: "Test",
			RuntimeType: DomainRuntimeType.UnitTest,
			Timestamp: DateTimeOffset.UtcNow,
			StartTimestamp: System.Diagnostics.Stopwatch.GetTimestamp(),
			UserState: userState,
			OperationId: Guid.NewGuid().ToString(),
			CorrelationId: Guid.NewGuid().ToString());

	private sealed class TestResource : IAuthorizableObject;

	/// <summary>
	/// Subclass that publicly exposes the protected SetAccessScope setter
	/// for assertion purposes.
	/// </summary>
	private sealed class AccessScopeExposingUserState : UserStateBase {
		public override bool IsAuthenticationComplete => true;
		public void StampScope(AccessScope scope) => base.SetAccessScope(scope);
	}

	/// <summary>
	/// Minimal IUserState implementation with no AccessScope override — exercises
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
