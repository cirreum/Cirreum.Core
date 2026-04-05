namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using System.Security.Claims;
using Cirreum;
using Cirreum.Authorization;
using Cirreum.Security;

[TestClass]
public class OwnerScopeEvaluatorTests {

	private const string CallerTenantId = "tenant-A";
	private const string OtherTenantId = "tenant-B";

	// Non-applicable resource
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Non_owner_scoped_resource_passes_through() {
		var evaluator = new DefaultOwnerScopeEvaluator();
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
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand(),
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: false));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	[TestMethod]
	public async Task Loaded_non_owned_app_user_denies_with_UserDisabled() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand(),
			accessScope: AccessScope.Tenant,
			appUser: new PlainAppUser(isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.UserDisabled);
	}

	[TestMethod]
	public async Task Unloaded_app_user_passes_enabled_check_progressive_enrichment() {
		// Tenant scope with unloaded user → enabled check passes (progressive),
		// but ResolveTenantId returns null → TenantUnresolvable.
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand(),
			accessScope: AccessScope.Tenant,
			appUser: null);

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.TenantUnresolvable);
	}

	// AccessScope.None
	// -------------------------------------------------------------

	[TestMethod]
	public async Task None_scope_denies_with_AuthenticationRequired() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand(),
			accessScope: AccessScope.None,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.AuthenticationRequired);
	}

	// AccessScope.Global
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Global_write_without_OwnerId_denies_with_OwnerIdRequired() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand { OwnerId = null },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdRequired);
	}

	[TestMethod]
	public async Task Global_write_with_OwnerId_passes() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand { OwnerId = OtherTenantId },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Global_read_without_OwnerId_passes() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedQuery { OwnerId = null },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	// Cacheable owner-scoped reads — stricter OwnerId requirement for Global callers
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Global_cacheable_read_without_OwnerId_denies_with_CacheableReadOwnerIdRequired() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCacheableQuery { OwnerId = null },
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.CacheableReadOwnerIdRequired);
	}

	[TestMethod]
	public async Task Global_cacheable_read_with_OwnerId_passes_and_stamps_Global_scope() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery { OwnerId = OtherTenantId };
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
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery { OwnerId = CallerTenantId };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(AccessScope.Tenant, resource.CallerAccessScope);
	}

	[TestMethod]
	public async Task Tenant_cacheable_read_enriches_OwnerId_and_stamps_scope() {
		// Tenant callers can omit OwnerId (enriched) AND get CallerAccessScope stamped.
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(CallerTenantId, resource.OwnerId);
		Assert.AreEqual(AccessScope.Tenant, resource.CallerAccessScope);
	}

	[TestMethod]
	public async Task CallerAccessScope_is_not_stamped_on_denied_evaluation() {
		// A failing evaluation must not mutate the resource.
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery { OwnerId = null };
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
		// Leak 2 defense: Global-with-OwnerId="A" and Tenant-A must NOT share a cache bucket.
		var evaluator = new DefaultOwnerScopeEvaluator();

		var globalResource = new OwnedCacheableQuery { OwnerId = CallerTenantId };
		var globalCtx = BuildContext(
			resource: globalResource,
			accessScope: AccessScope.Global,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));
		var globalResult = await evaluator.EvaluateAsync(globalCtx);

		var tenantResource = new OwnedCacheableQuery { OwnerId = CallerTenantId };
		var tenantCtx = BuildContext(
			resource: tenantResource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));
		var tenantResult = await evaluator.EvaluateAsync(tenantCtx);

		Assert.IsTrue(globalResult.IsValid);
		Assert.IsTrue(tenantResult.IsValid);
		Assert.AreNotEqual(
			((ICacheableQuery<string>)globalResource).CacheKey,
			((ICacheableQuery<string>)tenantResource).CacheKey,
			"Global and Tenant cacheable reads of the same tenant's data must use distinct cache keys.");
	}

	[TestMethod]
	public async Task Composed_CacheKey_contains_owner_scope_and_scoped_key() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery { OwnerId = CallerTenantId };
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
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery {
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
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCacheableQuery { OwnerId = CallerTenantId };
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

	// AccessScope.Tenant
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Tenant_with_null_user_OwnerId_denies_with_TenantUnresolvable() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand { OwnerId = CallerTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(ownerId: null, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.TenantUnresolvable);
	}

	[TestMethod]
	public async Task Tenant_enriches_resource_OwnerId_from_caller_when_missing() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var resource = new OwnedCommand { OwnerId = null };
		var ctx = BuildContext(
			resource: resource,
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(CallerTenantId, resource.OwnerId);
	}

	[TestMethod]
	public async Task Tenant_with_matching_OwnerId_passes() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand { OwnerId = CallerTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Tenant_with_matching_OwnerId_is_case_insensitive() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand { OwnerId = "TENANT-a" },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		Assert.IsTrue(result.IsValid);
	}

	[TestMethod]
	public async Task Tenant_with_mismatched_OwnerId_denies_with_OwnerIdMismatch() {
		var evaluator = new DefaultOwnerScopeEvaluator();
		var ctx = BuildContext(
			resource: new OwnedCommand { OwnerId = OtherTenantId },
			accessScope: AccessScope.Tenant,
			appUser: new TestOwnedAppUser(CallerTenantId, isEnabled: true));

		var result = await evaluator.EvaluateAsync(ctx);

		AssertDeniedWith(result, DenyCodes.OwnerIdMismatch);
	}

	// Helpers
	// -------------------------------------------------------------

	private static AuthorizationContext<TResource> BuildContext<TResource>(
		TResource resource,
		AccessScope accessScope,
		IApplicationUser? appUser)
		where TResource : notnull, IAuthorizableResource {

		var userState = new OwnerScopeTestUserState(accessScope, appUser);
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

	private sealed class OwnedCommand : IAuthorizableOwnerScopedResource, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}

	private sealed class OwnedQuery : IAuthorizableOwnerScopedResource {
		public string? OwnerId { get; set; }
	}

	private sealed class OwnedCacheableQuery : IAuthorizableOwnerScopedCacheableQuery<string> {
		public string? OwnerId { get; set; }
		public AccessScope? CallerAccessScope { get; set; }
		public string ScopedCacheKey => "test-key";
		public string[]? ExtraTags { get; set; }
		public string[]? ScopedCacheTags => this.ExtraTags;
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

	private sealed class OwnerScopeTestUserState : UserStateBase {
		public override bool IsAuthenticationComplete => true;

		public OwnerScopeTestUserState(AccessScope scope, IApplicationUser? appUser) {
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
