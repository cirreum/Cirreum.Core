namespace Cirreum.Conductor.Tests {

using Cirreum.Authorization;
using Cirreum.Authorization.Operations;
using Cirreum.Authorization.Operations.Grants;
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

	// RequiredGrantCache — single-arg feature resolution
	// -------------------------------------------------------------

	[TestMethod]
	public void Single_arg_permission_resolves_feature_from_namespace() {
		var permissions = RequiredGrantCache.GetFor<DeleteIssueCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("issues", permissions[0].Feature);
		Assert.AreEqual("delete", permissions[0].Operation);
	}

	[TestMethod]
	public void Two_arg_permission_validates_matching_feature() {
		var permissions = RequiredGrantCache.GetFor<ArchiveIssueCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("issues", permissions[0].Feature);
		Assert.AreEqual("archive", permissions[0].Operation);
	}

	[TestMethod]
	public void Mismatched_feature_on_granted_resource_throws() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => RequiredGrantCache.GetFor<CrossFeatureCmd>());
	}

	[TestMethod]
	public void Single_arg_on_type_without_domain_namespace_throws() {
		Assert.ThrowsExactly<InvalidOperationException>(
			() => RequiredGrantCache.GetFor<NonGrantedWithNameOnly>());
	}

	[TestMethod]
	public void Multiple_permissions_are_all_resolved() {
		var permissions = RequiredGrantCache.GetFor<MultiPermCmd>();

		Assert.HasCount(2, permissions);
		Assert.IsTrue(permissions.Any(p => p.Operation == "write"));
		Assert.IsTrue(permissions.Any(p => p.Operation == "audit"));
		Assert.IsTrue(permissions.All(p => p.Feature == "issues"));
	}

	[TestMethod]
	public void Duplicate_permissions_are_deduplicated() {
		var permissions = RequiredGrantCache.GetFor<DuplicatePermCmd>();

		Assert.HasCount(1, permissions);
		Assert.AreEqual("delete", permissions[0].Operation);
	}

	[TestMethod]
	public void No_permission_attributes_returns_empty() {
		var permissions = RequiredGrantCache.GetFor<NoPermCmd>();

		Assert.HasCount(0, permissions);
	}

	// Non-Domain namespace test double (stays in Cirreum.Conductor.Tests namespace)

	[RequiresGrant("delete")]
	private sealed class NonGrantedWithNameOnly : IAuthorizableOperation;
}
}

// ─────────────────────────────────────────────────────────────────
// Test doubles in a *.Domain.Issues.* namespace for DomainFeatureResolver
// ─────────────────────────────────────────────────────────────────

namespace Cirreum.Conductor.Tests.Domain.Issues {

	using Cirreum.Authorization;
	using Cirreum.Authorization.Operations;
	using Cirreum.Authorization.Operations.Grants;
	using Cirreum.Conductor;

	[RequiresGrant("delete")]
	internal sealed class DeleteIssueCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	[RequiresGrant("issues", "archive")]
	internal sealed class ArchiveIssueCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	[RequiresGrant("audit", "write")]
	internal sealed class CrossFeatureCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	[RequiresGrant("write")]
	[RequiresGrant("audit")]
	internal sealed class MultiPermCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	[RequiresGrant("delete")]
	[RequiresGrant("issues", "delete")]
	internal sealed class DuplicatePermCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}

	internal sealed class NoPermCmd : IOwnerMutateOperation {
		public string? OwnerId { get; set; }
	}
}
