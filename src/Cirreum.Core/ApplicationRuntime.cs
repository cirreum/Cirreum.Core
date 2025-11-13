namespace Cirreum;

/// <summary>
/// Defines the attributes of this application runtime.
/// </summary>
public class ApplicationRuntime {

	/// <summary>
	/// Gets or sets the applications <see cref="ApplicationRuntimeType"/>.
	/// </summary>
	public ApplicationRuntimeType RuntimeType { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public static ApplicationRuntime Current { get; set; } = new();

}