namespace Cirreum.Authorization.Operations;

using Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Grant-aware cross-owner query. Composes foundation <see cref="IAuthorizableOperation{TResponse}"/>
/// with the <see cref="IGrantableSearchBase"/> detection surface. Developers implement this
/// single interface for granted cross-owner queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the search operation.</typeparam>
public interface IOwnerSearchOperation<out TResponse>
	: IAuthorizableOperation<TResponse>, IGrantableSearchBase;
