namespace Cirreum.Authorization.Modeling.Export;
/// <summary>
/// Represents a domain resource with its authorization information.
/// This is the serializable view of a resource - whether protected or anonymous.
/// </summary>
public sealed record ResourceInfo(
	string ResourceName,
	string ResourceFullName,
	string DomainBoundary,
	string ResourceKind,
	bool IsAnonymous,
	bool IsProtected,
	bool RequiresAuthorization,
	string? AuthorizerName,
	string? AuthorizerFullName,
	IReadOnlyList<AuthorizationRuleInfo> Rules
);