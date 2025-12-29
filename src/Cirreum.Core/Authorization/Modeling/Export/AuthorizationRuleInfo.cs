namespace Cirreum.Authorization.Modeling.Export;

/// <summary>
/// Represents detailed information about an authorization rule.
/// This is the serializable view for visualization or inspection.
/// </summary>
public sealed record AuthorizationRuleInfo(
	string ResourceTypeName,
	string ResourceTypeFullName,
	string AuthorizorTypeName,
	string AuthorizorTypeFullName,
	string PropertyPath,
	string ValidationLogic,
	string Message
);