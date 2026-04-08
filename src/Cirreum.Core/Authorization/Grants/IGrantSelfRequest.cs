namespace Cirreum.Authorization.Grants;

using Cirreum.Conductor;

/// <summary>
/// Self-scoped mutation with no response. Developers implement this for void
/// operations on the caller's own resources.
/// </summary>
public interface IGrantMutateSelfRequest
	: IAuthorizableRequest, IGrantableSelfBase;

/// <summary>
/// Self-scoped mutation with a response. Developers implement this for
/// operations on the caller's own resources that return a value.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the mutation.</typeparam>
public interface IGrantMutateSelfRequest<out TResponse>
	: IAuthorizableRequest<TResponse>, IGrantableSelfBase;

/// <summary>
/// Self-scoped lookup with a response. Developers implement this for
/// read operations on the caller's own resources.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the lookup.</typeparam>
public interface IGrantLookupSelfRequest<out TResponse>
	: IAuthorizableRequest<TResponse>, IGrantableSelfBase;
