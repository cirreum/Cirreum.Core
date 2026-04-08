namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Authorization.Operations.Grants.Caching;

[TestClass]
public class OperationGrantCacheKeysTests {

	// PermissionSet.ToSignature
	// -------------------------------------------------------------

	[TestMethod]
	public void Single_permission_signature_is_operation_only() {
		var set = new PermissionSet([new Permission("issues", "delete")]);

		Assert.AreEqual("delete", set.ToSignature());
	}

	[TestMethod]
	public void Multiple_permissions_are_sorted_and_joined() {
		var set = new PermissionSet([
			new Permission("issues", "write"),
			new Permission("issues", "delete"),
			new Permission("issues", "audit"),
		]);

		Assert.AreEqual("audit+delete+write", set.ToSignature());
	}

	[TestMethod]
	public void Empty_permissions_produce_empty_signature() {
		Assert.AreEqual(string.Empty, PermissionSet.Empty.ToSignature());
	}

	[TestMethod]
	public void Signature_is_deterministic_regardless_of_input_order() {
		var a = new PermissionSet([
			new Permission("issues", "delete"),
			new Permission("issues", "write"),
		]);
		var b = new PermissionSet([
			new Permission("issues", "write"),
			new Permission("issues", "delete"),
		]);

		Assert.AreEqual(a.ToSignature(), b.ToSignature());
	}

	// BuildKey
	// -------------------------------------------------------------

	[TestMethod]
	public void BuildKey_produces_expected_format() {
		var set = new PermissionSet([new Permission("issues", "delete")]);

		var key = OperationGrantCacheKeys.BuildKey(1, "user-123", "issues", set);

		Assert.AreEqual("grant:v1:user-123:issues:delete", key);
	}

	[TestMethod]
	public void BuildKey_with_multiple_permissions_sorts_signature() {
		var set = new PermissionSet([
			new Permission("issues", "write"),
			new Permission("issues", "delete"),
		]);

		var key = OperationGrantCacheKeys.BuildKey(2, "user-456", "issues", set);

		Assert.AreEqual("grant:v2:user-456:issues:delete+write", key);
	}

	[TestMethod]
	public void Version_bump_changes_key() {
		var set = new PermissionSet([new Permission("issues", "read")]);

		var v1 = OperationGrantCacheKeys.BuildKey(1, "u1", "issues", set);
		var v2 = OperationGrantCacheKeys.BuildKey(2, "u1", "issues", set);

		Assert.AreNotEqual(v1, v2);
	}

	// BuildTags
	// -------------------------------------------------------------

	[TestMethod]
	public void BuildTags_returns_caller_and_domain_tags() {
		var tags = OperationGrantCacheKeys.BuildTags("user-123", "issues");

		Assert.HasCount(2, tags);
		Assert.AreEqual("grant:caller:user-123", tags[0]);
		Assert.AreEqual("grant:domain:issues", tags[1]);
	}

	[TestMethod]
	public void CallerTag_format() {
		Assert.AreEqual("grant:caller:abc", OperationGrantCacheKeys.CallerTag("abc"));
	}

	[TestMethod]
	public void DomainTag_format() {
		Assert.AreEqual("grant:domain:issues", OperationGrantCacheKeys.DomainTag("issues"));
	}
}
