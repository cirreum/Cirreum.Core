namespace Cirreum.Authorization.Modeling.Types;

using Cirreum.Authorization.Modeling.Export;
using Cirreum.Authorization;

/// <summary>
/// Internal type used during analysis that includes CLR Type references.
/// Use ResourceInfo for serialization/public API.
/// </summary>
public sealed record ResourceTypeInfo(
	Type ResourceType,
	string DomainBoundary,
	string ResourceKind,
	bool IsAnonymous,
	bool IsCacheableQuery,
	bool IsProtected,
	bool RequiresAuthorization,
	Type? AuthorizerType,
	IReadOnlyList<AuthorizationRuleTypeInfo> Rules,
	bool IsGranted = false,
	string? GrantDomain = null,
	IReadOnlyList<Permission>? RequiredPermissions = null
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
		IsCacheableQuery: this.IsCacheableQuery,
		IsProtected: this.IsProtected,
		RequiresAuthorization: this.RequiresAuthorization,
		AuthorizerName: this.AuthorizerType?.Name,
		AuthorizerFullName: this.AuthorizerType?.FullName,
		Rules: [.. this.Rules.Select(r => r.ToRuleInfo())],
		IsGranted: this.IsGranted,
		GrantDomain: this.GrantDomain,
		RequiredPermissions: this.RequiredPermissions is not null
			? [.. this.RequiredPermissions.Select(p => p.ToString())]
			: []
	);
}