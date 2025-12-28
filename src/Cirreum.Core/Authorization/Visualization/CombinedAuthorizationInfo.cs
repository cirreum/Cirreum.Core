namespace Cirreum.Authorization.Visualization;

public record CombinedAuthorizationInfo(
	IReadOnlyList<AuthorizationRuleTypeInfo> ResourceRules,
	IReadOnlyList<PolicyRuleTypeInfo> PolicyRules,
	int TotalProtectionPoints
);