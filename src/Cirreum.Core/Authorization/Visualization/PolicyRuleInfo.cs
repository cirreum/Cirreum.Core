namespace Cirreum.Authorization.Visualization;

public record PolicyRuleInfo(
	string PolicyName,
	Type ValidatorType,
	int Order,
	ApplicationRuntimeType[] SupportedRuntimeTypes,
	bool IsAttributeBased,
	Type? TargetAttributeType,
	string Description
);