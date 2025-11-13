namespace Cirreum.Authorization.Visualization;

/// <summary>
/// Represents detailed information about an authorization rule used for visualization or inspection.
/// </summary>
/// <param name="ResourceType">The type of resource being authorized.</param>
/// <param name="ValidatorType">The type of validator implementing the authorization rule.</param>
/// <param name="PropertyPath">The path to the property being validated.</param>
/// <param name="ValidationLogic">The description of the validation logic being applied.</param>
/// <param name="Message">The error message displayed when validation fails.</param>
/// <param name="Condition">Optional conditions (When/Unless) under which the rule is applied.</param>
public record AuthorizationRuleInfo(
	Type ResourceType,
	Type ValidatorType,
	string PropertyPath,        // The property being validated
	string ValidationLogic,     // The actual validation being performed
	string Message,             // Error message
	string? Condition = null    // Any When/Unless conditions
);