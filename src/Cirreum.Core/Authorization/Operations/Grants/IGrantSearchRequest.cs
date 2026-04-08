namespace Cirreum.Authorization.Operations.Grants;

using Cirreum.Conductor;

/// <summary>
/// Grant-aware cross-owner query. Composes foundation <see cref="IAuthorizableRequest{TResponse}"/>
/// with the <see cref="IGrantableSearchBase"/> detection surface. Developers implement this
/// single interface for granted cross-owner queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the search operation.</typeparam>
public interface IGrantSearchRequest<out TResponse>
	: IAuthorizableRequest<TResponse>, IGrantableSearchBase;
