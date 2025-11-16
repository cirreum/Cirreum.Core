namespace Cirreum;

/// <summary>
/// Contains information regarding the domain environment.
/// </summary>
public interface IDomainEnvironment {
	/// <summary>
	/// Gets the name of the application.
	/// </summary>
	string ApplicationName { get; }
	/// <summary>
	/// Gets the name of the current application environment.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The environment name typically indicates the runtime context, such as "Development", "Staging", or
	/// "Production". This value can be used to configure application behavior based on the environment.
	/// </para>
	/// </remarks>
	string EnvironmentName { get; }
	/// <summary>
	/// Gets the applications <see cref="DomainRuntimeType"/>.
	/// </summary>
	public DomainRuntimeType RuntimeType { get; }
}