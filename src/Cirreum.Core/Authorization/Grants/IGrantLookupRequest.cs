namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Grant-aware point-lookup. Composes foundation <see cref="IAuthorizableRequest{TResponse}"/>
/// with the <see cref="IGrantableLookupBase"/> detection surface. Developers implement this
/// single interface for granted lookup queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the lookup.</typeparam>
public interface IGrantLookupRequest<out TResponse>
	: IAuthorizableRequest<TResponse>, IGrantableLookupBase;
