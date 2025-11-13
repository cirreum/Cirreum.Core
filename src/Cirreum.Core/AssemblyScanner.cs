namespace Cirreum;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Scans assemblies in the <see cref="AppDomain.CurrentDomain"/>, excluding common 
/// .NET, Microsoft, and third-party libraries.
/// </summary>
public static class AssemblyScanner {

	/// <summary>
	/// Scans and returns all assemblies in the current application domain, 
	/// excluding assemblies with predefined prefixes.
	/// </summary>
	/// <returns>An enumerable collection of non-excluded assemblies.</returns>
	/// <remarks>
	/// Assemblies with certain prefixes (e.g., "System", "Microsoft", "Azure") 
	/// are ignored to prevent scanning unnecessary dependencies.
	/// 
	/// <para><b>Exception Handling:</b></para>
	/// The following exceptions are caught and ignored to ensure scanning continues smoothly:
	/// <list type="bullet">
	///   <item><description><see cref="ReflectionTypeLoadException"/> - If an assembly cannot be loaded properly.</description></item>
	///   <item><description><see cref="FileNotFoundException"/> - If an assembly dependency is missing.</description></item>
	///   <item><description><see cref="BadImageFormatException"/> - If an assembly is not a valid .NET assembly.</description></item>
	/// </list>
	/// </remarks>
	public static IEnumerable<Assembly> ScanAssemblies() => ScanAssemblies(null, false);

	/// <summary>
	/// Scans and returns all assemblies in the current application domain, 
	/// allowing the caller to specify additional exclusions.
	/// </summary>
	/// <param name="customExclusions">
	/// A set of additional assembly name prefixes to exclude. These will be applied 
	/// along with or instead of the default exclusions, depending on the value of 
	/// <paramref name="ignoreDefaultExclusions"/>.
	/// </param>
	/// <param name="ignoreDefaultExclusions">
	/// If <see langword="true"/>, only the provided exclusions in <paramref name="customExclusions"/> will be used, 
	/// and all default exclusions will be ignored.  
	/// If <see langword="false"/>, the provided exclusions will be merged with the default exclusions.
	/// </param>
	/// <returns>An enumerable collection of non-excluded assemblies.</returns>
	/// <remarks>
	/// <para><b>Default Exclusions:</b></para>
	/// Assemblies with certain prefixes (e.g., "System", "Microsoft", "Azure") 
	/// are excluded unless <paramref name="ignoreDefaultExclusions"/> is set to <see langword="true"/>.
	/// 
	/// <para><b>Exception Handling:</b></para>
	/// The following exceptions are caught and ignored to ensure scanning continues smoothly:
	/// <list type="bullet">
	///   <item><description><see cref="ReflectionTypeLoadException"/> - If an assembly cannot be loaded properly.</description></item>
	///   <item><description><see cref="FileNotFoundException"/> - If an assembly dependency is missing.</description></item>
	///   <item><description><see cref="BadImageFormatException"/> - If an assembly is not a valid .NET assembly.</description></item>
	/// </list>
	/// </remarks>
	public static IEnumerable<Assembly> ScanAssemblies(HashSet<string>? customExclusions = null, bool ignoreDefaultExclusions = false) {
		var seenNames = new HashSet<string>();

		// Determine which exclusions to use
		var exclusions = ignoreDefaultExclusions
			? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			: new HashSet<string>(ExcludedAssemblyNames, StringComparer.OrdinalIgnoreCase);

		if (customExclusions != null) {
			exclusions.UnionWith(customExclusions);
		}

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
			Assembly? scannedAssembly = null;

			try {
				var (excluded, name) = IsExcluded(assembly, exclusions);
				if (excluded) {
					continue;
				}

				if (seenNames.Add(name)) {
					scannedAssembly = assembly;
				}
			} catch (Exception ex) when (
				  ex is ReflectionTypeLoadException ||
				  ex is FileNotFoundException ||
				  ex is BadImageFormatException) {
				// Log or handle the exception as needed
				continue;
			}

			if (scannedAssembly != null) {
				yield return scannedAssembly;
			}
		}
	}

	/// <summary>
	/// Scans and returns assemblies that reference the specified assembly name,
	/// including the referenced assembly itself.
	/// </summary>
	/// <remarks>
	/// This method includes both the referenced assembly (which may contain base implementations)
	/// and all assemblies that reference it (which contain consumer implementations).
	/// </remarks>
	/// <param name="referencedAssemblyName">The name of the assembly that must be referenced.</param>
	/// <param name="customExclusions">Additional assembly name prefixes to exclude.</param>
	/// <param name="ignoreDefaultExclusions">If true, only custom exclusions are applied.</param>
	/// <returns>An enumerable collection of assemblies that reference the specified assembly, plus the assembly itself.</returns>
	public static IEnumerable<Assembly> ScanReferencedAssemblies(
		string referencedAssemblyName,
		HashSet<string>? customExclusions = null,
		bool ignoreDefaultExclusions = false) {

		foreach (var assembly in ScanAssemblies(customExclusions, ignoreDefaultExclusions)) {
			var assemblyName = assembly.GetName().Name;

			// Include the assembly itself (may contain built-in implementations)
			if (assemblyName == referencedAssemblyName) {
				yield return assembly;
				continue;
			}

			// Include assemblies that reference it (consumer implementations)
			if (assembly.GetReferencedAssemblies().Any(a => a.Name == referencedAssemblyName)) {
				yield return assembly;
			}
		}
	}

	/// <summary>
	/// Returns exported types from scanned assemblies that match the specified predicate.
	/// </summary>
	/// <param name="predicate">A function to test each exported type. 
	/// The type is included if the predicate returns <see langword="true"/>.</param>
	/// <param name="customAssemblyExclusions">
	/// A set of additional assembly name prefixes to exclude. These will be applied 
	/// along with or instead of the default assembly exclusions, depending on the value of 
	/// <paramref name="ignoreDefaultAssemblyExclusions"/>.
	/// </param>
	/// <param name="ignoreDefaultAssemblyExclusions">
	/// If <see langword="true"/>, only the provided assembly exclusions in <paramref name="customAssemblyExclusions"/>
	/// will be used, and all default assembly exclusions will be ignored.  
	/// If <see langword="false"/>, the provided assembly exclusions will be merged with the default assembly exclusions.
	/// </param>
	/// <returns>An enumerable collection of matching exported types.</returns>
	/// <remarks>
	/// <para>
	/// - Only <c>public</c> types that are visible outside their assembly are included.  
	/// </para>
	/// <para>
	/// - Types are <c>yielded</c> one at a time for improved memory efficiency.
	/// </para>
	/// </remarks>
	public static IEnumerable<Type> ScanExportedTypes(Func<Type, bool> predicate, HashSet<string>? customAssemblyExclusions = null, bool ignoreDefaultAssemblyExclusions = false) {
		foreach (var implementationType in ScanAssemblies(customAssemblyExclusions, ignoreDefaultAssemblyExclusions)
			.SelectMany(a => a.GetExportedTypes())
			.Where(t => predicate(t))) {
			yield return implementationType;
		}
	}

	/// <summary>
	/// Returns all types (both public and non-public) from scanned assemblies that match the
	/// specified predicate.
	/// </summary>
	/// <param name="predicate">A function to test each type. 
	/// The type is included if the predicate returns <see langword="true"/>.</param>
	/// <param name="customAssemblyExclusions">
	/// A set of additional assembly name prefixes to exclude. These will be applied 
	/// along with or instead of the default assembly exclusions, depending on the value of 
	/// <paramref name="ignoreDefaultAssemblyExclusions"/>.
	/// </param>
	/// <param name="ignoreDefaultAssemblyExclusions">
	/// If <see langword="true"/>, only the provided assembly exclusions in <paramref name="customAssemblyExclusions"/>
	/// will be used, and all default assembly exclusions will be ignored.  
	/// If <see langword="false"/>, the provided assembly exclusions will be merged with the default assembly exclusions.
	/// </param>
	/// <returns>An enumerable collection of matching types.</returns>
	/// <remarks>
	/// <para>
	/// - Unlike <see cref="ScanExportedTypes"/>, this method includes <c>internal</c> and <c>private</c> types.  
	/// </para>
	/// <para>
	/// - Types are <c>yielded</c> one at a time for improved memory efficiency.
	/// </para>
	/// </remarks>
	public static IEnumerable<Type> ScanTypes(Func<Type, bool> predicate, HashSet<string>? customAssemblyExclusions = null, bool ignoreDefaultAssemblyExclusions = false) {
		foreach (var implementationType in ScanAssemblies(customAssemblyExclusions, ignoreDefaultAssemblyExclusions)
			.SelectMany(a => a.GetTypes())
			.Where(t => predicate(t))) {
			yield return implementationType;
		}
	}

	private static (bool, string) IsExcluded(Assembly assembly, HashSet<string> exclusions) {
		if (assembly == null || assembly.IsDynamic) {
			return (true, string.Empty);
		}

		var assemblyName = assembly.GetName()?.Name;
		if (assemblyName == null) {
			return (true, string.Empty);
		}

		var isExcluded = exclusions.Any(prefix =>
			assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

		return (isExcluded, assemblyName);
	}

	private static readonly HashSet<string> ExcludedAssemblyNames = new(StringComparer.OrdinalIgnoreCase) {
		"System",
		"Microsoft",
		"mscorlib",
		"netstandard",
		"Azure",
		"Amazon",
		"AWS",
		"Polly",
		"Humanizer",
		"SmartFormat",
		"Swashbuckle",
		"Scalar",
		"Google",
		"Grpc",
		"Facebook",
		"CommandLine",
		"Vio",
		"MediatR",
		"BlazorApplicationInsights"
	};
}
