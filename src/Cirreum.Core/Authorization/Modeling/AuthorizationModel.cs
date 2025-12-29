namespace Cirreum.Authorization.Modeling;

using Cirreum;
using Cirreum.Authorization;
using Cirreum.Authorization.Modeling.Export;
using Cirreum.Authorization.Modeling.Types;
using Cirreum.Conductor;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Provides access to all domain resources and their authorization information.
/// This is the single source of truth for resource discovery and authorization rules.
/// Scans ALL IRequest types (both protected and anonymous).
/// </summary>
public class AuthorizationModel() {

	private static readonly Lazy<AuthorizationModel> _instance =
		new(() => new AuthorizationModel());

	/// <summary>
	/// Gets the singleton instance of the AuthorizationRuleProvider.
	/// </summary>
	public static AuthorizationModel Instance => _instance.Value;

	// Cached data
	private IReadOnlyList<ResourceTypeInfo>? _cachedResources;
	private IReadOnlyList<AuthorizationRuleTypeInfo>? _cachedRules;
	private IReadOnlyList<PolicyRuleTypeInfo>? _cachedPolicyRules;
	private DomainCatalog? _cachedCatalog;
	private IServiceProvider? _services;

	public void Initialize(IServiceProvider services) {
		this._services = services;
		this.ClearCache();
	}

	/// <summary>
	/// Gets the catalog organized by Domain Boundary -> Resource Kind -> Resource.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is useful for visualization and analysis of the overall authorization structure.
	/// </para>
	/// <para>
	/// The <see cref="DomainCatalog"/> is serializable and can be exported to JSON or other formats for reporting.
	/// </para>
	/// </remarks>
	public DomainCatalog GetCatalog(bool useCache = true) {
		if (useCache && this._cachedCatalog != null) {
			return this._cachedCatalog;
		}

		var allResources = this
			.GetAllResources(useCache)
			.Select(r => r.ToResourceInfo())
			.ToList();
		this._cachedCatalog = DomainCatalog.Build(allResources);
		return this._cachedCatalog;

	}

	#region Domain Resources

	/// <summary>
	/// Gets all domain resources (Commands, Queries, Events) with their authorization information.
	/// Includes both protected and anonymous resources.
	/// </summary>
	public IReadOnlyList<ResourceTypeInfo> GetAllResources(bool useCache = true) {
		if (useCache && this._cachedResources != null) {
			return this._cachedResources;
		}

		var resources = new List<ResourceTypeInfo>();

		// Step 1: Scan all assemblies for types
		var assemblies = Cirreum.AssemblyScanner.ScanAssemblies();
		var allTypes = assemblies
			.SelectMany(a => {
				try {
					return a.GetTypes();
				} catch {
					return Type.EmptyTypes;
				}
			})
			.Where(t => t.IsClass && !t.IsAbstract)
			.ToList();

		// Step 2: Find all IDomainResource types
		var domainResourceTypes = allTypes.Where(IsDomainResource).ToList();

		// Step 3: Find all authorizors and build a lookup by resource type
		var authorizorTypes = allTypes.Where(IsAuthorizationValidator).ToList();
		var authorizorsByResource = new Dictionary<Type, Type>();
		foreach (var authorizer in authorizorTypes) {
			var resourceType = GetResourceTypeFromAuthorizor(authorizer);
			if (resourceType != null) {
				authorizorsByResource[resourceType] = authorizer;
			}
		}

		// Step 4: Build ResourceInfo for each domain resource
		foreach (var resourceType in domainResourceTypes) {
			var hasAuthorizer = authorizorsByResource.TryGetValue(resourceType, out var authorizorType);
			var rules = hasAuthorizer ? ExtractValidationRules(resourceType, authorizorType!) : [];

			// IsAnonymous: not an IAuthorizableResource (doesn't participate in authorization)
			var isAnonymous = !IsAuthorizableResource(resourceType);

			// RequiresAuthorization: is an IAuthorizableRequest (flows through pipeline, must have authorizer)
			var requiresAuthorization = !isAnonymous && ImplementsAuthorizableRequest(resourceType);

			var resourceInfo = new ResourceTypeInfo(
				ResourceType: resourceType,
				DomainBoundary: GetDomainBoundary(resourceType),
				ResourceKind: GetResourceKind(resourceType),
				IsAnonymous: isAnonymous,
				IsProtected: authorizorType != null,
				RequiresAuthorization: requiresAuthorization,
				AuthorizerType: authorizorType,
				Rules: rules.AsReadOnly()
			);

			resources.Add(resourceInfo);
		}

		this._cachedResources = resources.AsReadOnly();
		return this._cachedResources;

	}

	/// <summary>
	/// Gets only anonymous resources (those that don't require authorization).
	/// </summary>
	public IReadOnlyList<ResourceTypeInfo> GetAnonymousResources(bool useCache = true) {
		return this.GetAllResources(useCache).Where(r => r.IsAnonymous).ToList().AsReadOnly();
	}

	/// <summary>
	/// Gets only authorizable resources (those that implement IAuthorizableResource).
	/// </summary>
	public IReadOnlyList<ResourceTypeInfo> GetAuthorizableResources(bool useCache = true) {
		return this.GetAllResources(useCache).Where(r => !r.IsAnonymous).ToList().AsReadOnly();
	}

	#endregion

	#region Authorization/Policy Rules

	/// <summary>
	/// Gets all authorization rules defined in the application.
	/// </summary>
	public IReadOnlyList<AuthorizationRuleTypeInfo> GetAuthorizationRules(bool useCache = true) {
		if (useCache && this._cachedRules != null) {
			return this._cachedRules;
		}

		var rules = new HashSet<AuthorizationRuleTypeInfo>();

		// Find all authorizors
		var assemblies = Cirreum.AssemblyScanner.ScanAssemblies();
		var allTypes = assemblies
			.SelectMany(a => {
				try {
					return a.GetTypes();
				} catch {
					return Type.EmptyTypes;
				}
			})
			.Where(t => t.IsClass && !t.IsAbstract)
			.ToList();
		var authorizorTypes = allTypes.Where(IsAuthorizationValidator).ToList();

		// Extract rules from each authorizor
		foreach (var authorizorType in authorizorTypes) {

			// Get the resource type this authorizor protects
			var resourceType = GetResourceTypeFromAuthorizor(authorizorType);
			resourceType ??= typeof(MissingResource);

			// Extract validation rules
			var ruleInfos = ExtractValidationRules(resourceType, authorizorType);
			rules.UnionWith(ruleInfos);

		}

		this._cachedRules = rules.ToList().AsReadOnly();
		return this._cachedRules;

	}

	/// <summary>
	/// Retrieves information about all available authorization policy rules.
	/// </summary>
	/// <remarks>This method aggregates policy rule information from all registered IAuthorizationPolicyValidator
	/// services. If caching is enabled and cached data is available, the method returns the cached results to improve
	/// performance. Otherwise, it queries the underlying services to obtain the latest policy rules.</remarks>
	/// <param name="useCache">true to return cached policy rule information if available; false to force retrieval of the latest policy rules
	/// from the underlying services.</param>
	/// <returns>A read-only list of PolicyRuleTypeInfo objects representing all discovered authorization policy rules. Returns an
	/// empty list if no policy rules are available.</returns>
	public IReadOnlyList<PolicyRuleTypeInfo> GetPolicyRules(bool useCache = true) {
		if (useCache && this._cachedPolicyRules != null) {
			return this._cachedPolicyRules;
		}

		if (this._services == null) {
			return new List<PolicyRuleTypeInfo>().AsReadOnly();
		}

		var policyValidators = this._services.GetServices<IAuthorizationPolicyValidator>().ToList();
		var policyRules = new List<PolicyRuleTypeInfo>();

		foreach (var policy in policyValidators) {
			var ruleInfo = new PolicyRuleTypeInfo(
				PolicyName: policy.PolicyName,
				PolicyType: policy.GetType(),
				Order: policy.Order,
				SupportedRuntimeTypes: policy.SupportedRuntimeTypes,
				IsAttributeBased: IsAttributeBasedPolicy(policy),
				TargetAttributeType: GetTargetAttributeType(policy),
				Description: GetPolicyDescription(policy)
			);
			policyRules.Add(ruleInfo);
		}

		this._cachedPolicyRules = policyRules.AsReadOnly();
		return this._cachedPolicyRules;
	}

	/// <summary>
	/// Retrieves a combined view of resource and policy authorization rules, including the total number of protection
	/// points.
	/// </summary>
	/// <param name="useCache">true to use cached rule data if available; false to force retrieval of the latest rules.</param>
	/// <returns>A CombinedAuthorizationInfo object containing the current resource rules, policy rules, and the total count of
	/// protection points.</returns>
	public CombinedRuleTypeInfo GetAllRules(bool useCache = true) {
		var resourceRules = this.GetAuthorizationRules(useCache);
		var policyRules = this.GetPolicyRules(useCache);

		return new CombinedRuleTypeInfo(
			ResourceRules: resourceRules,
			PolicyRules: policyRules,
			TotalRules: resourceRules.Count + policyRules.Count
		);
	}

	#endregion

	#region Cache Management

	/// <summary>
	/// Clears all cached data, forcing a refresh on the next call.
	/// </summary>
	public void ClearCache() {
		this._cachedResources = null;
		this._cachedRules = null;
		this._cachedPolicyRules = null;
		this._cachedCatalog = null;
	}

	#endregion

	#region Boundary/Kind Extraction

	private static string GetDomainBoundary(Type resourceType) {
		var parts = resourceType.Namespace?.Split('.') ?? [];
		var domainIndex = Array.IndexOf(parts, "Domain");
		return domainIndex >= 0 && parts.Length > domainIndex + 1
			? parts[domainIndex + 1]
			: "Other";
	}

	private static string GetResourceKind(Type resourceType) {
		var parts = resourceType.Namespace?.Split('.') ?? [];
		return parts.LastOrDefault() ?? "Unknown";
	}

	#endregion

	#region Type Detection Helpers

	/// <summary>
	/// Determines whether the specified type implements the IDomainResource interface.
	/// </summary>
	/// <param name="type">The type to examine for implementation of the IDomainResource interface. Cannot be null.</param>
	/// <returns>true if the specified type implements IDomainResource; otherwise, false.</returns>
	private static bool IsDomainResource(Type type) {
		var interfaces = type.GetInterfaces();
		return interfaces.Any(i => i.Name == nameof(IDomainResource));
	}

	/// <summary>
	/// Determines whether the specified type implements the IAuthorizableResource interface.
	/// </summary>
	/// <param name="type">The type to examine for implementation of the IAuthorizableResource interface.</param>
	/// <returns>true if the specified type implements IAuthorizableResource; otherwise, false.</returns>
	private static bool IsAuthorizableResource(Type type) {
		var interfaces = type.GetInterfaces();
		return interfaces.Any(i => i.Name == nameof(IAuthorizableResource));
	}

	/// <summary>
	/// Determines whether the specified type implements an authorizable request interface.
	/// </summary>
	/// <param name="type">The type to inspect for implementation of an authorizable request interface.</param>
	/// <returns>true if the specified type implements IAuthorizableRequestBase, IAuthorizableRequest,
	/// or IAuthorizableRequest{T}; otherwise, false.
	/// </returns>
	/// <remarks>This method checks for implementation of any of the recognized authorizable request marker
	/// interfaces, including generic variants. Use this to identify types that support authorization in the request
	/// pipeline.
	/// </remarks>
	private static bool ImplementsAuthorizableRequest(Type type) {
		// Check if the type implements any of the request interfaces
		return type.GetInterfaces().Any(i =>
			i.Name == nameof(IAuthorizableRequestBase) ||
			i.Name == nameof(IAuthorizableRequest) ||
			(i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuthorizableRequest<>)));
	}

	private static bool IsAuthorizationValidator(Type type) {
		// Check for base class validators
		if (type.BaseType?.IsGenericType == true &&
			type.BaseType.GetGenericTypeDefinition() == typeof(AuthorizationValidatorBase<>)) {
			return true;
		}

		// Check for interface-based validators
		return type.GetInterfaces()
			.Any(i => i.IsGenericType &&
					  i.GetGenericTypeDefinition() == typeof(IAuthorizationResourceValidator<>));
	}

	private static Type? GetResourceTypeFromAuthorizor(Type authorizorType) {
		// Try base class first
		if (authorizorType.BaseType?.IsGenericType == true &&
			authorizorType.BaseType.GetGenericTypeDefinition() == typeof(AuthorizationValidatorBase<>)) {
			return authorizorType.BaseType.GetGenericArguments()[0];
		}

		// Try interface
		var validatorInterface = authorizorType.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType &&
								 i.GetGenericTypeDefinition() == typeof(IAuthorizationResourceValidator<>));

		return validatorInterface?.GetGenericArguments()[0];
	}

	private static bool IsAttributeBasedPolicy(IAuthorizationPolicyValidator policy) {
		var baseType = policy.GetType().BaseType;
		return baseType != null &&
			   baseType.IsGenericType &&
			   baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>);
	}

	private static Type? GetTargetAttributeType(IAuthorizationPolicyValidator policy) {
		if (!IsAttributeBasedPolicy(policy)) {
			return null;
		}

		return policy.GetType().BaseType?.GetGenericArguments()[0];
	}

	private static string GetPolicyDescription(IAuthorizationPolicyValidator policy) {
		var type = policy.GetType();
		var description = type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
			.Cast<System.ComponentModel.DescriptionAttribute>()
			.FirstOrDefault()?.Description;

		return description ?? $"Policy validator: {policy.PolicyName}";
	}

	#endregion

	#region Rule Extraction

	private static List<AuthorizationRuleTypeInfo> ExtractValidationRules(Type resourceType, Type validatorType) {
		var rules = new List<AuthorizationRuleTypeInfo>();

		try {
			// Create an instance of the validator
			var validatorInstance = Activator.CreateInstance(validatorType);

			// Get the descriptor using reflection
			var descriptorMethod = validatorType.GetMethod("CreateDescriptor", BindingFlags.Instance | BindingFlags.Public);
			if (descriptorMethod != null && descriptorMethod.Invoke(validatorInstance, null) is IValidatorDescriptor descriptor) {
				// Get all members with validators
				var membersWithValidators = descriptor.GetMembersWithValidators();

				foreach (var propertyGroup in membersWithValidators) {
					var propertyPath = propertyGroup.Key;

					foreach (var (validator, options) in propertyGroup) {
						if (validator is null) {
							continue;
						}

						var validationLogic = GetValidationLogicDescription(validator);
						var message = options.GetUnformattedErrorMessage() ?? "Default error message";

						rules.Add(new AuthorizationRuleTypeInfo(
							resourceType,
							validatorType,
							propertyPath,
							validationLogic,
							message
						));
					}
				}

				// Process include rules
				foreach (var rule in descriptor.Rules) {
					if (rule is IIncludeRule) {
						rules.Add(new AuthorizationRuleTypeInfo(
							resourceType,
							validatorType,
							rule.PropertyName ?? "AuthorizationContext",
							"Included Validator",
							"References another validator"
						));

						var ruleType = rule.GetType();
						var validatorProperty = ruleType.GetProperty("Validator");
						if (validatorProperty != null) {
							var includedValidator = validatorProperty.GetValue(rule);
							if (includedValidator != null) {
								var includedValidatorType = includedValidator.GetType();
								rules[^1] = rules[^1] with {
									ValidationLogic = $"Included Validator: {includedValidatorType.Name}"
								};
							}
						}
					}
				}
			}
		} catch (Exception ex) {
			Debug.WriteLine($"Error extracting rules from validator {validatorType.Name}: {ex.Message}");
		}

		return rules;
	}

	private static string GetValidationLogicDescription(IPropertyValidator validator) {
		if (validator is INotNullValidator) {
			return "Not Null";
		}

		if (validator is INotEmptyValidator) {
			return "Not Empty";
		}

		if (validator is ILengthValidator lengthVal) {
			if (lengthVal.Max == int.MaxValue) {
				return $"Min Length: {lengthVal.Min}";
			}

			if (lengthVal.Min == 0) {
				return $"Max Length: {lengthVal.Max}";
			}

			return $"Length: {lengthVal.Min}-{lengthVal.Max}";
		}

		if (validator is IComparisonValidator compVal) {
			var comparisonType = compVal.Comparison.ToString();
			var valueToCompare = compVal.ValueToCompare?.ToString() ?? "null";
			return $"{comparisonType} {valueToCompare}";
		}

		if (validator is IRegularExpressionValidator regexVal) {
			return $"Regex: {regexVal.Expression}";
		}

		if (validator is IEmailValidator) {
			return "Email";
		}

		if (validator is IPredicateValidator) {
			return "Custom Predicate";
		}

		return (validator.Name ?? validator.GetType().Name.Replace("Validator", "")).Humanize();
	}

	#endregion
}
