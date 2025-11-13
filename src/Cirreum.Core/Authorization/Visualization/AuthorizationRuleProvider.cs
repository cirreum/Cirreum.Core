namespace Cirreum.Authorization.Visualization;

using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Humanizer;
using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Provides access to authorization rule information across the application.
/// Acts as a central service for both analyzers and documenters.
/// </summary>
public class AuthorizationRuleProvider() {

	// Optional: Singleton instance for easy access
	private static readonly Lazy<AuthorizationRuleProvider> _instance =
		new Lazy<AuthorizationRuleProvider>(() => new AuthorizationRuleProvider());

	/// <summary>
	/// Gets the singleton instance of the AuthorizationRuleProvider.
	/// </summary>
	public static AuthorizationRuleProvider Instance => _instance.Value;

	// Optional: Cache the rules to avoid repeated reflection
	private IReadOnlyList<AuthorizationRuleInfo>? _cachedRules;

	/// <summary>
	/// Gets all authorization rules defined in the application.
	/// </summary>
	/// <param name="useCache">Whether to use cached rules or refresh them.</param>
	/// <returns>A read-only list of authorization rule information.</returns>
	public IReadOnlyList<AuthorizationRuleInfo> GetAllRules(bool useCache = true) {

		if (useCache && this._cachedRules != null) {
			return this._cachedRules;
		}

		var rules = new HashSet<AuthorizationRuleInfo>();

		// Find all types that inherit from AuthorizationValidatorBase<>
		var assemblies = Cirreum.AssemblyScanner
			.ScanAssemblies();
		var validatorTypes = assemblies
			.SelectMany(a => {
				try {
					return a.GetTypes();
				} catch {
					return Type.EmptyTypes;
				}
			})
			.Where(t => t.IsClass && !t.IsAbstract &&
						t.BaseType?.IsGenericType == true &&
						t.BaseType.GetGenericTypeDefinition() == typeof(AuthorizationValidatorBase<>));

		foreach (var validatorType in validatorTypes) {
			try {
				// Find the concrete subclass of the generic base type
				var resourceType = validatorType.BaseType?.GetGenericArguments()[0];
				if (resourceType is null) {
					continue;
				}

				// Extract validation rules
				var ruleInfos = ExtractValidationRules(resourceType, validatorType);
				rules.UnionWith(ruleInfos);
			} catch (Exception ex) {
				// Log the exception for debugging
				Debug.WriteLine($"Error processing validator {validatorType.Name}: {ex.Message}");
				// Skip validators that can't be instantiated or processed
			}
		}

		this._cachedRules = rules.ToList().AsReadOnly();
		return this._cachedRules;
	}

	/// <summary>
	/// Gets all authorization rules for a specific resource type.
	/// </summary>
	/// <param name="resourceType">The resource type to get rules for.</param>
	/// <returns>A read-only list of authorization rule information for the specified resource type.</returns>
	public IReadOnlyList<AuthorizationRuleInfo> GetRulesForResourceType(Type resourceType) {
		return this.GetAllRules()
			.Where(r => r.ResourceType == resourceType)
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Gets all authorization rules for a specific validator type.
	/// </summary>
	/// <param name="validatorType">The validator type to get rules for.</param>
	/// <returns>A read-only list of authorization rule information for the specified validator type.</returns>
	public IReadOnlyList<AuthorizationRuleInfo> GetRulesForValidatorType(Type validatorType) {
		return this.GetAllRules()
			.Where(r => r.ValidatorType == validatorType)
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Clears the cached rules, forcing a refresh on the next call to GetAllRules().
	/// </summary>
	public void ClearCache() {
		this._cachedRules = null;
	}

	private static List<AuthorizationRuleInfo> ExtractValidationRules(Type resourceType, Type validatorType) {
		var rules = new List<AuthorizationRuleInfo>();

		// Create an instance of the validator
		var validatorInstance = Activator.CreateInstance(validatorType);

		// Get the descriptor using reflection
		var descriptorMethod = validatorType.GetMethod("CreateDescriptor", BindingFlags.Instance | BindingFlags.Public);
		if (descriptorMethod != null && descriptorMethod.Invoke(validatorInstance, null) is IValidatorDescriptor descriptor) {
			// Get all members with validators
			var membersWithValidators = descriptor.GetMembersWithValidators();

			foreach (var propertyGroup in membersWithValidators) {
				var propertyPath = propertyGroup.Key;

				// Get the rules for this property to check for conditions
				var propertyRules = descriptor.GetRulesForMember(propertyPath);
				var condition = GetConditionDescription(propertyRules);

				// Process each validator for this property
				foreach (var (validator, options) in propertyGroup) {
					if (validator is null) {
						continue;
					}

					// Extract validation logic
					var validationLogic = GetValidationLogicDescription(validator);

					// Get error message
					var message = options.GetUnformattedErrorMessage() ?? "Default error message";

					// Add the rule info
					rules.Add(new AuthorizationRuleInfo(
						resourceType,
						validatorType,
						propertyPath,
						validationLogic,
						message,
						condition
					));
				}
			}

			// Process include rules using the IIncludeRule interface
			foreach (var rule in descriptor.Rules) {
				if (rule is IIncludeRule) {

					// This is an include rule
					rules.Add(new AuthorizationRuleInfo(
						resourceType,
						validatorType,
						rule.PropertyName ?? "AuthorizationContext",
						"Included Validator",
						"References another validator",
						null
					));

					var ruleType = rule.GetType();
					var validatorProperty = ruleType.GetProperty("Validator");
					if (validatorProperty != null) {
						var includedValidator = validatorProperty.GetValue(rule);
						if (includedValidator != null) {
							// You could extract more information about the included validator here
							var includedValidatorType = includedValidator.GetType();
							// Add the type name to the validation logic
							rules[^1] = rules[^1] with {
								ValidationLogic = $"Included Validator: {includedValidatorType.Name}"
							};
						}
					}
				}
			}
		}

		return rules;
	}

	private static string? GetConditionDescription(IEnumerable<IValidationRule> rules) {
		foreach (var rule in rules) {
			// Check if the rule has conditions by examining properties via reflection
			var ruleType = rule.GetType();
			var applyConditionProperty = ruleType.GetProperty("ApplyCondition");
			var asyncApplyConditionProperty = ruleType.GetProperty("AsyncApplyCondition");

			if (applyConditionProperty != null) {
				var applyCondition = applyConditionProperty.GetValue(rule);
				if (applyCondition != null) {
					return "Has When condition";
				}
			}

			if (asyncApplyConditionProperty != null) {
				var asyncApplyCondition = asyncApplyConditionProperty.GetValue(rule);
				if (asyncApplyCondition != null) {
					return "Has async condition";
				}
			}
		}

		return null;
	}

	private static string GetValidationLogicDescription(IPropertyValidator validator) {

		// Basic validators
		if (validator is INotNullValidator) {
			return "Not Null";
		}

		if (validator is INotEmptyValidator) {
			return "Not Empty";
		}

		// Length validators
		if (validator is ILengthValidator lengthVal) {
			if (lengthVal.Max == int.MaxValue) {
				return $"Min Length: {lengthVal.Min}";
			}

			if (lengthVal.Min == 0) {
				return $"Max Length: {lengthVal.Max}";
			}

			return $"Length: {lengthVal.Min}-{lengthVal.Max}";
		}

		// Comparison validators
		if (validator is IComparisonValidator compVal) {
			var comparisonType = compVal.Comparison.ToString();
			var valueToCompare = compVal.ValueToCompare?.ToString() ?? "null";
			return $"{comparisonType} {valueToCompare}";
		}

		// Regex validator
		if (validator is IRegularExpressionValidator regexVal) {
			return $"Regex: {regexVal.Expression}";
		}

		// Email validator
		if (validator is IEmailValidator) {
			return "Email";
		}

		// Predicate validator
		if (validator is IPredicateValidator) {
			return "Custom Predicate";
		}

		// For unknown validators, return the name or type
		return (validator.Name ?? validator.GetType().Name.Replace("Validator", "")).Humanize();

	}

}