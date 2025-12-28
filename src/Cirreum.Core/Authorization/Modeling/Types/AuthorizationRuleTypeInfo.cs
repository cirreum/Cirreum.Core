namespace Cirreum.Authorization.Modeling.Types;

using Cirreum.Authorization.Modeling.Export;

/// <summary>
/// Internal type used during analysis that includes CLR Type references.
/// Use AuthorizationRuleInfo for serialization/public API.
/// </summary>
public sealed record AuthorizationRuleTypeInfo(
	Type ResourceType,
	Type AuthorizorType,
	string PropertyPath,
	string ValidationLogic,
	string Message,
	string? Condition = null
) {
	/// <summary>
	/// Converts to the serializable AuthorizationRuleInfo type.
	/// </summary>
	public AuthorizationRuleInfo ToRuleInfo() => new(
		ResourceTypeName: this.ResourceType.Name,
		ResourceTypeFullName: this.ResourceType.FullName ?? this.ResourceType.Name,
		AuthorizorTypeName: this.AuthorizorType.Name,
		AuthorizorTypeFullName: this.AuthorizorType.FullName ?? this.AuthorizorType.Name,
		PropertyPath: this.PropertyPath,
		ValidationLogic: this.ValidationLogic,
		Message: this.Message,
		Condition: this.Condition
	);
}
