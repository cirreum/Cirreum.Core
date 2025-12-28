namespace Cirreum.Authorization.Modeling.Types;

public record CombinedRuleTypeInfo(
	IReadOnlyList<AuthorizationRuleTypeInfo> ResourceRules,
	IReadOnlyList<PolicyRuleTypeInfo> PolicyRules,
	int TotalRules
);