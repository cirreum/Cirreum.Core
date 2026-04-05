namespace Cirreum.Authorization;

/// <summary>
/// Zero-override concrete of <see cref="OwnerScopeEvaluatorBase"/>. Registered
/// automatically when the configured <see cref="IApplicationUser"/> type
/// participates in owner scoping (implements <see cref="IOwnedApplicationUser"/>).
/// </summary>
internal sealed class DefaultOwnerScopeEvaluator : OwnerScopeEvaluatorBase;
