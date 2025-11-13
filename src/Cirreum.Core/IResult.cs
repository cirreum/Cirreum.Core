namespace Cirreum;

/// <summary>
/// Represents the result of an operation, providing information about its success or failure.
/// </summary>
/// <remarks>Implementations of this interface typically include additional details about the operation, such as
/// error information or result data. Use the <see cref="IsSuccess"/> property to determine whether the operation was
/// successful before accessing further result details.</remarks>
public interface IResult {
	/// <summary>
	/// Gets a value indicating whether the operation completed successfully.
	/// </summary>
	bool IsSuccess { get; }
}