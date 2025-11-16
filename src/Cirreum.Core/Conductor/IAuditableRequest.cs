namespace Cirreum.Conductor;

// ===== Auditable Requests =====

/// <summary>
/// Marker interface to represent an auditable request with a void response
/// </summary>
public interface IAuditableRequest : IRequest, IAuditableRequestBase;

/// <summary>
/// Marker interface to represent an auditable request with a response
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IAuditableRequest<out TResponse> : IRequest<TResponse>, IAuditableRequestBase;

/// <summary>
/// Marker interface to allow requests to be audited.
/// </summary>
/// <remarks>
/// <para>
/// Normally you would not implement this interface directly.
/// </para>
/// <para>
/// See <see cref="IAuditableRequest"/> and <see cref="IAuditableRequest{TResponse}"/>
/// for the interfaces that should be implemented.
/// </para>
/// </remarks>
public interface IAuditableRequestBase : IBaseRequest;