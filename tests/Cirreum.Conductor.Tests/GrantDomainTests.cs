namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Authorization.Grants;

[TestClass]
public class GrantDomainTests {

	// GrantDomainAttribute
	// -------------------------------------------------------------

	[TestMethod]
	public void Attribute_normalizes_namespace_to_lowercase() {
		var attr = new GrantDomainAttribute("Issues");

		Assert.AreEqual("issues", attr.Namespace);
	}

	[TestMethod]
	public void Attribute_preserves_lowercase_namespace() {
		var attr = new GrantDomainAttribute("documents");

		Assert.AreEqual("documents", attr.Namespace);
	}

	// GrantDomainCache
	// -------------------------------------------------------------

	[TestMethod]
	public void Cache_resolves_attribute_from_decorated_interface() {
		var result = GrantDomainCache.GetFor<ITestIssueOperation>();

		Assert.AreEqual("issues", result.Namespace);
	}

	[TestMethod]
	public void Cache_throws_for_undecorated_interface() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => GrantDomainCache.GetFor<IUndecoratedDomain>());
	}

	// RequiredPermissionsCache — single-arg namespace resolution
	// -------------------------------------------------------------

	[TestMethod]
	public void Single_arg_permission_resolves_namespace_from_domain() {
		var permissions = RequiredPermissionsCache.GetFor<DeleteIssueCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("issues", permissions[0].Namespace);
		Assert.AreEqual("delete", permissions[0].Name);
	}

	[TestMethod]
	public void Two_arg_permission_validates_matching_namespace() {
		var permissions = RequiredPermissionsCache.GetFor<ArchiveIssueCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("issues", permissions[0].Namespace);
		Assert.AreEqual("archive", permissions[0].Name);
	}

	[TestMethod]
	public void Mismatched_namespace_on_granted_resource_throws() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => RequiredPermissionsCache.GetFor<CrossDomainCmd>());
	}

	[TestMethod]
	public void Single_arg_on_non_granted_resource_throws() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => RequiredPermissionsCache.GetFor<NonGrantedWithNameOnly>());
	}

	[TestMethod]
	public void Multiple_permissions_are_all_resolved() {
		var permissions = RequiredPermissionsCache.GetFor<MultiPermCmd>();

		Assert.HasCount(2, permissions);
		Assert.IsTrue(permissions.Any(p => p.Name == "write"));
		Assert.IsTrue(permissions.Any(p => p.Name == "audit"));
		Assert.IsTrue(permissions.All(p => p.Namespace == "issues"));
	}

	[TestMethod]
	public void Duplicate_permissions_are_deduplicated() {
		var permissions = RequiredPermissionsCache.GetFor<DuplicatePermCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("delete", permissions[0].Name);
	}

	[TestMethod]
	public void No_permission_attributes_returns_empty() {
		var permissions = RequiredPermissionsCache.GetFor<NoPermCmd>();

		Assert.HasCount(0, permissions);
	}

	// Test doubles
	// -------------------------------------------------------------

	[GrantDomain("issues")]
	private interface ITestIssueOperation;

	private interface IUndecoratedDomain;

	[RequiresPermission("delete")]
	private sealed class DeleteIssueCmd : IGrantedCommand<ITestIssueOperation>, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("issues", "archive")]
	private sealed class ArchiveIssueCmd : IGrantedCommand<ITestIssueOperation>, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("audit", "write")]
	private sealed class CrossDomainCmd : IGrantedCommand<ITestIssueOperation>, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("delete")]
	private sealed class NonGrantedWithNameOnly : IAuthorizableCommand;

	[RequiresPermission("write")]
	[RequiresPermission("audit")]
	private sealed class MultiPermCmd : IGrantedCommand<ITestIssueOperation>, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}

	[RequiresPermission("delete")]
	[RequiresPermission("issues", "delete")]
	private sealed class DuplicatePermCmd : IGrantedCommand<ITestIssueOperation>, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}

	private sealed class NoPermCmd : IGrantedCommand<ITestIssueOperation>, IAuthorizableCommand {
		public string? OwnerId { get; set; }
	}
}
