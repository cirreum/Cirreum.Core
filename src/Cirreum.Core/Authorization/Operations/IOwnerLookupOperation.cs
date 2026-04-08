namespace Cirreum.Authorization.Operations;

using Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Grant-aware point-lookup. Composes foundation <see cref="IAuthorizableOperation{TResponse}"/>
/// with the <see cref="IGrantableLookupBase"/> detection surface. Developers implement this
/// single interface for granted lookup queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the lookup.</typeparam>
public interface IOwnerLookupOperation<out TResponse>
	: IAuthorizableOperation<TResponse>, IGrantableLookupBase;
