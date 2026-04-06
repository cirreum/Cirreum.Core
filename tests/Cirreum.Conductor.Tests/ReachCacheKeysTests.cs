namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Authorization.Grants.Caching;

[TestClass]
public class ReachCacheKeysTests {

	// BuildPermissionSignature
	// -------------------------------------------------------------

	[TestMethod]
	public void Single_permission_signature_is_name_only() {
		var permissions = new List<Permission> { new("issues", "delete") };

		var sig = ReachCacheKeys.BuildPermissionSignature(permissions);

		Assert.AreEqual("delete", sig);
	}

	[TestMethod]
	public void Multiple_permissions_are_sorted_and_joined() {
		var permissions = new List<Permission> {
			new("issues", "write"),
			new("issues", "delete"),
			new("issues", "audit"),
		};

		var sig = ReachCacheKeys.BuildPermissionSignature(permissions);

		Assert.AreEqual("audit+delete+write", sig);
	}

	[TestMethod]
	public void Empty_permissions_produce_empty_signature() {
		var sig = ReachCacheKeys.BuildPermissionSignature([]);

		Assert.AreEqual(string.Empty, sig);
	}

	[TestMethod]
	public void Signature_is_deterministic_regardless_of_input_order() {
		var a = new List<Permission> {
			new("issues", "delete"),
			new("issues", "write"),
		};
		var b = new List<Permission> {
			new("issues", "write"),
			new("issues", "delete"),
		};

		Assert.AreEqual(
			ReachCacheKeys.BuildPermissionSignature(a),
			ReachCacheKeys.BuildPermissionSignature(b));
	}

	// BuildKey
	// -------------------------------------------------------------

	[TestMethod]
	public void BuildKey_produces_expected_format() {
		var permissions = new List<Permission> { new("issues", "delete") };

		var key = ReachCacheKeys.BuildKey(1, "user-123", "issues", permissions);

		Assert.AreEqual("reach:v1:user-123:issues:delete", key);
	}

	[TestMethod]
	public void BuildKey_with_multiple_permissions_sorts_signature() {
		var permissions = new List<Permission> {
			new("issues", "write"),
			new("issues", "delete"),
		};

		var key = ReachCacheKeys.BuildKey(2, "user-456", "issues", permissions);

		Assert.AreEqual("reach:v2:user-456:issues:delete+write", key);
	}

	[TestMethod]
	public void Version_bump_changes_key() {
		var permissions = new List<Permission> { new("issues", "read") };

		var v1 = ReachCacheKeys.BuildKey(1, "u1", "issues", permissions);
		var v2 = ReachCacheKeys.BuildKey(2, "u1", "issues", permissions);

		Assert.AreNotEqual(v1, v2);
	}

	// BuildTags
	// -------------------------------------------------------------

	[TestMethod]
	public void BuildTags_returns_caller_and_domain_tags() {
		var tags = ReachCacheKeys.BuildTags("user-123", "issues");

		Assert.HasCount(2, tags);
		Assert.AreEqual("reach:caller:user-123", tags[0]);
		Assert.AreEqual("reach:domain:issues", tags[1]);
	}

	[TestMethod]
	public void CallerTag_format() {
		Assert.AreEqual("reach:caller:abc", ReachCacheKeys.CallerTag("abc"));
	}

	[TestMethod]
	public void DomainTag_format() {
		Assert.AreEqual("reach:domain:issues", ReachCacheKeys.DomainTag("issues"));
	}
}
