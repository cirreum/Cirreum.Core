namespace Cirreum.Authorization.Visualization;

public record PolicyRuleInfo(
	string PolicyName,
	Type ValidatorType,
	int Order,
	DomainRuntimeType[] SupportedRuntimeTypes,
	bool IsAttributeBased,
	Type? TargetAttributeType,
	string Description
);