namespace Cirreum.Conductor.Tests {

using System.Collections.Immutable;
using Cirreum.Authorization;
using Cirreum.Authorization.Operations.Grants;
using Cirreum.Authorization.Operations.Grants.Caching;
using Cirreum.Caching;
using Cirreum.Conductor.Tests.Domain.TestGrant;

[TestClass]
public class OperationGrantCacheTests {

	private const string CallerId = "user-123";
	private const string TenantA = "tenant-A";

	// L1 — scoped memoization
	// -------------------------------------------------------------

	[TestMethod]
	public async Task L1_hit_resolves_grants_only_once_per_scope() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var factory = BuildFactory(grantResolver);

		var ctx1 = BuildContext(new TestDeleteCmd());
		var ctx2 = BuildContext(new TestDeleteCmd());

		var grant1 = await factory.CreateAsync(ctx1, CancellationToken.None);
		var grant2 = await factory.CreateAsync(ctx2, CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount, "ResolveGrantsAsync should be called once (L1 hit on second call)");
		Assert.IsFalse(grant1.IsDenied);
		Assert.IsFalse(grant2.IsDenied);
	}

	[TestMethod]
	public async Task L1_miss_when_different_permissions() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var factory = BuildFactory(grantResolver);

		var ctxDelete = BuildContext(new TestDeleteCmd());
		var ctxRead = BuildContext(new TestReadCmd());

		await factory.CreateAsync(ctxDelete, CancellationToken.None);
		await factory.CreateAsync(ctxRead, CancellationToken.None);

		Assert.AreEqual(2, grantResolver.ResolveCount, "Different permissions should cause L1 miss");
	}

	// L2 — cross-request cache
	// -------------------------------------------------------------

	[TestMethod]
	public async Task L2_hit_resolves_grants_once_across_scopes() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();
		var settings = new OperationGrantCacheSettings();

		// Scope 1
		var factory1 = BuildFactory(grantResolver, cacheService, settings);
		var ctx1 = BuildContext(new TestDeleteCmd());
		await factory1.CreateAsync(ctx1, CancellationToken.None);

		// Scope 2 — new factory instance, same L2 cache
		var factory2 = BuildFactory(grantResolver, cacheService, settings);
		var ctx2 = BuildContext(new TestDeleteCmd());
		await factory2.CreateAsync(ctx2, CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount, "L2 should serve second scope from cache");
	}

	// Permission-based dedup — same permissions share L2 entry
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Same_permissions_on_different_types_share_L2_entry() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();
		var settings = new OperationGrantCacheSettings();

		var factory = BuildFactory(grantResolver, cacheService, settings);

		// Both TestDeleteCmd and TestDeleteCmd2 require [RequiresPermission("delete")]
		var ctx1 = BuildContext(new TestDeleteCmd());
		var ctx2 = BuildContext(new TestDeleteCmd2());

		await factory.CreateAsync(ctx1, CancellationToken.None);
		await factory.CreateAsync(ctx2, CancellationToken.None);

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
		var factory1 = BuildFactory(grantResolver, noCacheService);
		await factory1.CreateAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		// Scope 2 — fresh factory, NoCacheService passes through
		var factory2 = BuildFactory(grantResolver, noCacheService);
		await factory2.CreateAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		Assert.AreEqual(2, grantResolver.ResolveCount);
	}

	// Domain override expiration
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Domain_override_expiration_is_respected() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();
		var settings = new OperationGrantCacheSettings {
			Expiration = TimeSpan.FromMinutes(5),
			DomainOverrides = new Dictionary<string, OperationGrantCacheDomainOverride> {
				["testgrant"] = new() { Expiration = TimeSpan.FromMinutes(10) }
			}
		};

		// Scope 1 — populates L2
		var factory1 = BuildFactory(grantResolver, cacheService, settings);
		await factory1.CreateAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		// Scope 2 — should hit L2 cache
		var factory2 = BuildFactory(grantResolver, cacheService, settings);
		await factory2.CreateAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		Assert.AreEqual(1, grantResolver.ResolveCount, "Domain override expiration should not prevent L2 hit");
	}

	// Version bump causes miss
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Version_bump_causes_L2_miss() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var cacheService = new InMemoryTestCacheService();

		// Scope 1 with version 1
		var settings1 = new OperationGrantCacheSettings { Version = 1 };
		var factory1 = BuildFactory(grantResolver, cacheService, settings1);
		await factory1.CreateAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		// Scope 2 with version 2 — different key, L2 miss
		var settings2 = new OperationGrantCacheSettings { Version = 2 };
		var factory2 = BuildFactory(grantResolver, cacheService, settings2);
		await factory2.CreateAsync(BuildContext(new TestDeleteCmd()), CancellationToken.None);

		Assert.AreEqual(2, grantResolver.ResolveCount, "Version bump should cause L2 miss");
	}

	// Bypass always live — never cached
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Bypass_always_runs_live() {
		var grantResolver = new CountingGrantResolver([TenantA]) { AlwaysBypass = true };
		var factory = BuildFactory(grantResolver);

		var ctx1 = BuildContext(new TestDeleteCmd());
		var ctx2 = BuildContext(new TestDeleteCmd());

		var grant1 = await factory.CreateAsync(ctx1, CancellationToken.None);
		var grant2 = await factory.CreateAsync(ctx2, CancellationToken.None);

		Assert.IsTrue(grant1.IsUnrestricted);
		Assert.IsTrue(grant2.IsUnrestricted);
		Assert.AreEqual(2, grantResolver.BypassCount, "ShouldBypassAsync should run every time");
		Assert.AreEqual(0, grantResolver.ResolveCount, "ResolveGrantsAsync should never be called when bypassing");
	}

	// Unauthenticated → Denied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Unauthenticated_caller_returns_denied() {
		var grantResolver = new CountingGrantResolver([TenantA]);
		var factory = BuildFactory(grantResolver);

		var ctx = BuildUnauthenticatedContext(new TestDeleteCmd());

		var grant = await factory.CreateAsync(ctx, CancellationToken.None);

		Assert.IsTrue(grant.IsDenied);
		Assert.AreEqual(0, grantResolver.ResolveCount);
	}

	// Empty grants → Denied
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Empty_grants_returns_denied() {
		var grantResolver = new CountingGrantResolver([]);
		var factory = BuildFactory(grantResolver);

		var ctx = BuildContext(new TestDeleteCmd());

		var grant = await factory.CreateAsync(ctx, CancellationToken.None);

		Assert.IsTrue(grant.IsDenied);
	}

	// Home owner merge
	// -------------------------------------------------------------

	[TestMethod]
	public async Task Home_owner_is_merged_into_grants() {
		var grantResolver = new CountingGrantResolver(["other-tenant"]) {
			HomeOwnerId = TenantA
		};
		var factory = BuildFactory(grantResolver);

		var ctx = BuildContext(new TestDeleteCmd());
		var grant = await factory.CreateAsync(ctx, CancellationToken.None);

		Assert.IsFalse(grant.IsDenied);
		Assert.IsFalse(grant.IsUnrestricted);
		Assert.IsNotNull(grant.OwnerIds);
		CollectionAssert.Contains(grant.OwnerIds.ToList(), TenantA);
		CollectionAssert.Contains(grant.OwnerIds.ToList(), "other-tenant");
	}

	// Helpers
	// -------------------------------------------------------------

	private static OperationGrantFactory BuildFactory(
		CountingGrantResolver grantResolver,
		ICacheService? cacheService = null,
		OperationGrantCacheSettings? cacheSettings = null,
		CacheSettings? rootCacheSettings = null) {

		return new OperationGrantFactory(
			grantResolver,
			cacheService ?? new InMemoryTestCacheService(),
			rootCacheSettings ?? new CacheSettings(),
			cacheSettings ?? new OperationGrantCacheSettings());
	}

	private static AuthorizationContext<TAuthorizableObject> BuildContext<TAuthorizableObject>(TAuthorizableObject authorizableObject)
		where TAuthorizableObject : IAuthorizableObject {

		var userState = TestUserState.CreateAuthenticated(id: CallerId);
		return new AuthorizationContext<TAuthorizableObject>(
			UserState: userState,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: authorizableObject);
	}

	private static AuthorizationContext<TAuthorizableObject> BuildUnauthenticatedContext<TAuthorizableObject>(TAuthorizableObject authorizableObject)
		where TAuthorizableObject : IAuthorizableObject {

		var userState = new TestUserState();
		return new AuthorizationContext<TAuthorizableObject>(
			UserState: userState,
			EffectiveRoles: ImmutableHashSet<Role>.Empty,
			AuthorizableObject: authorizableObject);
	}

	/// <summary>
	/// Simple in-memory cache service for testing L2 behavior.
	/// </summary>
	private sealed class InMemoryTestCacheService : ICacheService {

		private readonly Dictionary<string, object> _store = [];
		private readonly Dictionary<string, HashSet<string>> _tagIndex = [];

		public async ValueTask<TResultValue> GetOrCreateAsync<TResultValue>(
			string cacheKey,
			Func<CancellationToken, ValueTask<TResultValue>> factory,
			CacheExpirationSettings settings,
			string[]? tags = null,
			CancellationToken cancellationToken = default) {

			if (this._store.TryGetValue(cacheKey, out var cached)) {
				return (TResultValue)cached;
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
	using Cirreum.Authorization.Operations;
	using Cirreum.Authorization.Operations.Grants;
	using Cirreum.Conductor;

	[RequiresPermission("delete")]
	internal sealed class TestDeleteCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("delete")]
	internal sealed class TestDeleteCmd2 : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("read")]
	internal sealed class TestReadCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	internal sealed class CountingGrantResolver(IReadOnlyList<string> ownerIds)
		: IOperationGrantProvider {

		public int ResolveCount;
		public int BypassCount;
		public bool AlwaysBypass;
		public string? HomeOwnerId;

		public ValueTask<OperationGrantResult> ResolveGrantsAsync<TAuthorizableObject>(
			AuthorizationContext<TAuthorizableObject> context,
			CancellationToken cancellationToken)
			where TAuthorizableObject : IAuthorizableObject {

			Interlocked.Increment(ref this.ResolveCount);
			return ValueTask.FromResult(new OperationGrantResult(ownerIds));
		}

		public ValueTask<bool> ShouldBypassAsync<TAuthorizableObject>(
			AuthorizationContext<TAuthorizableObject> context,
			CancellationToken cancellationToken)
			where TAuthorizableObject : IAuthorizableObject {

			Interlocked.Increment(ref this.BypassCount);
			return ValueTask.FromResult(this.AlwaysBypass);
		}

		public ValueTask<string?> ResolveHomeOwnerAsync<TAuthorizableObject>(
			AuthorizationContext<TAuthorizableObject> context,
			CancellationToken cancellationToken)
			where TAuthorizableObject : IAuthorizableObject {

			return ValueTask.FromResult(this.HomeOwnerId);
		}
	}
}
