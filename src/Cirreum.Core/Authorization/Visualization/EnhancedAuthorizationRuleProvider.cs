namespace Cirreum.Authorization.Visualization;

using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Enhanced provider that includes both resource validators and policy validators.
/// </summary>
public class EnhancedAuthorizationRuleProvider {

	private static readonly Lazy<EnhancedAuthorizationRuleProvider> _instance =
		new(() => new EnhancedAuthorizationRuleProvider());

	public static EnhancedAuthorizationRuleProvider Instance => _instance.Value;

	private IReadOnlyList<PolicyRuleInfo>? _cachedPolicyRules;
	private IServiceProvider? _services;

	public void Initialize(IServiceProvider services) {
		this._services = services;
		this.ClearCache();
	}

	public static IReadOnlyList<AuthorizationRuleInfo>
		GetAllResourceRules(bool useCache = true)
			=> AuthorizationRuleProvider.Instance.GetAllRules(useCache);

	public IReadOnlyList<PolicyRuleInfo> GetAllPolicyRules(bool useCache = true) {
		if (useCache && this._cachedPolicyRules != null) {
			return this._cachedPolicyRules;
		}

		if (this._services == null) {
			return new List<PolicyRuleInfo>().AsReadOnly();
		}

		var policyValidators = this._services.GetServices<IAuthorizationPolicyValidator>().ToList();
		var policyRules = new List<PolicyRuleInfo>();

		foreach (var policy in policyValidators) {
			var ruleInfo = new PolicyRuleInfo(
				PolicyName: policy.PolicyName,
				ValidatorType: policy.GetType(),
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

	public CombinedAuthorizationInfo GetCombinedAuthorizationInfo(bool useCache = true) {
		var resourceRules = GetAllResourceRules(useCache);
		var policyRules = this.GetAllPolicyRules(useCache);

		return new CombinedAuthorizationInfo(
			ResourceRules: resourceRules,
			PolicyRules: policyRules,
			TotalProtectionPoints: resourceRules.Count + policyRules.Count
		);
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

		// Try to get description from XML comments or attributes
		var description = type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
			.Cast<System.ComponentModel.DescriptionAttribute>()
			.FirstOrDefault()?.Description;

		return description ?? $"Policy validator: {policy.PolicyName}";
	}

	public void ClearCache() {
		this._cachedPolicyRules = null;
		AuthorizationRuleProvider.Instance.ClearCache();
	}

}