namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum;
using Cirreum.Authorization;
using Cirreum.Authorization.Grants;
using Cirreum.Security;

[TestClass]
public class GrantEvaluatorTests {

	private const string CallerTenantId = "tenant-A";
	private const string OtherTenantId = "tenant-B";

	// Non-applicable resource — no Granted interface
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Non_granted_resource_passes_through() {
		var evaluator = BuildEvaluator(AccessReach.Denied);
		var ctx = BuildContext(
			resource: new NonOwnedResource(),
			accessScope: AccessScope.None,
			appUser: null);

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// CheckApplicationUserEnabled — IOwnedApplicationUser invariant
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Loaded_disabled_user_denies_with_UserDisabled() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedCommand(),
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: false));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	[TestMethod]
	public async Task Loaded_non_owned_app_user_denies_with_UserDisabled() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedCommand(),
			accessScope: AccessScope.Tenant,
			appUser: new PlainAppUser(isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	[TestMethod]
	public async Task Unloaded_app_user_passes_enabled_check_but_no_resolver_denies() {
		// Unloaded user → enabled check passes (progressive enrichment),
		// but no resolver registered → ScopeNotPermitted.
		var evaluator = BuildEvaluator(reach: null, registerResolver: false);
		var ctx = BuildContext(
			resource: new GrantedCommand(),
			accessScope: AccessScope.Tenant,
			appUser: null);

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.ScopeNotPermitted);
	}

	// ReachDenied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Denied_reach_denies_with_ReachDenied() {
		var evaluator = BuildEvaluator(AccessReach.Denied);
		var ctx = BuildContext(
			resource: new GrantedCommand { OwnerId = CallerTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.ReachDenied);
	}

	// Command — OwnerId enforcement
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Command_with_OwnerId_in_reach_passes() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedCommand { OwnerId = CallerTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Command_with_OwnerId_not_in_reach_denies_with_OwnerNotInReach() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedCommand { OwnerId = OtherTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	[TestMethod]
	public async Task Global_command_without_OwnerId_denies_with_OwnerIdRequired() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var ctx = BuildContext(
			resource: new GrantedCommand { OwnerId = null },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	[TestMethod]
	public async Task Command_enriches_OwnerId_from_single_element_reach() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(CallerTenantId, resource.OwnerId);
	}

	[TestMethod]
	public async Task Command_without_OwnerId_and_multi_element_reach_denies_with_OwnerAmbiguous() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId, OtherTenantId]));
		var resource = new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerAmbiguous);
	}

	[TestMethod]
	public async Task Command_with_unrestricted_reach_and_no_OwnerId_denies_with_OwnerIdRequired() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var resource = new GrantedCommand { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	// Read — OwnerId enforcement + Pattern C (deferred)
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Read_with_OwnerId_in_reach_passes() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedRead { OwnerId = CallerTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Read_with_OwnerId_not_in_reach_denies() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedRead { OwnerId = OtherTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	[TestMethod]
	public async Task Read_without_OwnerId_passes_deferred_Pattern_C() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var ctx = BuildContext(
			resource: new GrantedRead { OwnerId = null },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Global_read_without_OwnerId_passes_deferred_Pattern_C() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var ctx = BuildContext(
			resource: new GrantedRead { OwnerId = null },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// Cacheable Read — stricter OwnerId requirement for Global callers
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Global_cacheable_read_without_OwnerId_denies_with_CacheableReadOwnerIdRequired() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var ctx = BuildContext(
			resource: new GrantedCacheableRead { OwnerId = null },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.CacheableReadOwnerIdRequired);
	}

	[TestMethod]
	public async Task Global_cacheable_read_with_OwnerId_passes_and_stamps_Global_scope() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var resource = new GrantedCacheableRead { OwnerId = OtherTenantId };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AccessScope.Global, resource.CallerAccessScope);
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_passes_and_stamps_Tenant_scope() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AccessScope.Tenant, resource.CallerAccessScope);
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_defers_null_OwnerId_and_stamps_scope() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedCacheableRead { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AccessScope.Tenant, resource.CallerAccessScope);
	}

	[TestMethod]
	public async Task CallerAccessScope_is_not_stamped_on_denied_evaluation() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var resource = new GrantedCacheableRead { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsFalse(result.IsValid);
		Assert.IsNull(resource.CallerAccessScope);
	}

	[TestMethod]
	public async Task Same_OwnerId_under_Global_vs_Tenant_produces_distinct_CacheKeys() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);

		var globalResource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var globalCtx = BuildContext(
			resource: globalResource,
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));
		var globalResult = await evaluator.EvaluateAsync(globalCtx);

		var tenantEvaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var tenantResource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var tenantCtx = BuildContext(
			resource: tenantResource,
			accessScope: AccessScope.Tenant,
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
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var cacheKey = ((ICacheableQuery<string>)resource).CacheKey;
		StringAssert.Contains(cacheKey, $"owner:{CallerTenantId}");
		StringAssert.Contains(cacheKey, "scope:Tenant");
		StringAssert.Contains(cacheKey, resource.ScopedCacheKey);
	}

	[TestMethod]
	public async Task CacheTags_includes_automatic_tenant_tag_and_preserves_extras() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedCacheableRead {
			OwnerId = CallerTenantId,
			ExtraTags = ["dashboard", "section:sales"]
		};
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var tags = ((ICacheableQuery<string>)resource).CacheTags;
		Assert.IsNotNull(tags);
		CollectionAssert.Contains(tags, $"tenant:{CallerTenantId}");
		CollectionAssert.Contains(tags, "dashboard");
		CollectionAssert.Contains(tags, "section:sales");
	}

	[TestMethod]
	public async Task CacheTags_returns_only_tenant_tag_when_no_extras_supplied() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedCacheableRead { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);
		Assert.IsTrue(result.IsValid);

		var tags = ((ICacheableQuery<string>)resource).CacheTags;
		Assert.IsNotNull(tags);
		Assert.HasCount(1, tags);
		Assert.AreEqual($"tenant:{CallerTenantId}", tags[0]);
	}

	// List — OwnerIds enforcement + stamping
	// -------------------------------------------------------------

	[TestMethod]
	public async Task List_with_null_OwnerIds_stamps_from_reach() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId, OtherTenantId]));
		var resource = new GrantedList { OwnerIds = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.IsNotNull(resource.OwnerIds);
		Assert.HasCount(2, resource.OwnerIds);
	}

	[TestMethod]
	public async Task List_with_null_OwnerIds_and_unrestricted_reach_stamps_null() {
		var evaluator = BuildEvaluator(AccessReach.Unrestricted);
		var resource = new GrantedList { OwnerIds = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.IsNull(resource.OwnerIds);
	}

	[TestMethod]
	public async Task List_with_OwnerIds_subset_of_reach_passes() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId, OtherTenantId]));
		var resource = new GrantedList { OwnerIds = [CallerTenantId] };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task List_with_OwnerIds_not_subset_of_reach_denies() {
		var evaluator = BuildEvaluator(AccessReach.ForOwners([CallerTenantId]));
		var resource = new GrantedList { OwnerIds = [CallerTenantId, OtherTenantId] };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerNotInReach);
	}

	// No resolver registered — misconfiguration
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Granted_resource_without_resolver_denies_with_ScopeNotPermitted() {
		var evaluator = BuildEvaluator(reach: null, registerResolver: false);
		var ctx = BuildContext(
			resource: new GrantedCommand { OwnerId = CallerTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.ScopeNotPermitted);
	}

	// Helpers
	// -------------------------------------------------------------

	private static GrantEvaluator BuildEvaluator(
		AccessReach? reach,
		bool registerResolver = true) {

		IAccessReachResolver[] resolvers = registerResolver && reach is not null
			? [new StubAccessReachResolver(reach)]
			: [];

		var selector = new AccessReachResolverSelector(resolvers);
		var accessor = new DefaultAccessReachAccessor();
		return new GrantEvaluator(selector, accessor);
	}

	private static GrantEvaluator BuildEvaluator(AccessReach reach) =>
		BuildEvaluator(reach, registerResolver: true);

	private static AuthorizationContext<TResource> BuildContext<TResource>(
		TResource resource,
		AccessScope accessScope,
		IApplicationUser? appUser)
		where TResource : notnull, IAuthorizableResource {

		var userState = new GrantEvaluatorTestUserState(accessScope, appUser);
		var op = new OperationContext(
			Environment: "Test",
			RuntimeType: DomainRuntimeType.UnitTest,
			Timestamp: DateTimeOffset.UtcNow,
			StartTimestamp: System.Diagnostics.Stopwatch.GetTimestamp(),
			UserState: userState,
			OperationId: Guid.NewGuid().ToString(),
			CorrelationId: Guid.NewGuid().ToString());

		return new AuthorizationContext<TResource>(
			Operation: op,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			Resource: resource);
	}

	private static void AssertDeniedWith(FluentValidation.Results.ValidationResult result, string expectedCode) {
		Assert.IsFalse(result.IsValid, "expected denied result");
		var code = result.Errors.FirstOrDefault()?.ErrorCode;
		Assert.AreEqual(expectedCode, code);
	}

	// Test doubles
	// -------------------------------------------------------------

	private sealed class NonOwnedResource : IAuthorizableResource;

	private sealed class GrantedCommand : IGrantMutateRequest {
		public string? OwnerId { get; set; }
	}

	private sealed class GrantedRead : IGrantableLookupBase, IAuthorizableResource {
		public string? OwnerId { get; set; }
	}

	private sealed class GrantedList : IGrantableSearchBase, IAuthorizableResource {
		public IReadOnlyList<string>? OwnerIds { get; set; }
	}

	private sealed class GrantedCacheableRead : IGrantableCacheableLookupBase, ICacheableQuery<string>, IAuthorizableResource {
		public string? OwnerId { get; set; }
		public AccessScope? CallerAccessScope { get; set; }
		public string ScopedCacheKey => "test-key";
		public string[]? ExtraTags { get; set; }
		public string[]? ScopedCacheTags => this.ExtraTags;

		string ICacheableQuery<string>.CacheKey =>
			$"owner:{this.OwnerId}:scope:{this.CallerAccessScope}:{this.ScopedCacheKey}";

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

	private sealed class StubAccessReachResolver(AccessReach reach) : IAccessReachResolver {
		public bool Handles(Type resourceType) => true;

		public ValueTask<AccessReach> ResolveAsync<TResource>(
			AuthorizationContext<TResource> context,
			CancellationToken cancellationToken)
			where TResource : IAuthorizableResource {
			return ValueTask.FromResult(reach);
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

	private sealed class GrantEvaluatorTestUserState : UserStateBase {
		public override bool IsAuthenticationComplete => true;

		public GrantEvaluatorTestUserState(AccessScope scope, IApplicationUser? appUser) {
			this._principal = AnonymousUser.Shared;
			this._identity = (ClaimsIdentity)AnonymousUser.Shared.Identity!;
			this._profile = UserProfile.Anonymous;
			this._isAuthenticated = scope != AccessScope.None;
			this._authenticationType = AuthenticationLibraryType.None;
			this.SetAccessScope(scope);
			if (appUser is not null) {
				this.SetApplicationUser(appUser);
			}
		}
	}
}
