namespace Cirreum.Conductor;

using Cirreum.Authorization;

/// <summary>
/// Owner-scoped authorized read operation (query).
/// OwnerId rules are enforced based on the caller's <see cref="Security.AccessScope"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
public interface IAuthorizableOwnerScopedQuery<out TResponse>
	: IAuthorizableOwnerScopedResource, IAuthorizableQuery<TResponse>;
