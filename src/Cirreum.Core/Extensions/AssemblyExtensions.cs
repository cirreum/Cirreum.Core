namespace System.Reflection;

using System;
using System.Linq;

/// <summary>
/// Provides extension methods for the <see cref="Assembly"/> class to simplify type discovery.
/// </summary>
public static class AssemblyExtensions {

	/// <summary>
	/// Gets all non-abstract, non-interface types from the assembly that can be assigned to the specified type.
	/// </summary>
	/// <param name="assembly">The assembly to search for types.</param>
	/// <param name="type">The type to check for assignment compatibility.</param>
	/// <returns>An array of types from the assembly that are assignable to the specified type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> or <paramref name="type"/> is null.</exception>
	public static Type[] GetTypesOf(this Assembly assembly, Type type) {

		ArgumentNullException.ThrowIfNull(assembly);

		ArgumentNullException.ThrowIfNull(type);

		return [.. assembly
			.GetTypes()
			.Where(x => !x.IsInterface && !x.IsAbstract && type.IsAssignableFrom(x))];

	}

	/// <summary>
	/// Gets all non-abstract, non-interface types from the assembly that can be assigned to the type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to check for assignment compatibility.</typeparam>
	/// <param name="assembly">The assembly to search for types.</param>
	/// <returns>An array of types from the assembly that are assignable to the type <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is null.</exception>
	public static Type[] GetTypesOf<T>(this Assembly assembly) {
		ArgumentNullException.ThrowIfNull(assembly);
		return assembly.GetTypesOf(typeof(T));
	}

}