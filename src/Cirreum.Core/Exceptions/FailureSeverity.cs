namespace Cirreum.Exceptions;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// Specifies the severity of a failure (Error, Warning or Informational).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FailureSeverity>))]
public enum FailureSeverity {
	/// <summary>
	/// Error
	/// </summary>
	[Description("The failure is from an Error.")]
	Error = 0,
	/// <summary>
	/// Warning
	/// </summary>
	[Description("The failure is a Warning notice.")]
	Warning = 1,
	/// <summary>
	/// Info
	/// </summary>
	[Description("The failure is Informational only.")]
	Info = 2
}