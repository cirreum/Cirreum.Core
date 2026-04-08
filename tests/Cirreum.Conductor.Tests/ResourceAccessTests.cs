namespace Cirreum.Conductor.Tests;

using System.Collections.Immutable;
using Cirreum.Authorization;
using Cirreum.Authorization.Resources;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[TestClass]
public class ResourceAccessTests {

	// ———————— Test Fixtures ————————

	private static readonly Role UserRole = Role.ForApp("user");
	private static readonly Role AdminRole = Role.ForApp("admin");
	private static readonly Role ManagerRole = Role.ForApp("manager");

	private static readonly Permission ReadPerm = new("docs", "read");
	private static readonly Permission WritePerm = new("docs", "write");
	private static readonly Permission DeletePerm = new("docs", "delete");

	// ———————— AccessEntry.HasPermission ————————

	[TestMethod]
	public void AccessEntry_HasPermission_returns_true_for_matching_permission() {
		var entry = new AccessEntry {
			Role = UserRole,
			Permissions = [ReadPerm, WritePerm]
		};

		Assert.IsTrue(entry.HasPermission(ReadPerm));
		Assert.IsTrue(entry.HasPermission(WritePerm));
	}

	[TestMethod]
	public void AccessEntry_HasPermission_returns_false_for_non_matching_permission() {
		var entry = new AccessEntry {
			Role = UserRole,
			Permissions = [ReadPerm]
		};

		Assert.IsFalse(entry.HasPermission(WritePerm));
		Assert.IsFalse(entry.HasPermission(DeletePerm));
	}

	[TestMethod]
	public void AccessEntry_HasPermission_returns_false_for_empty_permissions() {
		var entry = new AccessEntry {
			Role = UserRole,
			Permissions = []
		};

		Assert.IsFalse(entry.HasPermission(ReadPerm));
	}

	// ———————— EffectiveAccess.IsAuthorized ————————

	[TestMethod]
	public void EffectiveAccess_IsAuthorized_returns_true_when_role_and_permission_match() {
		var entries = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [ReadPerm] },
			new() { Role = AdminRole, Permissions = [WritePerm, DeletePerm] }
		};
		var effective = new EffectiveAccess(entries);
		var callerRoles = ImmutableHashSet.Create(UserRole);

		Assert.IsTrue(effective.IsAuthorized(ReadPerm, callerRoles));
	}

	[TestMethod]
	public void EffectiveAccess_IsAuthorized_returns_false_when_role_matches_but_permission_does_not() {
		var entries = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [ReadPerm] }
		};
		var effective = new EffectiveAccess(entries);
		var callerRoles = ImmutableHashSet.Create(UserRole);

		Assert.IsFalse(effective.IsAuthorized(WritePerm, callerRoles));
	}

	[TestMethod]
	public void EffectiveAccess_IsAuthorized_returns_false_when_permission_matches_but_role_does_not() {
		var entries = new List<AccessEntry> {
			new() { Role = AdminRole, Permissions = [ReadPerm] }
		};
		var effective = new EffectiveAccess(entries);
		var callerRoles = ImmutableHashSet.Create(UserRole);

		Assert.IsFalse(effective.IsAuthorized(ReadPerm, callerRoles));
	}

	[TestMethod]
	public void EffectiveAccess_IsAuthorized_checks_multiple_entries() {
		var entries = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [ReadPerm] },
			new() { Role = AdminRole, Permissions = [DeletePerm] }
		};
		var effective = new EffectiveAccess(entries);
		var callerRoles = ImmutableHashSet.Create(AdminRole);

		// Caller has AdminRole — should match the second entry
		Assert.IsTrue(effective.IsAuthorized(DeletePerm, callerRoles));
		Assert.IsFalse(effective.IsAuthorized(ReadPerm, callerRoles));
	}

	[TestMethod]
	public void EffectiveAccess_IsAuthorized_returns_false_for_empty_entries() {
		var effective = new EffectiveAccess([]);
		var callerRoles = ImmutableHashSet.Create(UserRole);

		Assert.IsFalse(effective.IsAuthorized(ReadPerm, callerRoles));
	}

	[TestMethod]
	public void EffectiveAccess_IsAuthorized_returns_false_for_empty_roles() {
		var entries = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [ReadPerm] }
		};
		var effective = new EffectiveAccess(entries);

		Assert.IsFalse(effective.IsAuthorized(ReadPerm, ImmutableHashSet<Role>.Empty));
	}

	// ———————— ResourceAccessEvaluator — CheckAsync(resource) ————————

	[TestMethod]
	public async Task CheckAsync_resource_allows_when_acl_grants_permission() {
		var sut = BuildEvaluator();
		var folder = new TestFolder("folder-1", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		]);

		var result = await sut.CheckAsync(folder, ReadPerm);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task CheckAsync_resource_denies_when_acl_does_not_grant_permission() {
		var sut = BuildEvaluator();
		var folder = new TestFolder("folder-1", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		]);

		var result = await sut.CheckAsync(folder, WritePerm);

		Assert.IsFalse(result.IsSuccess);
	}

	[TestMethod]
	public async Task CheckAsync_resource_denies_when_caller_has_no_roles() {
		var sut = BuildEvaluator(effectiveRoles: ImmutableHashSet<Role>.Empty);
		var folder = new TestFolder("folder-1", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		]);

		var result = await sut.CheckAsync(folder, ReadPerm);

		Assert.IsFalse(result.IsSuccess);
	}

	// ———————— ResourceAccessEvaluator — CheckAsync(id) ————————

	[TestMethod]
	public async Task CheckAsync_id_loads_resource_and_evaluates() {
		var folder = new TestFolder("folder-1", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		]);
		var sut = BuildEvaluator(resources: [folder]);

		var result = await sut.CheckAsync<TestFolder>("folder-1", ReadPerm);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task CheckAsync_id_returns_failure_when_resource_not_found() {
		var sut = BuildEvaluator(resources: []);

		var result = await sut.CheckAsync<TestFolder>("nonexistent", ReadPerm);

		Assert.IsFalse(result.IsSuccess);
	}

	[TestMethod]
	public async Task CheckAsync_null_id_uses_root_defaults() {
		var rootDefaults = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [ReadPerm] }
		};
		var sut = BuildEvaluator(rootDefaults: rootDefaults);

		var result = await sut.CheckAsync<TestFolder>(resourceId: null, ReadPerm);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task CheckAsync_null_id_denies_when_root_defaults_insufficient() {
		var rootDefaults = new List<AccessEntry> {
			new() { Role = AdminRole, Permissions = [ReadPerm] }
		};
		// Caller has UserRole, root defaults require AdminRole
		var sut = BuildEvaluator(rootDefaults: rootDefaults);

		var result = await sut.CheckAsync<TestFolder>(resourceId: null, ReadPerm);

		Assert.IsFalse(result.IsSuccess);
	}

	// ———————— ResourceAccessEvaluator — FilterAsync ————————

	[TestMethod]
	public async Task FilterAsync_returns_only_authorized_resources() {
		var allowed = new TestFolder("f-1", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		]);
		var denied = new TestFolder("f-2", [
			new() { Role = AdminRole, Permissions = [ReadPerm] }
		]);
		var alsoAllowed = new TestFolder("f-3", [
			new() { Role = UserRole, Permissions = [ReadPerm, WritePerm] }
		]);
		var sut = BuildEvaluator(resources: [allowed, denied, alsoAllowed]);

		var result = await sut.FilterAsync([allowed, denied, alsoAllowed], ReadPerm);

		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("f-1", result[0].ResourceId);
		Assert.AreEqual("f-3", result[1].ResourceId);
	}

	[TestMethod]
	public async Task FilterAsync_returns_empty_when_caller_has_no_roles() {
		var folder = new TestFolder("f-1", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		]);
		var sut = BuildEvaluator(
			resources: [folder],
			effectiveRoles: ImmutableHashSet<Role>.Empty);

		var result = await sut.FilterAsync([folder], ReadPerm);

		Assert.AreEqual(0, result.Count);
	}

	// ———————— Hierarchy walking ————————

	[TestMethod]
	public async Task Hierarchy_inherits_parent_permissions() {
		// Parent has ReadPerm, child inherits
		var parent = new TestFolder("parent", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: null, InheritPermissions: true);

		var child = new TestFolder("child", [], ParentId: "parent", InheritPermissions: true);

		var sut = BuildEvaluator(resources: [parent, child]);

		var result = await sut.CheckAsync(child, ReadPerm);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task Hierarchy_stops_when_InheritPermissions_is_false() {
		var parent = new TestFolder("parent", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: null, InheritPermissions: true);

		// Child breaks inheritance — parent's ReadPerm should NOT be inherited
		var child = new TestFolder("child", [
			new() { Role = UserRole, Permissions = [WritePerm] }
		], ParentId: "parent", InheritPermissions: false);

		var sut = BuildEvaluator(resources: [parent, child]);

		// Child has WritePerm directly
		Assert.IsTrue((await sut.CheckAsync(child, WritePerm)).IsSuccess);
		// Child should NOT inherit parent's ReadPerm
		Assert.IsFalse((await sut.CheckAsync(child, ReadPerm)).IsSuccess);
	}

	[TestMethod]
	public async Task Hierarchy_merges_root_defaults_at_top() {
		var rootDefaults = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [DeletePerm] }
		};

		// Root folder (no parent) with inheritance on
		var root = new TestFolder("root", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: null, InheritPermissions: true);

		var child = new TestFolder("child", [], ParentId: "root", InheritPermissions: true);

		var sut = BuildEvaluator(resources: [root, child], rootDefaults: rootDefaults);

		// Child should inherit root's ReadPerm + rootDefaults' DeletePerm
		Assert.IsTrue((await sut.CheckAsync(child, ReadPerm)).IsSuccess);
		Assert.IsTrue((await sut.CheckAsync(child, DeletePerm)).IsSuccess);
	}

	[TestMethod]
	public async Task Hierarchy_detects_cycle_and_stops() {
		// A → B → A (cycle)
		var folderA = new TestFolder("A", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: "B", InheritPermissions: true);

		var folderB = new TestFolder("B", [
			new() { Role = UserRole, Permissions = [WritePerm] }
		], ParentId: "A", InheritPermissions: true);

		var sut = BuildEvaluator(resources: [folderA, folderB]);

		// Should not hang — cycle detection stops the walk
		var result = await sut.CheckAsync(folderA, ReadPerm);
		Assert.IsTrue(result.IsSuccess); // Own ACL still works
	}

	[TestMethod]
	public async Task Hierarchy_handles_orphan_parent() {
		// Child references a parent that doesn't exist
		var child = new TestFolder("child", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: "nonexistent", InheritPermissions: true);

		var sut = BuildEvaluator(resources: [child]);

		// Own ACL still works despite orphan
		var result = await sut.CheckAsync(child, ReadPerm);
		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task Hierarchy_L1_cache_reuses_effective_access_for_same_resource() {
		var parent = new TestFolder("parent", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: null, InheritPermissions: true);

		var child = new TestFolder("child", [], ParentId: "parent", InheritPermissions: true);

		var provider = new TestFolderProvider([parent, child]);
		var sut = BuildEvaluator(provider: provider);

		// First check walks the hierarchy
		Assert.IsTrue((await sut.CheckAsync(child, ReadPerm)).IsSuccess);

		// Reset the load counter — second check on same resource should hit L1 cache
		provider.ResetLoadCount();
		Assert.IsTrue((await sut.CheckAsync(child, WritePerm)).IsSuccess == false);

		// Child's effective access was cached — no provider loads needed
		Assert.AreEqual(0, provider.LoadCount, "Second check should have been served from L1 cache");
	}

	[TestMethod]
	public async Task Hierarchy_L1_cache_sibling_reuses_parent_when_parent_checked_first() {
		var rootDefaults = new List<AccessEntry> {
			new() { Role = UserRole, Permissions = [DeletePerm] }
		};

		var parent = new TestFolder("parent", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: null, InheritPermissions: true);

		var sibling1 = new TestFolder("s1", [], ParentId: "parent", InheritPermissions: true);

		var provider = new TestFolderProvider([parent, sibling1], rootDefaults);
		var sut = BuildEvaluator(provider: provider);

		// Check parent first — caches parent's effective access
		Assert.IsTrue((await sut.CheckAsync(parent, ReadPerm)).IsSuccess);

		// Reset the load counter — sibling should find parent in L1 cache
		provider.ResetLoadCount();
		Assert.IsTrue((await sut.CheckAsync(sibling1, ReadPerm)).IsSuccess);

		// Parent should NOT have been loaded again (L1 hit on parent's cached effective access)
		Assert.AreEqual(0, provider.LoadCount, "Parent should have been served from L1 cache");
	}

	[TestMethod]
	public async Task Hierarchy_multi_level_walk() {
		var grandparent = new TestFolder("gp", [
			new() { Role = UserRole, Permissions = [DeletePerm] }
		], ParentId: null, InheritPermissions: true);

		var parent = new TestFolder("p", [
			new() { Role = UserRole, Permissions = [WritePerm] }
		], ParentId: "gp", InheritPermissions: true);

		var child = new TestFolder("c", [
			new() { Role = UserRole, Permissions = [ReadPerm] }
		], ParentId: "p", InheritPermissions: true);

		var sut = BuildEvaluator(resources: [grandparent, parent, child]);

		// Child should have all three permissions
		Assert.IsTrue((await sut.CheckAsync(child, ReadPerm)).IsSuccess);
		Assert.IsTrue((await sut.CheckAsync(child, WritePerm)).IsSuccess);
		Assert.IsTrue((await sut.CheckAsync(child, DeletePerm)).IsSuccess);
	}

	// ———————— Test Infrastructure ————————

	private sealed record TestFolder(
		string? ResourceId,
		IReadOnlyList<AccessEntry> AccessList,
		string? ParentId = null,
		bool InheritPermissions = false) : IProtectedResource;

	private sealed class TestFolderProvider : IAccessEntryProvider<TestFolder> {

		private readonly Dictionary<string, TestFolder> _resources;
		public IReadOnlyList<AccessEntry> RootDefaults { get; }
		public int LoadCount { get; private set; }

		public TestFolderProvider(
			IEnumerable<TestFolder> resources,
			IReadOnlyList<AccessEntry>? rootDefaults = null) {
			_resources = resources.Where(r => r.ResourceId is not null)
				.ToDictionary(r => r.ResourceId!);
			RootDefaults = rootDefaults ?? [];
		}

		public ValueTask<TestFolder?> GetByIdAsync(string resourceId, CancellationToken cancellationToken) {
			LoadCount++;
			_resources.TryGetValue(resourceId, out var resource);
			return new ValueTask<TestFolder?>(resource);
		}

		public string? GetParentId(TestFolder resource) => resource.ParentId;

		public void ResetLoadCount() => LoadCount = 0;
	}

	private ResourceAccessEvaluator BuildEvaluator(
		IEnumerable<TestFolder>? resources = null,
		IReadOnlyList<AccessEntry>? rootDefaults = null,
		IImmutableSet<Role>? effectiveRoles = null,
		TestFolderProvider? provider = null) {

		provider ??= new TestFolderProvider(resources ?? [], rootDefaults);

		// Create an authenticated user with the app:user role claim
		var userState = TestUserState.CreateAuthenticated();

		// Pre-populate the accessor with a resolved context (simulates the pipeline having run)
		var contextAccessor = new DefaultAuthorizationContextAccessor();
		contextAccessor.Set(new AuthorizationContext(
			userState,
			effectiveRoles ?? ImmutableHashSet.Create(UserRole)));

		var services = new ServiceCollection();
		services.AddLogging(lb => lb.AddDebug().SetMinimumLevel(LogLevel.Trace));
		services.AddSingleton<IAccessEntryProvider<TestFolder>>(provider);
		var sp = services.BuildServiceProvider();

		return new ResourceAccessEvaluator(
			contextAccessor,
			sp,
			sp.GetRequiredService<ILogger<ResourceAccessEvaluator>>());
	}
}
