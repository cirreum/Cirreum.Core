namespace Cirreum.Conductor.Tests {

using Cirreum.Authorization;
using Cirreum.Authorization.Grants;
using Cirreum.Conductor.Tests.Domain.Issues;

[TestClass]
public class DomainFeatureTests {

	// DomainFeatureResolver — namespace convention
	// -------------------------------------------------------------

	[TestMethod]
	public void Resolver_derives_domain_from_namespace() {
		var result = DomainFeatureResolver.Resolve(typeof(DeleteIssueCmd));

		Assert.AreEqual("issues", result);
	}

	[TestMethod]
	public void Resolver_returns_null_for_type_without_domain_namespace() {
		var result = DomainFeatureResolver.Resolve(typeof(string));

		Assert.IsNull(result);
	}

	[TestMethod]
	public void Resolver_lowercases_domain_name() {
		// The namespace segment is "Issues" (PascalCase), should resolve to "issues"
		var result = DomainFeatureResolver.Resolve(typeof(DeleteIssueCmd));

		Assert.AreEqual("issues", result);
	}

	// PermissionSetCache — single-arg feature resolution
	// -------------------------------------------------------------

	[TestMethod]
	public void Single_arg_permission_resolves_feature_from_namespace() {
		var permissions = PermissionSetCache.GetFor<DeleteIssueCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("issues", permissions[0].Feature);
		Assert.AreEqual("delete", permissions[0].Operation);
	}

	[TestMethod]
	public void Two_arg_permission_validates_matching_feature() {
		var permissions = PermissionSetCache.GetFor<ArchiveIssueCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("issues", permissions[0].Feature);
		Assert.AreEqual("archive", permissions[0].Operation);
	}

	[TestMethod]
	public void Mismatched_feature_on_granted_resource_throws() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => PermissionSetCache.GetFor<CrossDomainCmd>());
	}

	[TestMethod]
	public void Single_arg_on_type_without_domain_namespace_throws() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => PermissionSetCache.GetFor<NonGrantedWithNameOnly>());
	}

	[TestMethod]
	public void Multiple_permissions_are_all_resolved() {
		var permissions = PermissionSetCache.GetFor<MultiPermCmd>();

		Assert.HasCount(2, permissions);
		Assert.IsTrue(permissions.Any(p => p.Operation == "write"));
		Assert.IsTrue(permissions.Any(p => p.Operation == "audit"));
		Assert.IsTrue(permissions.All(p => p.Feature == "issues"));
	}

	[TestMethod]
	public void Duplicate_permissions_are_deduplicated() {
		var permissions = PermissionSetCache.GetFor<DuplicatePermCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("delete", permissions[0].Operation);
	}

	[TestMethod]
	public void No_permission_attributes_returns_empty() {
		var permissions = PermissionSetCache.GetFor<NoPermCmd>();

		Assert.HasCount(0, permissions);
	}

	// Non-Domain namespace test double (stays in Cirreum.Conductor.Tests namespace)

	[RequiresPermission("delete")]
	private sealed class NonGrantedWithNameOnly : IAuthorizableCommand;
}
}

// ─────────────────────────────────────────────────────────────────
// Test doubles in a *.Domain.Issues.* namespace for DomainFeatureResolver
// ─────────────────────────────────────────────────────────────────

namespace Cirreum.Conductor.Tests.Domain.Issues {

	using Cirreum.Authorization;
	using Cirreum.Authorization.Grants;
	using Cirreum.Conductor;

	[RequiresPermission("delete")]
	internal sealed class DeleteIssueCmd : IGrantedCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("issues", "archive")]
	internal sealed class ArchiveIssueCmd : IGrantedCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("audit", "write")]
	internal sealed class CrossDomainCmd : IGrantedCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("write")]
	[RequiresPermission("audit")]
	internal sealed class MultiPermCmd : IGrantedCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("delete")]
	[RequiresPermission("issues", "delete")]
	internal sealed class DuplicatePermCmd : IGrantedCommand {
		public string? OwnerId { get; set; }
	}

	internal sealed class NoPermCmd : IGrantedCommand {
		public string? OwnerId { get; set; }
	}
}
