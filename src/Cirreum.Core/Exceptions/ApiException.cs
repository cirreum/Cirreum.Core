namespace Cirreum.Exceptions;

/// <summary>
/// An exception thrown when a client receives an exception from an api call.
/// </summary>
/// <param name="exceptionModel"></param>
/// <remarks>
/// <para>
/// Works the core <see cref="ExceptionModel"/> and exception handling.
/// </para>
/// </remarks>
public class ApiException(ExceptionModel exceptionModel) : Exception(exceptionModel.Detail) {

	/// <summary>
	/// Gets the <see cref="ExceptionModel"/> for the error.
	/// </summary>
	public ExceptionModel Model { get; init; } = exceptionModel;

	/// <summary>
	/// Gets the <see cref="ExceptionModel.Title"/> for the error.
	/// </summary>
	public string Title {
		get {
			return this.Model.Title ?? "Unknown Error";
		}
	}

	/// <summary>
	/// Gets the <see cref="ExceptionModel.Failures"/> for the error.
	/// </summary>
	public List<FailureModel> Failures {
		get {
			return [.. this.Model.Failures];
		}
	}

}