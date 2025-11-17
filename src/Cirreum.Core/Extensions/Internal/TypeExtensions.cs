namespace Cirreum.Extensions.Internal;

using System;
using System.Reflection;

internal static class TypeExtensions {

	/// <summary>
	/// Evaluates the specified collection, to determine if it contains the <typeparamref name="TType"/> type.
	/// </summary>
	/// <typeparam name="TType">The type to evaluate for existence</typeparam>
	/// <param name="types">The collection of types to evaluate.</param>
	/// <returns>
	/// <see langword="true"/> if the provided <paramref name="types"/> contains the
	/// <typeparamref name="TType"/> type; otherwise <see langword="false"/></returns>
	public static bool ContainsType<TType>(this Type[] types) {
		var ttype = typeof(TType);
		return types.ContainsType(ttype);
	}

	/// <summary>
	/// Evaluates the specified collection, to determine if it contains the specified <paramref name="type"/>.
	/// </summary>
	/// <param name="types">The collection of types to evaluate.</param>
	/// <param name="type">The type to evaluate for existence</param>
	/// <returns>
	/// <see langword="true"/> if the provided <paramref name="types"/> contains the
	/// specified <paramref name="type"/>; otherwise <see langword="false"/></returns>
	public static bool ContainsType(this Type[] types, Type type) =>
		 Array.IndexOf(types, type) >= 0;

	/// <summary>
	/// Retrieves the value of a public static property from the specified type.
	/// </summary>
	/// <typeparam name="T">The expected type of the property's value. Must be a non-nullable type.</typeparam>
	/// <param name="type">The type from which to retrieve the static property value.</param>
	/// <param name="propertyName">The name of the static property to retrieve.</param>
	/// <param name="defaultValue">
	/// The value to return if the property's value is <c>null</c>. 
	/// If both the property value and <paramref name="defaultValue"/> are <c>null</c>, 
	/// an <see cref="InvalidOperationException"/> is thrown.
	/// </param>
	/// <returns>
	/// The non-null value of the static property, cast to type <typeparamref name="T"/>, 
	/// or <paramref name="defaultValue"/> if the property value is <c>null</c>.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the specified property is not found, is not a public static property, 
	/// or when both the property value and <paramref name="defaultValue"/> are <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="type"/> is <c>null</c>.
	/// </exception>
	/// <remarks>
	/// This method searches only for public static properties declared directly on the specified type.
	/// It does not search inherited static properties from base classes.
	/// </remarks>
	public static T GetStaticPropertyValue<T>(
		this Type type,
		string propertyName,
		T? defaultValue = default)
		where T : notnull {
		var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
				   ?? throw new InvalidOperationException(
					   $"Type {type.Name} is missing {propertyName} property");
		if (prop.GetValue(null) is T propertyValue) {
			return propertyValue;
		}
		return defaultValue ?? throw new InvalidOperationException(
			$"Type {type.Name} has null {propertyName} value");
	}

	/// <summary>
	/// Determines if the type is a concrete class (not abstract and not a generic type definition).
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a concrete class, false otherwise.</returns>
	public static bool IsConcreteClass(this Type type) =>
		type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition;

	/// <summary>
	/// Determines if the type is concrete (not abstract and not an interface).
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is concrete, false otherwise.</returns>
	public static bool IsConcrete(this Type type) =>
		!type.IsAbstract && !type.IsInterface;

	/// <summary>
	/// Determines if the type implements a specific generic interface.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="openGenericInterface">The open generic interface type to look for.</param>
	/// <returns>True if the type implements the specified generic interface, false otherwise.</returns>
	public static bool ImplementsGenericInterface(this Type type, Type openGenericInterface) {
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(openGenericInterface);

		if (!openGenericInterface.IsInterface || !openGenericInterface.IsGenericTypeDefinition) {
			throw new ArgumentException("Must be an open generic interface type.", nameof(openGenericInterface));
		}

		return type.GetInterfaces()
			.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);
	}

	/// <summary>
	/// Determines if the type inherits from a specific generic class.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="openGenericBase">The open generic base class type to look for.</param>
	/// <returns>True if the type inherits from the specified generic class, false otherwise.</returns>
	public static bool InheritsFromGenericClass(this Type type, Type openGenericBase) {
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(openGenericBase);

		var def = openGenericBase.IsGenericTypeDefinition
			? openGenericBase
			: openGenericBase.GetGenericTypeDefinition();

		if (!def.IsClass) {
			throw new ArgumentException("Template must be a class.", nameof(openGenericBase));
		}

		for (var t = type; t is not null && t != typeof(object); t = t.BaseType) {
			if (t.IsGenericType && t.GetGenericTypeDefinition() == def) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the first interface that matches the specified generic interface type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="openGenericType">The open generic interface type to look for.</param>
	/// <returns>The matching interface type if found; otherwise, null.</returns>
	public static Type? GetFirstMatchingGenericInterface(this Type type, Type openGenericType) =>
		type.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType &&
								 i.GetGenericTypeDefinition() == openGenericType);

}