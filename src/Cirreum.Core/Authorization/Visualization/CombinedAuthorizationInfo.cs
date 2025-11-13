namespace Cirreum.Authorization.Visualization;

public record CombinedAuthorizationInfo(
	IReadOnlyList<AuthorizationRuleInfo> ResourceRules,
	IReadOnlyList<PolicyRuleInfo> PolicyRules,
	int TotalProtectionPoints
);