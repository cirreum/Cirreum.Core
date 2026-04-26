namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum;
using Cirreum.Authorization;
using Cirreum.Authorization.Operations;
using Cirreum.Authorization.Operations.Grants;
using Cirreum.Caching;
using Cirreum.Conductor;
using Cirreum.Security;

[TestClass]
public class OperationGrantEvaluatorTests {

	private const string CallerTenantId = "tenant-A";
	private const string OtherTenantId = "tenant-B";

	// Non-applicable resource — no Granted interface
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Non_granted_resource_passes_through() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Denied);
		var ctx = BuildContext(
			authorizableObject: new NonOwnedResource(),
			authenticationBoundary: AuthenticationBoundary.None,
			appUser: null);

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// CheckApplicationUserEnabled — IOwnedApplicationUser invariant
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Loaded_disabled_user_denies_with_UserDisabled() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand(),
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: false));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	[TestMethod]
	public async Task Loaded_non_owned_app_user_denies_with_UserDisabled() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand(),
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new PlainAppUser(isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	// GrantDenied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Denied_grant_denies_with_GrantDenied() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Denied);
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = CallerTenantId },
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.GrantDenied);
	}

	// Command — OwnerId enforcement
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Command_with_OwnerId_in_grant_passes() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = CallerTenantId },
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Command_with_OwnerId_not_in_grant_denies_with_OwnerNotInReach() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = OtherTenantId },
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	[TestMethod]
	public async Task Global_command_without_OwnerId_denies_with_OwnerIdRequired() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var ctx = BuildContext(
			authorizableObject: new GrantedCommand { OwnerId = null },
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	[TestMethod]
	public async Task Command_enriches_OwnerId_from_single_element_grant() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(CallerTenantId, authorizableObject.OwnerId);
	}

	[TestMethod]
	public async Task Command_without_OwnerId_and_multi_element_grant_denies_with_OwnerAmbiguous() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId, OtherTenantId]));
		var authorizableObject =new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerAmbiguous);
	}

	[TestMethod]
	public async Task Command_with_unrestricted_grant_and_no_OwnerId_denies_with_OwnerIdRequired() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	// Read — OwnerId enforcement + Pattern C (deferred)
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Read_with_OwnerId_in_grant_passes() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = CallerTenantId },
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Read_with_OwnerId_not_in_grant_denies() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = OtherTenantId },
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	[TestMethod]
	public async Task Read_without_OwnerId_passes_deferred_Pattern_C() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = null },
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Global_read_without_OwnerId_passes_deferred_Pattern_C() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var ctx = BuildContext(
			authorizableObject: new GrantedRead { OwnerId = null },
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// Cacheable Read — stricter OwnerId requirement for Global callers
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Global_cacheable_read_without_OwnerId_denies_with_CacheableReadOwnerIdRequired() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var ctx = BuildContext(
			authorizableObject: new GrantedCacheableRead { OwnerId = null },
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.CacheableReadOwnerIdRequired);
	}

	[TestMethod]
	public async Task Global_cacheable_read_with_OwnerId_passes_and_stamps_Global_scope() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedCacheableRead { OwnerId = OtherTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		StringAssert.Contains(cacheKeyContext.KeyPrefix, "boundary:Global");
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_passes_and_stamps_Tenant_scope() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		StringAssert.Contains(cacheKeyContext.KeyPrefix, "boundary:Tenant");
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_defers_null_OwnerId_and_stamps_scope() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		StringAssert.Contains(cacheKeyContext.KeyPrefix, "boundary:Tenant");
	}

	[TestMethod]
	public async Task CacheKeyContext_is_not_stamped_on_denied_evaluation() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedCacheableRead { OwnerId = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsFalse(result.IsValid);
		Assert.IsNull(cacheKeyContext.KeyPrefix);
	}

	[TestMethod]
	public async Task Same_OwnerId_under_Global_vs_Tenant_produces_distinct_CacheKeyPrefixes() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);

		var globalResource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var globalCtx = BuildContext(
			authorizableObject: globalResource,
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));
		var globalResult = await evaluator.EvaluateAsync(globalCtx);
		var globalPrefix = cacheKeyContext.KeyPrefix;

		var (tenantEvaluator, tenantCacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var tenantResource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var tenantCtx = BuildContext(
			authorizableObject: tenantResource,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));
		var tenantResult = await tenantEvaluator.EvaluateAsync(tenantCtx);
		var tenantPrefix = tenantCacheKeyContext.KeyPrefix;

		Assert.IsTrue(globalResult.IsValid);
		Assert.IsTrue(tenantResult.IsValid);
		Assert.AreNotEqual(globalPrefix, tenantPrefix,
			"Global and Tenant cacheable reads of the same tenant's data must use distinct cache key prefixes.");
	}

	[TestMethod]
	public async Task CacheKeyContext_prefix_contains_owner_and_boundary() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var prefix = cacheKeyContext.KeyPrefix;
		Assert.IsNotNull(prefix);
		StringAssert.Contains(prefix, $"owner:{CallerTenantId}");
		StringAssert.Contains(prefix, "boundary:Tenant");
	}

	[TestMethod]
	public async Task CacheKeyContext_ExtraTags_includes_tenant_tag() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var extraTags = cacheKeyContext.ExtraTags;
		Assert.IsNotNull(extraTags);
		CollectionAssert.Contains(extraTags, $"owner:{CallerTenantId}");
	}

	[TestMethod]
	public async Task CacheKeyContext_ExtraTags_contains_only_tenant_tag() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var extraTags = cacheKeyContext.ExtraTags;
		Assert.IsNotNull(extraTags);
		Assert.HasCount(1, extraTags);
		Assert.AreEqual($"owner:{CallerTenantId}", extraTags[0]);
	}

	// List — OwnerIds enforcement + stamping
	// -------------------------------------------------------------

	[TestMethod]
	public async Task List_with_null_OwnerIds_stamps_from_grant() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId, OtherTenantId]));
		var authorizableObject =new GrantedList { OwnerIds = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.IsNotNull(authorizableObject.OwnerIds);
		Assert.HasCount(2, authorizableObject.OwnerIds);
	}

	[TestMethod]
	public async Task List_with_null_OwnerIds_and_unrestricted_grant_stamps_null() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.Unrestricted);
		var authorizableObject =new GrantedList { OwnerIds = null };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.IsNull(authorizableObject.OwnerIds);
	}

	[TestMethod]
	public async Task List_with_OwnerIds_subset_of_grant_passes() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId, OtherTenantId]));
		var authorizableObject =new GrantedList { OwnerIds = [CallerTenantId] };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task List_with_OwnerIds_not_subset_of_grant_denies() {
		var (evaluator, cacheKeyContext) = BuildEvaluator(OperationGrant.ForOwners([CallerTenantId]));
		var authorizableObject =new GrantedList { OwnerIds = [CallerTenantId, OtherTenantId] };
		var ctx = BuildContext(
			authorizableObject: authorizableObject,
			authenticationBoundary: AuthenticationBoundary.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	// Helpers
	// -------------------------------------------------------------

	private static (OperationGrantEvaluator evaluator, CacheKeyContext cacheKeyContext) BuildEvaluator(OperationGrant grant) {
		var factory = new StubOperationGrantFactory(grant);
		var accessor = new DefaultOperationGrantAccessor();
		var cacheKeyContext = new CacheKeyContext();
		var evaluator = new OperationGrantEvaluator(factory, accessor, cacheKeyContext);
		return (evaluator, cacheKeyContext);
	}

	private static AuthorizationContext<TAuthorizableObject> BuildContext<TAuthorizableObject>(
		TAuthorizableObject authorizableObject,
		AuthenticationBoundary authenticationBoundary,
		IApplicationUser? appUser)
		where TAuthorizableObject : notnull, IAuthorizableObject {

		var userState = new OperationGrantEvaluatorTestUserState(authenticationBoundary, appUser);
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

	private sealed class GrantedCommand : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	private sealed class GrantedRead : IGrantableLookupBase, IAuthorizableObject {
		public string? OwnerId { get; set; }
	}

	private sealed class GrantedList : IGrantableSearchBase, IAuthorizableObject {
		public IReadOnlyList<string>? OwnerIds { get; set; }
	}

	private sealed class GrantedCacheableRead : IGrantableLookupBase, ICacheableOperation<string>, IAuthorizableObject {
		public string? OwnerId { get; set; }
		public string CacheKey => "test-key";
		public string[]? ExtraTags { get; set; }
		public string[]? CacheTags => this.ExtraTags;
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

		public OperationGrantEvaluatorTestUserState(AuthenticationBoundary scope, IApplicationUser? appUser) {
			this._principal = AnonymousUser.Shared;
			this._identity = (ClaimsIdentity)AnonymousUser.Shared.Identity!;
			this._profile = UserProfile.Anonymous;
			this._isAuthenticated = scope != AuthenticationBoundary.None;
			this._authenticationType = AuthenticationLibraryType.None;
			this.SetAuthenticationBoundary(scope);
			if (appUser is not null) {
				this.SetApplicationUser(appUser);
			}
		}
	}
}
