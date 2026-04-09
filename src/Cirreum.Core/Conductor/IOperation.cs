namespace Cirreum.Conductor;

/// <summary>
/// Marker interface to represent an operation with a void response
/// </summary>
public interface IOperation : IBaseOperation;

/// <summary>
/// Marker interface to represent an operation with a response
/// </summary>
/// <typeparam name="TResultValue">Response type</typeparam>
public interface IOperation<out TResultValue> : IBaseOperation;

/// <summary>
/// Allows for generic type constraints of objects implementing IOperation or IOperation{TResultValue}
/// </summary>
public interface IBaseOperation : IDomainObject;