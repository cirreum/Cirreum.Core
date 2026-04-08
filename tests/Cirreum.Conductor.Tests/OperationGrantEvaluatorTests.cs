namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum;
using Cirreum.Authorization;
using Cirreum.Authorization.Operations.Grants;
using Cirreum.Security;

[TestClass]
public class OperationGrantEvaluatorTests {

	private const string CallerTenantId = "tenant-A";
	private const string OtherTenantId = "tenant-B";

	// Non-applicable resource — no Granted interface
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Non_granted_resource_passes_through() {
		var evaluator = BuildEvaluator(OperationGrant.Denied);
		var ctx = BuildContext(
			authorizableObject: new NonOwnedResource(),
			authenticationScope: AuthenticationScope.None,
			appUser: null);

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// CheckApplicationUserEnabled — IOwnedApplicationUser invariant
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Loaded_disabled_user_denies_with_UserDisabled() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand(),
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: false));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	[TestMethod]
	public async Task Loaded_non_owned_app_user_denies_with_UserDisabled() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand(),
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new PlainAppUser(isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	// GrantDenied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Denied_grant_denies_with_GrantDenied() {
		var evaluator = BuildEvaluator(OperationGrant.Denied);
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = CallerTenantId },
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.GrantDenied);
	}

	// Command — OwnerId enforcement
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Command_with_OwnerId_in_grant_passes() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = CallerTenantId },
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Command_with_OwnerId_not_in_grant_denies_with_OwnerNotInReach() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = OtherTenantId },
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	[TestMethod]
	public async Task Global_command_without_OwnerId_denies_with_OwnerIdRequired() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = null },
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	[TestMethod]
	public async Task Command_enriches_OwnerId_from_single_element_grant() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(CallerTenantId, authorizableObject.OwnerId);
	}

	[TestMethod]
	public async Task Command_without_OwnerId_and_multi_element_grant_denies_with_OwnerAmbiguous() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId, OtherTenantId]));
		var authorizableObject =new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerAmbiguous);
	}

	[TestMethod]
	public async Task Command_with_unrestricted_grant_and_no_OwnerId_denies_with_OwnerIdRequired() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	// Read — OwnerId enforcement + Pattern C (deferred)
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Read_with_OwnerId_in_grant_passes() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = CallerTenantId },
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Read_with_OwnerId_not_in_grant_denies() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = OtherTenantId },
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	[TestMethod]
	public async Task Read_without_OwnerId_passes_deferred_Pattern_C() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = null },
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Global_read_without_OwnerId_passes_deferred_Pattern_C() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = null },
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// Cacheable Read — stricter OwnerId requirement for Global callers
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Global_cacheable_read_without_OwnerId_denies_with_CacheableReadOwnerIdRequired() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var ctx = BuildContext(
			authorizableObject: new GrantedCacheableRead { OwnerId = null },
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.CacheableReadOwnerIdRequired);
	}

	[TestMethod]
	public async Task Global_cacheable_read_with_OwnerId_passes_and_stamps_Global_scope() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedCacheableRead { OwnerId = OtherTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AuthenticationScope.Global, authorizableObject.CallerAuthenticationScope);
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_passes_and_stamps_Tenant_scope() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AuthenticationScope.Tenant, authorizableObject.CallerAuthenticationScope);
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_defers_null_OwnerId_and_stamps_scope() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AuthenticationScope.Tenant, authorizableObject.CallerAuthenticationScope);
	}

	[TestMethod]
	public async Task CallerAuthenticationScope_is_not_stamped_on_denied_evaluation() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedCacheableRead { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsFalse(result.IsValid);
		Assert.IsNull(authorizableObject.CallerAuthenticationScope);
	}

	[TestMethod]
	public async Task Same_OwnerId_under_Global_vs_Tenant_produces_distinct_CacheKeys() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);

		var globalResource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var globalCtx = BuildContext(
			authorizableObject: globalResource,
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));
		var globalResult = await evaluator.EvaluateAsync(globalCtx);

		var tenantEvaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var tenantResource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var tenantCtx = BuildContext(
			authorizableObject: tenantResource,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));
		var tenantResult = await tenantEvaluator.EvaluateAsync(tenantCtx);

		Assert.IsTrue(globalResult.IsValid);
		Assert.IsTrue(tenantResult.IsValid);
		Assert.AreNotEqual(
			((ICacheableQuery<string>)globalResource).CacheKey,
			((ICacheableQuery<string>)tenantResource).CacheKey,
			"Global and Tenant cacheable reads of the same tenant's data must use distinct cache keys.");
	}

	[TestMethod]
	public async Task Composed_CacheKey_contains_owner_scope_and_scoped_key() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var cacheKey = ((ICacheableQuery<string>)authorizableObject).CacheKey;
		StringAssert.Contains(cacheKey, $"owner:{CallerTenantId}");
		StringAssert.Contains(cacheKey, "scope:Tenant");
		StringAssert.Contains(cacheKey, authorizableObject.ScopedCacheKey);
	}

	[TestMethod]
	public async Task CacheTags_includes_automatic_tenant_tag_and_preserves_extras() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead {
			OwnerId = CallerTenantId,
			ExtraTags = ["dashboard", "section:sales"]
		};
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var tags = ((ICacheableQuery<string>)authorizableObject).CacheTags;
		Assert.IsNotNull(tags);
		CollectionAssert.Contains(tags, $"tenant:{CallerTenantId}");
		CollectionAssert.Contains(tags, "dashboard");
		CollectionAssert.Contains(tags, "section:sales");
	}

	[TestMethod]
	public async Task CacheTags_returns_only_tenant_tag_when_no_extras_supplied() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var tags = ((ICacheableQuery<string>)authorizableObject).CacheTags;
		Assert.IsNotNull(tags);
		Assert.HasCount(1, tags);
		Assert.AreEqual($"tenant:{CallerTenantId}", tags[0]);
	}

	// List — OwnerIds enforcement + stamping
	// -------------------------------------------------------------

	[TestMethod]
	public async Task List_with_null_OwnerIds_stamps_from_grant() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId, OtherTenantId]));
		var authorizableObject =new GrantedList { OwnerIds = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.IsNotNull(authorizableObject.OwnerIds);
		Assert.HasCount(2, authorizableObject.OwnerIds);
	}

	[TestMethod]
	public async Task List_with_null_OwnerIds_and_unrestricted_grant_stamps_null() {
		var evaluator = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedList { OwnerIds = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.IsNull(authorizableObject.OwnerIds);
	}

	[TestMethod]
	public async Task List_with_OwnerIds_subset_of_grant_passes() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId, OtherTenantId]));
		var authorizableObject =new GrantedList { OwnerIds = [CallerTenantId] };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task List_with_OwnerIds_not_subset_of_grant_denies() {
		var evaluator = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedList { OwnerIds = [CallerTenantId, OtherTenantId] };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationScope: AuthenticationScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	// Helpers
	// -------------------------------------------------------------

	private static OperationGrantEvaluator BuildEvaluator(OperationGrant grant) {
		var factory = new StubOperationGrantFactory(grant);
		var accessor = new DefaultOperationGrantAccessor();
		return new OperationGrantEvaluator(factory, accessor);
	}

	private static AuthorizationContext<TAuthorizableObject> BuildContext<TAuthorizableObject>(
		TAuthorizableObject authorizableObject,
		AuthenticationScope authenticationScope,
		IApplicationUser? appUser)
		where TAuthorizableObject : notnull, IAuthorizableObject {

		var userState = new OperationGrantEvaluatorTestUserState(authenticationScope, appUser);
		return new AuthorizationContext<TAuthorizableObject>(
			UserState: userState,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: authorizableObject);
	}

	private static void AssertDeniedWith(FluentValidation.Results.ValidationResult result, string expectedCode) {
		Assert.IsFalse(result.IsValid, "expected denied result");
		var code = result.Errors.FirstOrDefault()?.ErrorCode;
		Assert.AreEqual(expectedCode, code);
	}

	// Test doubles
	// -------------------------------------------------------------

	private sealed class NonOwnedResource : IAuthorizableObject;

	private sealed class GrantedCommand : IGrantMutateRequest {
		public string? OwnerId { get; set; }
	}

	private sealed class GrantedRead : IGrantableLookupBase, IAuthorizableObject {
		public string? OwnerId { get; set; }
	}

	private sealed class GrantedList : IGrantableSearchBase, IAuthorizableObject {
		public IReadOnlyList<string>? OwnerIds { get; set; }
	}

	private sealed class GrantedCacheableRead : IGrantableCacheableLookupBase, ICacheableQuery<string>, IAuthorizableObject {
		public string? OwnerId { get; set; }
		public AuthenticationScope? CallerAuthenticationScope { get; set; }
		public string ScopedCacheKey => "test-key";
		public string[]? ExtraTags { get; set; }
		public string[]? ScopedCacheTags => this.ExtraTags;

		string ICacheableQuery<string>.CacheKey =>
			$"owner:{this.OwnerId}:scope:{this.CallerAuthenticationScope}:{this.ScopedCacheKey}";

		string[]? ICacheableQuery<string>.CacheTags {
			get {
				var tenantTag = $"tenant:{this.OwnerId}";
				var extra = this.ScopedCacheTags;
				if (extra is null || extra.Length == 0) {
					return [tenantTag];
				}
				return [tenantTag, .. extra];
			}
		}
	}

	private sealed class StubOperationGrantFactory(OperationGrant grant) : IOperationGrantFactory {

		public ValueTask<OperationGrant> CreateAsync<TAuthorizableObject>(
			AuthorizationContext<TAuthorizableObject> context,
			CancellationToken cancellationToken)
			where TAuthorizableObject : IAuthorizableObject {
			return ValueTask.FromResult(grant);
		}
	}

	private sealed class TestOwnedAppUser(string? ownerId, bool isEnabled) : IOwnedApplicationUser {
		public string? OwnerId { get; } = ownerId;
		public bool IsEnabled { get; } = isEnabled;
		public IReadOnlyList<string> Roles => [];
	}

	private sealed class PlainAppUser(bool isEnabled) : IApplicationUser {
		public bool IsEnabled { get; } = isEnabled;
		public IReadOnlyList<string> Roles => [];
	}

	private sealed class OperationGrantEvaluatorTestUserState : UserStateBase {
		public override bool IsAuthenticationComplete => true;

		public OperationGrantEvaluatorTestUserState(AuthenticationScope scope, IApplicationUser? appUser) {
			this._principal = AnonymousUser.Shared;
			this._identity = (ClaimsIdentity)AnonymousUser.Shared.Identity!;
			this._profile = UserProfile.Anonymous;
			this._isAuthenticated = scope != AuthenticationScope.None;
			this._authenticationType = AuthenticationLibraryType.None;
			this.SetAuthenticationScope(scope);
			if (appUser is not null) {
				this.SetApplicationUser(appUser);
			}
		}
	}
}
