namespace Cirreum.Conductor;

// ===== Domain Requests =====


/// <summary>
/// 
/// </summary>
public interface IDomainCommand : IAuditableRequest, IAuthorizableRequest;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IDomainCommand<out TResponse> : IAuditableRequest<TResponse>, IAuthorizableRequest<TResponse>;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IDomainQuery<TResponse> : IAuditableRequest<TResponse>, IAuthorizableRequest<TResponse>;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IDomainCacheableQuery<TResponse>
	: IAuditableRequest<TResponse>,
	IAuthorizableRequest<TResponse>,
	ICacheableQuery<TResponse>;
