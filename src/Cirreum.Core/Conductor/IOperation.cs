namespace Cirreum.Conductor;

/// <summary>
/// Marker interface to represent an operation with a void response
/// </summary>
public interface IOperation : IBaseOperation;

/// <summary>
/// Marker interface to represent an operation with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IOperation<out TResponse> : IBaseOperation;

/// <summary>
/// Allows for generic type constraints of objects implementing IOperation or IOperation{TResponse}
/// </summary>
public interface IBaseOperation : IDomainObject;