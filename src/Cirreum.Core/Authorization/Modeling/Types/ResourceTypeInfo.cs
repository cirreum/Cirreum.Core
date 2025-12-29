namespace Cirreum.Authorization.Modeling.Types;

using Cirreum.Authorization.Modeling.Export;

/// <summary>
/// Internal type used during analysis that includes CLR Type references.
/// Use ResourceInfo for serialization/public API.
/// </summary>
public sealed record ResourceTypeInfo(
	Type ResourceType,
	string DomainBoundary,
	string ResourceKind,
	bool IsAnonymous,
	bool IsAuditable,
	bool IsCacheableQuery,
	bool IsProtected,
	bool RequiresAuthorization,
	Type? AuthorizerType,
	IReadOnlyList<AuthorizationRuleTypeInfo> Rules
) {
	/// <summary>
	/// Converts to the serializable ResourceInfo type.
	/// </summary>
	public ResourceInfo ToResourceInfo() => new(
		ResourceName: this.ResourceType.Name,
		ResourceFullName: this.ResourceType.FullName ?? this.ResourceType.Name,
		DomainBoundary: this.DomainBoundary,
		ResourceKind: this.ResourceKind,
		IsAnonymous: this.IsAnonymous,
		IsAuditable: this.IsAuditable,
		IsCacheableQuery: this.IsCacheableQuery,
		IsProtected: this.IsProtected,
		RequiresAuthorization: this.RequiresAuthorization,
		AuthorizerName: this.AuthorizerType?.Name,
		AuthorizerFullName: this.AuthorizerType?.FullName,
		Rules: [.. this.Rules.Select(r => r.ToRuleInfo())]
	);
}