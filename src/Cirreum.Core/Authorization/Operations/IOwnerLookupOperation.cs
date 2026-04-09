namespace Cirreum.Authorization.Operations;

using Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Grant-aware point-lookup. Composes foundation <see cref="IAuthorizableOperation{TResultValue}"/>
/// with the <see cref="IGrantableLookupBase"/> detection surface. Developers implement this
/// single interface for granted lookup queries.
/// </summary>
/// <typeparam name="TResultValue">The type of response returned by the lookup.</typeparam>
public interface IOwnerLookupOperation<out TResultValue>
	: IAuthorizableOperation<TResultValue>, IGrantableLookupBase;
