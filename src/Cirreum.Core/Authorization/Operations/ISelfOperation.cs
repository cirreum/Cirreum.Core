namespace Cirreum.Authorization.Operations;

using Cirreum.Authorization.Operations.Grants;

/// <summary>
/// Self-scoped mutation with no response. Developers implement this for void
/// operations on the caller's own resources.
/// </summary>
public interface ISelfMutateOperation
	: IAuthorizableOperation, IGrantableSelfBase;

/// <summary>
/// Self-scoped mutation with a response. Developers implement this for
/// operations on the caller's own resources that return a value.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the mutation.</typeparam>
public interface ISelfMutateOperation<out TResponse>
	: IAuthorizableOperation<TResponse>, IGrantableSelfBase;

/// <summary>
/// Self-scoped lookup with a response. Developers implement this for
/// read operations on the caller's own resources.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the lookup.</typeparam>
public interface ISelfLookupOperation<out TResponse>
	: IAuthorizableOperation<TResponse>, IGrantableSelfBase;
