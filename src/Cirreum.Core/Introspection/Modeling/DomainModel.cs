namespace Cirreum.Introspection.Modeling;

using Cirreum;
using Cirreum.Authorization;
using Cirreum.Authorization.Operations;
using Cirreum.Authorization.Operations.Grants;
using Cirreum.Conductor;
using Cirreum.Introspection.Modeling.Export;
using Cirreum.Introspection.Modeling.Types;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Provides access to all domain resources and their authorization information.
/// This is the single source of truth for resource discovery and authorization rules.
/// Scans all IDomainObject types (both protected and anonymous).
/// </summary>
public class DomainModel() {

	private static readonly Lazy<DomainModel> _instance =
		new(() => new DomainModel());

	/// <summary>
	/// Gets the singleton instance of the AuthorizationRuleProvider.
	/// </summary>
	public static DomainModel Instance => _instance.Value;

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

		// Step 2: Find all IDomainObject types
		var domainResourceTypes = allTypes.Where(IsDomainResource).ToList();

		// Step 3: Find all authorizers and build a lookup by resource type
		var authorizerTypes = allTypes.Where(IsResourceAuthorizer).ToList();
		var authorizersByResource = new Dictionary<Type, Type>();
		foreach (var authorizer in authorizerTypes) {
			var resourceType = GetResourceTypeFromAuthorizer(authorizer);
			if (resourceType != null) {
				authorizersByResource[resourceType] = authorizer;
			}
		}

		// Step 4: Build ResourceInfo for each domain resource
		foreach (var resourceType in domainResourceTypes) {
			var hasAuthorizer = authorizersByResource.TryGetValue(resourceType, out var authorizerType);
			var rules = hasAuthorizer ? ExtractValidationRules(resourceType, authorizerType!) : [];

			// IsAnonymous: not an IAuthorizableObject (doesn't participate in authorization)
			var isAnonymous = !IsAuthorizableResource(resourceType);

			// IsCacheableQuery: is an ICacheableOperation (the query results are cached and returned to prevent re-query)
			var isCacheableQuery = IsCacheableQuery(resourceType);

			// RequiresAuthorization: is an IAuthorizableOperationBase (flows through pipeline, must have authorizer)
			var requiresAuthorization = !isAnonymous && ImplementsAuthorizableOperation(resourceType);

			// Permission metadata: domain feature and required permissions are available
			// for all resources in a *.Domain.* namespace, not just grant-aware ones.
			var grantDomain = RequiredGrantCache.ResolveDomainFeature(resourceType);
			var permissions = RequiredGrantCache.GetFor(resourceType);
			var isSelfScoped = typeof(IGrantableSelfBase).IsAssignableFrom(resourceType);
			var isGranted = isSelfScoped
				|| typeof(IGrantableMutateBase).IsAssignableFrom(resourceType)
				|| typeof(IGrantableLookupBase).IsAssignableFrom(resourceType)
				|| typeof(IGrantableSearchBase).IsAssignableFrom(resourceType);

			var grantableKind = isSelfScoped ? "Self"
				: typeof(IGrantableMutateBase).IsAssignableFrom(resourceType) ? "Mutate"
				: typeof(IGrantableLookupBase).IsAssignableFrom(resourceType) ? "Lookup"
				: typeof(IGrantableSearchBase).IsAssignableFrom(resourceType) ? "Search"
				: (string?)null;

			var resourceInfo = new ResourceTypeInfo(
				ResourceType: resourceType,
				DomainBoundary: GetDomainBoundary(resourceType),
				ResourceKind: GetResourceKind(resourceType),
				IsAnonymous: isAnonymous,
				IsCacheableQuery: isCacheableQuery,
				IsProtected: authorizerType != null,
				RequiresAuthorization: requiresAuthorization,
				AuthorizerType: authorizerType,
				Rules: rules.AsReadOnly(),
				IsGranted: isGranted,
				GrantDomain: grantDomain,
				GrantableKind: grantableKind,
				IsSelfScoped: isSelfScoped,
				Permissions: permissions
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
	/// Gets only authorizable resources (those that implement IAuthorizableObject).
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

		// Find all authorizers
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
		var authorizerTypes = allTypes.Where(IsResourceAuthorizer).ToList();

		// Extract rules from each authorizer
		foreach (var authorizerType in authorizerTypes) {

			// Get the resource type this authorizer protects
			var resourceType = GetResourceTypeFromAuthorizer(authorizerType);
			resourceType ??= typeof(MissingResource);

			// Extract validation rules
			var ruleInfos = ExtractValidationRules(resourceType, authorizerType);
			rules.UnionWith(ruleInfos);

		}

		this._cachedRules = rules.ToList().AsReadOnly();
		return this._cachedRules;

	}

	/// <summary>
	/// Retrieves information about all available authorization policy rules.
	/// </summary>
	/// <remarks>This method aggregates policy rule information from all registered IPolicyValidator
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

		var policyValidators = this._services.GetServices<IPolicyValidator>().ToList();
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
		var resolved = DomainFeatureResolver.Resolve(resourceType);
		if (resolved is null) {
			return "Other";
		}
		// PascalCase for display: "issues" → "Issues"
		return char.ToUpperInvariant(resolved[0]) + resolved[1..];
	}

	private static string GetResourceKind(Type resourceType) {
		var parts = resourceType.Namespace?.Split('.') ?? [];
		return parts.LastOrDefault() ?? "Unknown";
	}

	#endregion

	#region Type Detection Helpers

	/// <summary>
	/// Determines whether the specified type implements the IDomainObject interface.
	/// </summary>
	/// <param name="type">The type to examine for implementation of the IDomainObject interface. Cannot be null.</param>
	/// <returns>true if the specified type implements IDomainObject; otherwise, false.</returns>
	private static bool IsDomainResource(Type type) {
		var interfaces = type.GetInterfaces();
		return interfaces.Any(i => i.Name == nameof(IDomainObject));
	}

	/// <summary>
	/// Determines whether the specified type implements the IAuthorizableObject interface.
	/// </summary>
	/// <param name="type">The type to examine for implementation of the IAuthorizableObject interface.</param>
	/// <returns>true if the specified type implements IAuthorizableObject; otherwise, false.</returns>
	private static bool IsAuthorizableResource(Type type) {
		var interfaces = type.GetInterfaces();
		return interfaces.Any(i => i.Name == nameof(IAuthorizableObject));
	}

	/// <summary>
	/// Determines whether the specified type implements the ICacheableOperation interface.
	/// </summary>
	/// <param name="type">The type to examine for implementation of the ICacheableOperation interface.</param>
	/// <returns>true if the specified type implements ICacheableOperation; otherwise, false.</returns>
	private static bool IsCacheableQuery(Type type) {
		var interfaces = type.GetInterfaces();
		return interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICacheableOperation<>));
	}

	/// <summary>
	/// Determines whether the specified type implements <see cref="IAuthorizableOperationBase"/>.
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <returns>
	/// true if the specified type implements <see cref="IAuthorizableOperationBase"/>; otherwise, false.
	/// </returns>
	/// <remarks>
	/// <see cref="IAuthorizableOperationBase"/> is the single pipeline discriminator for authorization —
	/// all operation marker interfaces (<c>IAuthorizableOperation</c>, <c>IAuthorizableOperation&lt;T&gt;</c>,
	/// and the grant interfaces) inherit from it.
	/// </remarks>
	private static bool ImplementsAuthorizableOperation(Type type) {
		return type.GetInterfaces().Any(i => i.Name == nameof(IAuthorizableOperationBase));
	}

	private static bool IsResourceAuthorizer(Type type) {
		// Check for base class validators
		if (type.BaseType?.IsGenericType == true &&
			type.BaseType.GetGenericTypeDefinition() == typeof(AuthorizerBase<>)) {
			return true;
		}

		// Check for interface-based validators
		return type.GetInterfaces()
			.Any(i => i.IsGenericType &&
					  i.GetGenericTypeDefinition() == typeof(IAuthorizer<>));
	}

	private static Type? GetResourceTypeFromAuthorizer(Type authorizerType) {
		// Try base class first
		if (authorizerType.BaseType?.IsGenericType == true &&
			authorizerType.BaseType.GetGenericTypeDefinition() == typeof(AuthorizerBase<>)) {
			return authorizerType.BaseType.GetGenericArguments()[0];
		}

		// Try interface
		var validatorInterface = authorizerType.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType &&
								 i.GetGenericTypeDefinition() == typeof(IAuthorizer<>));

		return validatorInterface?.GetGenericArguments()[0];
	}

	private static bool IsAttributeBasedPolicy(IPolicyValidator policy) {
		var baseType = policy.GetType().BaseType;
		return baseType != null &&
			   baseType.IsGenericType &&
			   baseType.GetGenericTypeDefinition() == typeof(AttributeValidatorBase<>);
	}

	private static Type? GetTargetAttributeType(IPolicyValidator policy) {
		if (!IsAttributeBasedPolicy(policy)) {
			return null;
		}

		return policy.GetType().BaseType?.GetGenericArguments()[0];
	}

	private static string GetPolicyDescription(IPolicyValidator policy) {
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

		return ToDisplayName((validator.Name ?? validator.GetType().Name.Replace("Validator", "")));
	}

	private static string ToDisplayName(string name) {
		if (string.IsNullOrWhiteSpace(name)) {
			return name;
		}

		// Insert space before uppercase letters that follow lowercase letters
		// or before uppercase letters followed by lowercase (handles acronyms)
		var sb = new System.Text.StringBuilder(name.Length + 10);
		for (var i = 0; i < name.Length; i++) {
			var current = name[i];
			var previous = i > 0 ? name[i - 1] : '\0';
			var next = i < name.Length - 1 ? name[i + 1] : '\0';

			// Insert space before uppercase if:
			// - preceded by a lowercase letter (camelCase boundary)
			// - preceded by uppercase AND followed by lowercase (acronym end: "HTMLParser" → "HTML Parser")
			// - preceded by a digit
			if (i > 0 && char.IsUpper(current)) {
				if (char.IsLower(previous) ||
					char.IsDigit(previous) ||
					(char.IsUpper(previous) && char.IsLower(next))) {
					sb.Append(' ');
				}
			}

			// Insert space before digit sequences that follow letters
			if (i > 0 && char.IsDigit(current) && char.IsLetter(previous)) {
				sb.Append(' ');
			}

			sb.Append(i == 0 ? char.ToUpper(current) : char.ToLower(current));
		}

		return sb.ToString();
	}

	#endregion
}
