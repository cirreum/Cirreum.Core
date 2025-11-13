namespace Cirreum.Exceptions;

using FluentValidation.Results;

/// <summary>
/// Contains related details associated with an unhandled exception.
/// </summary>
public sealed record FailureModel {

	/// <summary>
	/// Constructor
	/// </summary>
	public FailureModel() {

	}

	/// <summary>
	/// The name of the property.
	/// </summary>
	public string PropertyName { get; init; } = "";
	/// <summary>
	/// The error message
	/// </summary>
	public string ErrorMessage { get; init; } = "";
	/// <summary>
	/// The property value that caused the failure.
	/// </summary>
	public object AttemptedValue { get; init; } = new object();
	/// <summary>
	/// Custom state associated with the failure.
	/// </summary>
	public object CustomState { get; init; } = new object();
	/// <summary>
	/// Custom severity level associated with the failure.
	/// </summary>
	public FailureSeverity Severity { get; init; }
	/// <summary>
	/// Gets or sets the error code.
	/// </summary>
	public string ErrorCode { get; init; } = "";

	/// <summary>
	/// Map from a <see cref="ValidationFailure"/> instance.
	/// </summary>
	/// <param name="failure">A <see cref="ValidationFailure"/> instance</param>
	/// <returns>A new <see cref="FailureModel"/> instance mapped from a <see cref="ValidationFailure"/>.</returns>
	public static FailureModel FromFluentValidationFailure(ValidationFailure failure) {

		return new FailureModel {
			PropertyName = failure.PropertyName,
			ErrorMessage = failure.ErrorMessage,
			ErrorCode = failure.ErrorCode,
			AttemptedValue = failure.AttemptedValue,
			CustomState = failure.CustomState,
			Severity = (FailureSeverity)(int)failure.Severity
		};

	}

}