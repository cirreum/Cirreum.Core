namespace Cirreum.Conductor.Tests {

using System.Collections.Immutable;
using Cirreum.Authorization;
using Cirreum.Authorization.Grants;
using Cirreum.Authorization.Grants.Caching;
using Cirreum.Caching;
using Cirreum.Conductor.Tests.Domain.TestGrant;

[TestClass]
public class ReachCacheTests {

	private const string CallerId = "user-123";
	private const string TenantA = "tenant-A";

	// L1 — scoped memoization
	// -------------------------------------------------------------

	[TestMethod]
	public async Task L1_hit_resolves_grants_only_once_per_scope() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var resolver = BuildResolver(grantResolver);

		var ctx1 = BuildContext(new TestDeleteCmd());
		var ctx2 = BuildContext(new TestDeleteCmd());

		var reach1 = await resolver.ResolveAsync(ctx1, CancellationToken.None);
		var reach2 = await resolver.ResolveAsync(ctx2, CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount, "ResolveGrantsAsync should be called once (L1 hit on second call)");
		Assert.IsFalse(reach1.IsDenied);
		Assert.IsFalse(reach2.IsDenied);
	}

	[TestMethod]
	public async Task L1_miss_when_different_permissions() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var resolver = BuildResolver(grantResolver);

		var ctxDelete = BuildContext(new TestDeleteCmd());
		var ctxRead = BuildContext(new TestReadCmd());

		await resolver.ResolveAsync(ctxDelete, CancellationToken.None);
		await resolver.ResolveAsync(ctxRead, CancellationToken.None);

		Assert.AreEqual(2, grantResolver.ResolveCount, "Different permissions should cause L1 miss");
	}

	// L2 — cross-request cache
	// -------------------------------------------------------------

	[TestMethod]
	public async Task L2_hit_resolves_grants_once_across_scopes() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();
		var settings = new GrantCacheSettings();

		// Scope 1
		var resolver1 = BuildResolver(grantResolver, cacheService, settings);
		var ctx1 = BuildContext(new TestDeleteCmd());
		await resolver1.ResolveAsync(ctx1, CancellationToken.None);

		// Scope 2 — new resolver instance, same L2 cache
		var resolver2 = BuildResolver(grantResolver, cacheService, settings);
		var ctx2 = BuildContext(new TestDeleteCmd());
		await resolver2.ResolveAsync(ctx2, CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount, "L2 should serve second scope from cache");
	}

	// Permission-based dedup — same permissions share L2 entry
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Same_permissions_on_different_types_share_L2_entry() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();
		var settings = new GrantCacheSettings();

		var resolver = BuildResolver(grantResolver, cacheService, settings);

		// Both TestDeleteCmd and TestDeleteCmd2 require [RequiresPermission("delete")]
		var ctx1 = BuildContext(new TestDeleteCmd());
		var ctx2 = BuildContext(new TestDeleteCmd2());

		await resolver.ResolveAsync(ctx1, CancellationToken.None);
		await resolver.ResolveAsync(ctx2, CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount,
			"Two resource types with same permissions should share a cache entry");
	}

	// Cache disabled (NoCacheService) — resolves every time
	// -------------------------------------------------------------

	[TestMethod]
	public async Task NoCacheService_resolves_every_time() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var noCacheService = new NoCacheService();

		// Scope 1
		var resolver1 = BuildResolver(grantResolver, noCacheService);
		await resolver1.ResolveAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		// Scope 2 — fresh resolver, NoCacheService passes through
		var resolver2 = BuildResolver(grantResolver, noCacheService);
		await resolver2.ResolveAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		Assert.AreEqual(2, grantResolver.ResolveCount);
	}

	// Domain override expiration
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Domain_override_expiration_is_respected() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();
		var settings = new GrantCacheSettings {
			Expiration = TimeSpan.FromMinutes(5),
			DomainOverrides = new Dictionary<string, GrantCacheDomainOverride> {
				["testgrant"] = new() { Expiration = TimeSpan.FromMinutes(10) }
			}
		};

		// Scope 1 — populates L2
		var resolver1 = BuildResolver(grantResolver, cacheService, settings);
		await resolver1.ResolveAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		// Scope 2 — should hit L2 cache
		var resolver2 = BuildResolver(grantResolver, cacheService, settings);
		await resolver2.ResolveAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount, "Domain override expiration should not prevent L2 hit");
	}

	// Version bump causes miss
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Version_bump_causes_L2_miss() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();

		// Scope 1 with version 1
		var settings1 = new GrantCacheSettings { Version = 1 };
		var resolver1 = BuildResolver(grantResolver, cacheService, settings1);
		await resolver1.ResolveAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		// Scope 2 with version 2 — different key, L2 miss
		var settings2 = new GrantCacheSettings { Version = 2 };
		var resolver2 = BuildResolver(grantResolver, cacheService, settings2);
		await resolver2.ResolveAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		Assert.AreEqual(2, grantResolver.ResolveCount, "Version bump should cause L2 miss");
	}

	// Bypass always live — never cached
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Bypass_always_runs_live() {
		var grantResolver = new CountingGrantResolver([TenantA]) { AlwaysBypass = true };
		var resolver = BuildResolver(grantResolver);

		var ctx1 = BuildContext(new TestDeleteCmd());
		var ctx2 = BuildContext(new TestDeleteCmd());

		var reach1 = await resolver.ResolveAsync(ctx1, CancellationToken.None);
		var reach2 = await resolver.ResolveAsync(ctx2, CancellationToken.None);

		Assert.IsTrue(reach1.IsUnrestricted);
		Assert.IsTrue(reach2.IsUnrestricted);
		Assert.AreEqual(2, grantResolver.BypassCount, "ShouldBypassAsync should run every time");
		Assert.AreEqual(0, grantResolver.ResolveCount, "ResolveGrantsAsync should never be called when bypassing");
	}

	// Unauthenticated → Denied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Unauthenticated_caller_returns_denied() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var resolver = BuildResolver(grantResolver);

		var ctx = BuildUnauthenticatedContext(new TestDeleteCmd());

		var reach = await resolver.ResolveAsync(ctx, CancellationToken.None);

		Assert.IsTrue(reach.IsDenied);
		Assert.AreEqual(0, grantResolver.ResolveCount);
	}

	// Empty grants → Denied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Empty_grants_returns_denied() {
		var grantResolver = new CountingGrantResolver([]);
		var resolver = BuildResolver(grantResolver);

		var ctx = BuildContext(new TestDeleteCmd());

		var reach = await resolver.ResolveAsync(ctx, CancellationToken.None);

		Assert.IsTrue(reach.IsDenied);
	}

	// Home owner merge
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Home_owner_is_merged_into_grants() {
		var grantResolver = new CountingGrantResolver(["other-tenant"]) {
			HomeOwnerId = TenantA
		};
		var resolver = BuildResolver(grantResolver);

		var ctx = BuildContext(new TestDeleteCmd());
		var reach = await resolver.ResolveAsync(ctx, CancellationToken.None);

		Assert.IsFalse(reach.IsDenied);
		Assert.IsFalse(reach.IsUnrestricted);
		Assert.IsNotNull(reach.OwnerIds);
		CollectionAssert.Contains(reach.OwnerIds.ToList(), TenantA);
		CollectionAssert.Contains(reach.OwnerIds.ToList(), "other-tenant");
	}

	// Helpers
	// -------------------------------------------------------------

	private static GrantBasedAccessReachResolver BuildResolver(
		CountingGrantResolver grantResolver,
		ICacheService? cacheService = null,
		GrantCacheSettings? cacheSettings = null,
		CacheSettings? rootCacheSettings = null) {

		return new GrantBasedAccessReachResolver(
			grantResolver,
			cacheService ?? new InMemoryTestCacheService(),
			rootCacheSettings ?? new CacheSettings(),
			cacheSettings ?? new GrantCacheSettings());
	}

	private static AuthorizationContext<TResource> BuildContext<TResource>(TResource resource)
		where TResource : IAuthorizableResource {

		var userState = TestUserState.CreateAuthenticated(id: CallerId);
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

	private static AuthorizationContext<TResource> BuildUnauthenticatedContext<TResource>(TResource resource)
		where TResource : IAuthorizableResource {

		var userState = new TestUserState();
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

	/// <summary>
	/// Simple in-memory cache service for testing L2 behavior.
	/// </summary>
	private sealed class InMemoryTestCacheService : ICacheService {

		private readonly Dictionary<string, object> _store = [];
		private readonly Dictionary<string, HashSet<string>> _tagIndex = [];

		public async ValueTask<TResponse> GetOrCreateAsync<TResponse>(
			string cacheKey,
			Func<CancellationToken, ValueTask<TResponse>> factory,
			CacheExpirationSettings settings,
			string[]? tags = null,
			CancellationToken cancellationToken = default) {

			if (this._store.TryGetValue(cacheKey, out var cached)) {
				return (TResponse)cached;
			}

			var result = await factory(cancellationToken).ConfigureAwait(false);
			this._store[cacheKey] = result!;

			if (tags is not null) {
				foreach (var tag in tags) {
					if (!this._tagIndex.TryGetValue(tag, out var keys)) {
						keys = [];
						this._tagIndex[tag] = keys;
					}
					keys.Add(cacheKey);
				}
			}

			return result;
		}

		public ValueTask RemoveAsync(string cacheKey, CancellationToken cancellationToken = default) {
			this._store.Remove(cacheKey);
			return ValueTask.CompletedTask;
		}

		public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) {
			if (this._tagIndex.TryGetValue(tag, out var keys)) {
				foreach (var key in keys) {
					this._store.Remove(key);
				}
				this._tagIndex.Remove(tag);
			}
			return ValueTask.CompletedTask;
		}

		public ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default) {
			foreach (var tag in tags) {
				this.RemoveByTagAsync(tag, cancellationToken);
			}
			return ValueTask.CompletedTask;
		}
	}
}
}

// ─────────────────────────────────────────────────────────────────
// Test doubles in a *.Domain.TestGrant.* namespace for DomainFeatureResolver
// ─────────────────────────────────────────────────────────────────

namespace Cirreum.Conductor.Tests.Domain.TestGrant {

	using Cirreum.Authorization;
	using Cirreum.Authorization.Grants;
	using Cirreum.Conductor;

	[RequiresPermission("delete")]
	internal sealed class TestDeleteCmd : IGrantMutateRequest {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("delete")]
	internal sealed class TestDeleteCmd2 : IGrantMutateRequest {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("read")]
	internal sealed class TestReadCmd : IGrantMutateRequest {
		public string? OwnerId { get; set; }
	}

	internal sealed class CountingGrantResolver(IReadOnlyList<string> ownerIds)
		: IGrantResolver {

		public int ResolveCount;
		public int BypassCount;
		public bool AlwaysBypass;
		public string? HomeOwnerId;

		public ValueTask<GrantedReach> ResolveGrantsAsync<TResource>(
			AuthorizationContext<TResource> context,
			CancellationToken cancellationToken)
			where TResource : IAuthorizableResource {

			Interlocked.Increment(ref this.ResolveCount);
			return ValueTask.FromResult(new GrantedReach(ownerIds));
		}

		public ValueTask<bool> ShouldBypassAsync<TResource>(
			AuthorizationContext<TResource> context,
			CancellationToken cancellationToken)
			where TResource : IAuthorizableResource {

			Interlocked.Increment(ref this.BypassCount);
			return ValueTask.FromResult(this.AlwaysBypass);
		}

		public ValueTask<string?> ResolveHomeOwnerAsync<TResource>(
			AuthorizationContext<TResource> context,
			CancellationToken cancellationToken)
			where TResource : IAuthorizableResource {

			return ValueTask.FromResult(this.HomeOwnerId);
		}
	}
}
